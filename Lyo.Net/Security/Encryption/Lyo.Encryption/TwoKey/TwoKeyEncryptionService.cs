using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using Lyo.Common.Extensions;
using Lyo.Common.Records;
using Lyo.Encryption.Exceptions;
using Lyo.Encryption.Security;
using Lyo.Encryption.Streaming;
using Lyo.Exceptions;
using Lyo.Keystore;
using Lyo.Streams;
using KeyNotFoundException = Lyo.Keystore.Exceptions.KeyNotFoundException;

namespace Lyo.Encryption.TwoKey;

/// <summary>
/// Generic two-key encryption service that uses a Key Encryption Key (KEK) to encrypt Data Encryption Keys (DEK). Supports any encryption algorithm via IEncryptionService,
/// enabling envelope encryption where each encryption operation uses a unique DEK that is encrypted with the KEK. This design allows flexibility in choosing encryption algorithms: -
/// Use the same algorithm for both DEK and KEK operations (most common) - Use different algorithms for DEK and KEK operations if needed - Leverages existing encryption service
/// implementations, reducing code duplication
/// </summary>
public sealed class TwoKeyEncryptionService<TKeyEncryptionService, TDataEncryptionService> : ITwoKeyEncryptionService, IDisposable
    where TKeyEncryptionService : IEncryptionService where TDataEncryptionService : IEncryptionService

{
    // Stream format: [FormatVersion: 1][DEKAlgorithmId: 1][KEKAlgorithmId: 1][DekKeyMaterialBytes: 1][DekEncoding: 1][KeyIdLength: 4][KeyId][KeyVersionLength: 4][KeyVersion]
    //                [EncryptedDEKLength: 4][EncryptedDEK][NoncePrefix: NonceSize-4][Chunks...]
    // Every chunk authenticates the immutable header fields ([FormatVersion][DEKAlgorithmId][KEKAlgorithmId][DekKeyMaterialBytes] + NoncePrefix) as AAD; per-chunk nonces are
    // derived from the nonce prefix plus a chunk counter (see AeadStreamProcessor). Mutable fields (KeyId, KeyVersion, EncryptedDEK, DekEncoding) are deliberately excluded so
    // KEK rotation can rewrite the header in place without re-encrypting data; they need no AAD binding because tampering with them yields a different (or unrecoverable) DEK,
    // which fails every chunk's authentication tag anyway.
    private const byte CurrentFormatVersion = (byte)StreamFormatVersion.V1;

    /// <summary>DEK encoding: the DEK is encrypted with the KEK encryption service's regular single-shot format (used when no raw AES-sized KEK is available, e.g. RSA KEKs).</summary>
    private const byte DekEncodingEnvelope = EncryptionHeader.DekEncodingEnvelope;

    /// <summary>DEK encoding: the DEK is wrapped with AES Key Wrap (RFC 3394) — deterministic, integrity-checked, and always exactly <c>dekLength + 8</c> bytes.</summary>
    private const byte DekEncodingAesKeyWrap = EncryptionHeader.DekEncodingAesKeyWrap;

    /// <summary>Maximum accepted encrypted-DEK length. Bounds attacker-controlled allocation when parsing the stream header (largest real value is ~1 KB for RSA-4096 + framing).</summary>
    private const int MaxEncryptedDekLength = 8 * 1024;

    private readonly TDataEncryptionService _dekEncryptionService; // For encrypting data with DEK

    private readonly TKeyEncryptionService _kekEncryptionService; // For encrypting DEK with KEK

    private readonly IKeyStore _keyStore;
    private Encoding _decryptionEncoding = Encoding.UTF8;

    private Encoding _encryptionEncoding = Encoding.UTF8;

    /// <summary>Initializes a new instance using the same encryption service for both DEK and KEK operations. This is the most common use case.</summary>
    /// <param name="encryptionService">The encryption service to use for both DEK and KEK operations</param>
    /// <param name="keyStore">The key store to use for retrieving Key Encryption Keys (KEK)</param>
    /// <exception cref="ArgumentNullException">Thrown when encryptionService or keyStore is null</exception>
    /// <exception cref="ArgumentException">Thrown when keyStore doesn't have any keys configured, or when encryptionService cannot be used for both DEK and KEK operations</exception>
    public TwoKeyEncryptionService(IEncryptionService encryptionService, IKeyStore keyStore)
    {
        // Ensure both generic types can use the same encryption service instance
        // This constructor only works when TDataEncryptionService and TKeyEncryptionService are the same type
        ArgumentHelpers.ThrowIf(
            typeof(TDataEncryptionService) != typeof(TKeyEncryptionService),
            $"This constructor can only be used when both DEK and KEK encryption service types are the same. " +
            $"Current types: DEK={typeof(TDataEncryptionService).Name}, KEK={typeof(TKeyEncryptionService).Name}. " +
            "Use the constructor that takes separate DEK and KEK services instead.", nameof(encryptionService));

        if (encryptionService is not TDataEncryptionService dekService) {
            throw new ArgumentException(
                $"Encryption service must be of type {typeof(TDataEncryptionService).Name}, but was {encryptionService.GetType().Name}", nameof(encryptionService));
        }

        // Both are the same type, so we can use the same instance
        _dekEncryptionService = dekService;
        _kekEncryptionService = (TKeyEncryptionService)(object)dekService;
        _keyStore = keyStore;
    }

    /// <summary>
    /// Initializes a new instance with separate encryption services for DEK and KEK operations. Use this constructor if you need different algorithms for data encryption vs key
    /// encryption.
    /// </summary>
    /// <param name="dekEncryptionService">The encryption service to use for encrypting data with the DEK</param>
    /// <param name="kekEncryptionService">The encryption service to use for encrypting the DEK with the KEK</param>
    /// <param name="keyStore">The key store to use for retrieving Key Encryption Keys (KEK)</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null</exception>
    /// <exception cref="ArgumentException">Thrown when keyStore doesn't have any keys configured</exception>
    public TwoKeyEncryptionService(TDataEncryptionService dekEncryptionService, TKeyEncryptionService kekEncryptionService, IKeyStore keyStore)
    {
        _dekEncryptionService = dekEncryptionService;
        _kekEncryptionService = kekEncryptionService;
        _keyStore = keyStore;
    }

    /// <summary>
    /// Disposes of the encryption service. The injected DEK/KEK services and keystore are NOT disposed: they are owned by the caller (or DI container) and may be shared with
    /// other consumers.
    /// </summary>
    public void Dispose() { }

    public string FileExtension => _dekEncryptionService.FileExtension + FileTypeInfo.TwoKeyEnvelopeSuffix;

    /// <inheritdoc />
    public Encoding GetEncryptionEncoding() => _encryptionEncoding;

    /// <inheritdoc />
    public void SetEncryptionEncoding(Encoding encoding) => _encryptionEncoding = encoding ?? throw new ArgumentNullException(nameof(encoding));

    /// <inheritdoc />
    public Encoding GetDecryptionEncoding() => _decryptionEncoding;

    /// <inheritdoc />
    public void SetDecryptionEncoding(Encoding encoding) => _decryptionEncoding = encoding ?? throw new ArgumentNullException(nameof(encoding));

    /// <summary>Gets the encryption algorithm used for Data Encryption Key (DEK) operations.</summary>
    public EncryptionAlgorithm? DekAlgorithm => DetermineAlgorithm(_dekEncryptionService);

    /// <summary>Gets the encryption algorithm used for Key Encryption Key (KEK) operations.</summary>
    public EncryptionAlgorithm? KekAlgorithm => DetermineAlgorithm(_kekEncryptionService);

    /// <summary>Gets the current key version for a specific key ID.</summary>
    /// <param name="keyId">The key identifier</param>
    /// <returns>The current key version, or null if no keys exist for this key ID</returns>
    public string? GetKeyVersion(string keyId) => _keyStore.GetCurrentVersion(keyId);

    /// <summary>Gets the salt used for key derivation for a specific key ID and version.</summary>
    /// <param name="keyId">The key identifier</param>
    /// <param name="version">The key version</param>
    /// <returns>The salt used for key derivation, or null if not available</returns>
    public byte[]? GetSaltForVersion(string keyId, string version) => _keyStore.GetSaltForVersion(keyId, version);

    public TwoKeyEncryptionResult Encrypt(byte[] bytes, string? keyId = null, byte[]? kek = null)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(bytes);
        OperationHelpers.ThrowIf(keyId == null && kek == null, "Either keyId or kek must be provided.");
        var dek = CryptographicRandom.GetBytes(GetDekKeyMaterialSize(_dekEncryptionService));
        try {
            // Encrypt data with DEK using the DEK encryption service (no keyId for DEK - it's random)
            var encryptedData = _dekEncryptionService.Encrypt(bytes, null, dek);

            // Get KEK from keystore or use provided kek
            byte[]? kekBytes = null;
            string? actualKeyId = null;
            var keyVersion = "";
            if (kek != null)
                kekBytes = kek;
            else if (keyId != null) {
                actualKeyId = keyId;
                kekBytes = _keyStore.GetCurrentKey(keyId);
                OperationHelpers.ThrowIfNull(kekBytes, $"No Key Encryption Key available for key ID '{keyId}'. Ensure a key is configured in the KeyStore.");
                keyVersion = _keyStore.GetCurrentVersion(keyId) ?? "";
            }

            // Protect the DEK (AES Key Wrap when a raw AES-sized KEK is available, otherwise the KEK service's envelope)
            var encryptedDek = WrapDek(dek, kekBytes);

            // Get salt from keystore metadata to include in result (so FileStorageService can store it)
            byte[]? salt = null;
            if (actualKeyId == null || string.IsNullOrWhiteSpace(keyVersion))
                return new(encryptedData, encryptedDek, actualKeyId ?? "", keyVersion, salt, (byte)GetDekKeyMaterialSize(_dekEncryptionService));

            var keyMetadata = _keyStore.GetKeyMetadata(actualKeyId, keyVersion);
            if (keyMetadata?.AdditionalData == null || !keyMetadata.AdditionalData.TryGetValue("Pbkdf2Salt", out var saltBase64))
                return new(encryptedData, encryptedDek, actualKeyId, keyVersion, salt, (byte)GetDekKeyMaterialSize(_dekEncryptionService));

            try {
                salt = Convert.FromBase64String(saltBase64);
            }
            catch (FormatException) {
                // Invalid base64, salt will remain null
            }

            return new(encryptedData, encryptedDek, actualKeyId, keyVersion, salt, (byte)GetDekKeyMaterialSize(_dekEncryptionService));
        }
        finally {
            // Securely clear the DEK from memory
            SecurityUtilities.Clear(dek);
        }
    }

    /// <summary> Encrypts a string with encoding support. </summary>
    /// <param name="text">String to encrypt</param>
    /// <param name="keyId">The key identifier to use from the KeyStore. If null, uses the provided kek directly.</param>
    /// <param name="kek">Optional Key Encryption Key. If null and keyId is provided, uses the key from KeyStore.</param>
    /// <param name="encoding">Optional encoding. If null, uses the encryption encoding (see <see cref="GetEncryptionEncoding" />).</param>
    /// <returns>Encryption result containing encrypted data and encrypted DEK</returns>
    public TwoKeyEncryptionResult EncryptString(string text, string? keyId = null, byte[]? kek = null, Encoding? encoding = null)
        => Encrypt((encoding ?? GetEncryptionEncoding()).GetBytes(text), keyId, kek);

    public byte[] Decrypt(byte[] encryptedData, byte[] encryptedDataEncryptionKey, string? keyId = null, byte[]? kek = null, string? keyVersion = null, byte[]? salt = null)
    {
        ArgumentHelpers.ThrowIfNotInRange(encryptedData, 1, long.MaxValue);
        ArgumentHelpers.ThrowIfNotInRange(encryptedDataEncryptionKey, 1, long.MaxValue);
        byte[]? kekBytes = null;
        if (kek != null)
            kekBytes = kek;
        else if (keyId != null) {
            // Get KEK from keystore using keyId and optional version
            kekBytes = !string.IsNullOrWhiteSpace(keyVersion) ? _keyStore.GetKey(keyId, keyVersion) : _keyStore.GetCurrentKey(keyId);
        }
        else
            OperationHelpers.ThrowIf(true, "Either keyId or kek must be provided for decryption.");

        if (kekBytes == null) {
            var versionInfo = !string.IsNullOrWhiteSpace(keyVersion) ? $"version {keyVersion}" : "current version";
            var keyInfo = keyId != null ? $"key ID '{keyId}' " : "";
            var saltInfo = salt != null ? " Salt is available but cannot be used to derive KEK without the original password." : "";
            throw new KeyNotFoundException(
                $"No Key Encryption Key available for {keyInfo}{versionInfo} in KeyStore.{saltInfo} Ensure the keystore is properly initialized with the required key.");
        }

        // Recover the DEK (AES Key Wrap or KEK-service envelope, matching WrapDek)
        byte[]? dek = null;
        try {
            dek = UnwrapDek(encryptedDataEncryptionKey, kekBytes);
            TwoKeyDekValidation.ValidatePlaintextDekLength(dek, GetDekKeyMaterialSize(_dekEncryptionService));

            // Decrypt data using DEK encryption service
            return _dekEncryptionService.Decrypt(encryptedData, null, dek);
        }
        finally {
            // Securely clear the DEK from memory after decryption
            if (dek != null)
                SecurityUtilities.Clear(dek);
        }
    }

    /// <summary> Decrypts encrypted data and returns the decrypted string with encoding support. </summary>
    /// <param name="encryptedData">The encrypted data</param>
    /// <param name="encryptedDataEncryptionKey">The encrypted Data Encryption Key (DEK)</param>
    /// <param name="keyId">The key identifier to use from the KeyStore. If null, uses the provided kek directly.</param>
    /// <param name="encoding">Encoding to use for decoding the decrypted bytes. If null, uses the decryption encoding (see <see cref="GetDecryptionEncoding" />).</param>
    /// <param name="kek">Optional Key Encryption Key. If null and keyId is provided, uses the key from KeyStore.</param>
    /// <param name="keyVersion">Optional key version. If provided and a key store is configured, uses the key for that version.</param>
    /// <param name="salt">Optional salt used to derive the KEK. If provided, the KEK will be derived using this salt instead of the salt stored in keystore metadata.</param>
    /// <returns>Decrypted string</returns>
    public string DecryptString(
        byte[] encryptedData,
        byte[] encryptedDataEncryptionKey,
        string? keyId = null,
        Encoding? encoding = null,
        byte[]? kek = null,
        string? keyVersion = null,
        byte[]? salt = null)
        => (encoding ?? GetDecryptionEncoding()).GetString(Decrypt(encryptedData, encryptedDataEncryptionKey, keyId, kek, keyVersion, salt));

    public async Task<TwoKeyEncryptionResult> EncryptStreamAsync(Stream input, string? keyId = null, byte[]? kek = null, int chunkSize = 1024 * 1024)
    {
        ArgumentHelpers.ThrowIfNull(input);
        OperationHelpers.ThrowIfNotReadable(input, $"Stream '{nameof(input)}' must be readable.");
        OperationHelpers.ThrowIf(keyId == null && kek == null, "Either keyId or kek must be provided.");
        var dek = CryptographicRandom.GetBytes(GetDekKeyMaterialSize(_dekEncryptionService));
        try {
            using var encryptedDataStream = new MemoryStream();
            using var encryptedDataWriter = new BinaryWriter(encryptedDataStream);

            // Encrypt data chunks using DEK encryption service (no keyId for DEK - it's random).
            // The span overload encrypts straight from the pooled buffer — no exact-size copy per chunk.
            var buffer = ArrayPool<byte>.Shared.Rent(chunkSize);
            try {
                int bytesRead;
                while ((bytesRead = await input.ReadAsync(buffer, 0, chunkSize).ConfigureAwait(false)) > 0) {
                    var encryptedChunk = _dekEncryptionService.Encrypt(buffer.AsSpan(0, bytesRead), null, dek);
                    encryptedDataWriter.Write(encryptedChunk.Length);
                    encryptedDataWriter.Write(encryptedChunk);
                }
            }
            finally {
                // Buffer held plaintext — zero it before returning to the shared pool.
                ArrayPool<byte>.Shared.Return(buffer, true);
            }

            byte[]? kekBytes = null;
            string? actualKeyId = null;
            string? keyVersion = null;
            if (kek != null)
                kekBytes = kek;
            else if (keyId != null) {
                actualKeyId = keyId;
                kekBytes = await _keyStore.GetCurrentKeyAsync(keyId).ConfigureAwait(false);
                OperationHelpers.ThrowIfNull(kekBytes, $"No Key Encryption Key available for key ID '{keyId}'. Ensure a key is configured.");
                keyVersion = await _keyStore.GetCurrentVersionAsync(keyId).ConfigureAwait(false) ?? "";
            }

            // Protect the DEK (AES Key Wrap when a raw AES-sized KEK is available, otherwise the KEK service's envelope)
            var encryptedDek = WrapDek(dek, kekBytes);

            // Get salt from keystore metadata to include in result
            byte[]? salt = null;
            if (actualKeyId == null || keyVersion.IsNullOrWhitespace())
                return new(encryptedDataStream.ToArray(), encryptedDek, actualKeyId ?? "", keyVersion ?? "", salt, (byte)GetDekKeyMaterialSize(_dekEncryptionService));

            var keyMetadata = await _keyStore.GetKeyMetadataAsync(actualKeyId, keyVersion).ConfigureAwait(false);
            if (keyMetadata?.AdditionalData == null || !keyMetadata.AdditionalData.TryGetValue("Pbkdf2Salt", out var saltBase64))
                return new(encryptedDataStream.ToArray(), encryptedDek, actualKeyId, keyVersion, salt, (byte)GetDekKeyMaterialSize(_dekEncryptionService));

            try {
                salt = Convert.FromBase64String(saltBase64);
            }
            catch (FormatException) {
                // Invalid base64, salt will remain null
            }

            return new(encryptedDataStream.ToArray(), encryptedDek, actualKeyId, keyVersion, salt, (byte)GetDekKeyMaterialSize(_dekEncryptionService));
        }
        finally {
            // Securely clear the DEK from memory
            SecurityUtilities.Clear(dek);
        }
    }

    public async Task DecryptStreamAsync(TwoKeyEncryptionResult result, Stream output, string? keyId = null, byte[]? kek = null)
    {
        ArgumentHelpers.ThrowIfNull(result);
        ArgumentHelpers.ThrowIfNull(output);
        OperationHelpers.ThrowIfNotWritable(output, $"Stream '{nameof(output)}' must be writable.");
        byte[]? kekBytes = null;
        var actualKeyId = result.KeyId;
        if (kek != null)
            kekBytes = kek;
        else if (keyId != null) {
            actualKeyId = keyId;
            if (!string.IsNullOrWhiteSpace(result.KeyVersion))
                kekBytes = await _keyStore.GetKeyAsync(keyId, result.KeyVersion).ConfigureAwait(false);
            else
                kekBytes = await _keyStore.GetCurrentKeyAsync(keyId).ConfigureAwait(false);
        }
        else if (!string.IsNullOrEmpty(result.KeyId)) {
            // Use keyId from result
            if (!string.IsNullOrWhiteSpace(result.KeyVersion))
                kekBytes = await _keyStore.GetKeyAsync(result.KeyId, result.KeyVersion).ConfigureAwait(false);
            else
                kekBytes = await _keyStore.GetCurrentKeyAsync(result.KeyId).ConfigureAwait(false);
        }

        if (kekBytes == null) {
            var versionInfo = !string.IsNullOrWhiteSpace(result.KeyVersion) ? $"version {result.KeyVersion}" : "current version";
            var saltInfo = result.KeyEncryptionKeySalt != null ? " Salt is available but cannot be used to derive KEK without the original password." : "";
            throw new KeyNotFoundException(
                $"No Key Encryption Key available for ID {actualKeyId} {versionInfo} in KeyStore.{saltInfo} Ensure the keystore is properly initialized with the required key.");
        }

        // Recover the DEK (AES Key Wrap or KEK-service envelope, matching WrapDek)
        byte[]? dek;
        try {
            dek = UnwrapDek(result.EncryptedDataEncryptionKey, kekBytes);
            TwoKeyDekValidation.ValidatePlaintextDekLength(dek, result.DekKeyMaterialBytes);
        }
        catch (DecryptionFailedException) {
            throw;
        }
        catch (Exception ex) {
            throw new DecryptionFailedException("Failed to decrypt Data Encryption Key. Possible causes: wrong KEK, corrupted data, or authentication failure.", ex);
        }

        try {
            // Maximum allowed encrypted chunk size (200 MB) to prevent denial-of-service attacks
            const int maxEncryptedChunkSize = 200 * 1024 * 1024; // 200 MB

            // Decrypt data chunks using DEK encryption service
            using var encryptedDataStream = new MemoryStream(result.EncryptedData);
            using var encryptedDataReader = new BinaryReader(encryptedDataStream);
            while (encryptedDataStream.Position < encryptedDataStream.Length) {
                var chunkLength = encryptedDataReader.ReadInt32();

                // Validate chunk length to prevent DoS attacks
                if (chunkLength <= 0)
                    throw new InvalidDataException($"Invalid chunk length: {chunkLength}. Chunk length must be positive.");

                if (chunkLength > maxEncryptedChunkSize) {
                    throw new InvalidDataException(
                        $"Invalid chunk length: {chunkLength} bytes. Maximum allowed: {maxEncryptedChunkSize} bytes ({maxEncryptedChunkSize / (1024 * 1024)} MB).");
                }

                // Check if stream has enough remaining data for this chunk
                var remainingBytes = encryptedDataStream.Length - encryptedDataStream.Position;
                if (remainingBytes < chunkLength)
                    throw new InvalidDataException($"Invalid encrypted data format: chunk length ({chunkLength} bytes) exceeds remaining stream size ({remainingBytes} bytes).");

                // BinaryReader.ReadBytes allocates a new array, which is fine for this path
                // as we're reading from a MemoryStream and the chunks are already in memory
                var encryptedChunk = encryptedDataReader.ReadBytes(chunkLength);
                byte[] decryptedChunk;
                try {
                    decryptedChunk = _dekEncryptionService.Decrypt(encryptedChunk, null, dek);
                }
                catch (DecryptionFailedException) {
                    throw;
                }
                catch (Exception ex) {
                    throw new DecryptionFailedException("Failed to decrypt data chunk. Possible causes: wrong key, corrupted data, or authentication failure.", ex);
                }

                await output.WriteAsync(decryptedChunk, 0, decryptedChunk.Length, CancellationToken.None).ConfigureAwait(false);
            }
        }
        finally {
            // Securely clear the DEK from memory after decryption
            SecurityUtilities.Clear(dek);
        }
    }

    public async Task EncryptToStreamAsync(Stream input, Stream output, string? keyId = null, byte[]? kek = null, int chunkSize = 1024 * 1024, CancellationToken ct = default)
    {
        OperationHelpers.ThrowIf(keyId == null && kek == null, "Either keyId or kek must be provided.");
        if (_dekEncryptionService is not EncryptionServiceBase dekBase) {
            throw new NotSupportedException(
                $"The data encryption service '{_dekEncryptionService.GetType().Name}' does not support streaming. It must derive from EncryptionServiceBase (a single-key AEAD service).");
        }

        // Generate a single DEK for the entire stream
        var dek = CryptographicRandom.GetBytes(GetDekKeyMaterialSize(_dekEncryptionService));
        try {
            byte[]? kekBytes = null;
            string? actualKeyId = null;
            string? keyVersion = null;
            if (kek != null)
                kekBytes = kek;
            else if (keyId != null) {
                actualKeyId = keyId;
                kekBytes = await _keyStore.GetCurrentKeyAsync(keyId, ct).ConfigureAwait(false);
                OperationHelpers.ThrowIfNull(kekBytes, $"No Key Encryption Key available for key ID '{keyId}'. Ensure a key is configured.");
                keyVersion = await _keyStore.GetCurrentVersionAsync(keyId, ct).ConfigureAwait(false) ?? "";
            }

            // Prefer AES Key Wrap (RFC 3394) when a raw AES-sized KEK is available and the KEK service is a symmetric AEAD: the wrapped DEK is deterministic, fixed-size
            // (dekLength + 8 bytes) and carries its own integrity check. Otherwise (e.g. RSA KEK service) fall back to the KEK service's single-shot envelope.
            byte dekEncoding;
            byte[] encryptedDek;
            if (_kekEncryptionService is EncryptionServiceBase && kekBytes != null && AesKeyWrap.IsValidKekLength(kekBytes.Length)) {
                dekEncoding = DekEncodingAesKeyWrap;
                encryptedDek = AesKeyWrap.Wrap(kekBytes, dek);
            }
            else {
                dekEncoding = DekEncodingEnvelope;
                encryptedDek = _kekEncryptionService.Encrypt(dek, null, kekBytes);
            }

            using var cryptor = dekBase.CreateStreamCryptor(dek);

            // Per-stream random nonce prefix; per-chunk nonces are derived as prefix || counter and never written to the wire.
            var noncePrefix = CryptographicRandom.GetBytes(cryptor.NonceSize - AeadStreamProcessor.CounterSize);

            // Compose the full header in one buffer (single write, and the exact bytes double as per-chunk AAD):
            // [FormatVersion][DEKAlgorithmId][KEKAlgorithmId][DekKeyMaterialBytes][DekEncoding][KeyIdLen][KeyId][KeyVersionLen][KeyVersion][EncryptedDEKLen][EncryptedDEK][NoncePrefix]
            var keyIdBytes = actualKeyId != null ? Encoding.UTF8.GetBytes(actualKeyId) : [];
            var keyVersionBytes = keyVersion != null ? Encoding.UTF8.GetBytes(keyVersion) : [];
            var header = new byte[5 + 4 + keyIdBytes.Length + 4 + keyVersionBytes.Length + 4 + encryptedDek.Length + noncePrefix.Length];
            header[0] = CurrentFormatVersion;
            header[1] = GetAlgorithmIdFromService(_dekEncryptionService);
            header[2] = GetAlgorithmIdFromService(_kekEncryptionService);
            header[3] = (byte)GetDekKeyMaterialSize(_dekEncryptionService);
            header[4] = dekEncoding;
            var offset = 5;
            BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(offset, 4), keyIdBytes.Length);
            keyIdBytes.CopyTo(header.AsSpan(offset + 4));
            offset += 4 + keyIdBytes.Length;
            BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(offset, 4), keyVersionBytes.Length);
            keyVersionBytes.CopyTo(header.AsSpan(offset + 4));
            offset += 4 + keyVersionBytes.Length;
            BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(offset, 4), encryptedDek.Length);
            encryptedDek.CopyTo(header.AsSpan(offset + 4));
            offset += 4 + encryptedDek.Length;
            noncePrefix.CopyTo(header.AsSpan(offset));
            await output.WriteAsync(header, 0, header.Length, ct).ConfigureAwait(false);

            // Encrypt and write data chunks; every chunk authenticates the immutable header fields + nonce prefix as AAD (mutable rotation fields excluded — see class remarks).
            var effectiveChunkSize = chunkSize <= 0 ? StreamChunkSizeHelper.DetermineChunkSize(input) : chunkSize;
            var (aadNonFinal, aadFinal) = EncryptionServiceBase.BuildChunkAads(BuildImmutableAadHeader(header.AsSpan(0, 4), noncePrefix), null);
            await AeadStreamProcessor.EncryptChunksAsync(input, output, cryptor, effectiveChunkSize, noncePrefix, aadNonFinal, aadFinal, ct).ConfigureAwait(false);
        }
        finally {
            // Securely clear the DEK from memory
            SecurityUtilities.Clear(dek);
        }
    }

    public async Task DecryptToStreamAsync(Stream input, Stream output, string? keyId = null, byte[]? kek = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(input);
        ArgumentHelpers.ThrowIfNull(output);
        OperationHelpers.ThrowIfNotReadable(input, $"Stream '{nameof(input)}' must be readable.");
        OperationHelpers.ThrowIfNotWritable(output, $"Stream '{nameof(output)}' must be writable.");
        if (_dekEncryptionService is not EncryptionServiceBase dekBase) {
            throw new NotSupportedException(
                $"The data encryption service '{_dekEncryptionService.GetType().Name}' does not support streaming. It must derive from EncryptionServiceBase (a single-key AEAD service).");
        }

        // Read the fixed header: [FormatVersion][DEKAlgorithmId][KEKAlgorithmId][DekKeyMaterialBytes][DekEncoding]
        var fixedHeader = new byte[5];
        if (await AeadChunkCodec.ReadAtLeastAsync(input, fixedHeader, 5, ct).ConfigureAwait(false) != 5)
            throw new EndOfStreamException("Unexpected end of stream while reading two-key stream header.");

        var rawFormat = fixedHeader[0];
        if (rawFormat != (byte)StreamFormatVersion.V1)
            throw new NotSupportedException($"Unsupported stream format version: {rawFormat}. Supported: {(byte)StreamFormatVersion.V1}.");

        var dekAlgorithmId = fixedHeader[1];
        var kekAlgorithmId = fixedHeader[2];
        var dekKeyMaterialBytes = fixedHeader[3];
        var dekEncoding = fixedHeader[4];
        if (dekEncoding is not (DekEncodingEnvelope or DekEncodingAesKeyWrap))
            throw new InvalidDataException($"Unknown DEK encoding: {dekEncoding}. Supported: {DekEncodingEnvelope} (KEK envelope), {DekEncodingAesKeyWrap} (AES Key Wrap).");

        TwoKeyDekValidation.ValidateHeader(dekAlgorithmId, dekKeyMaterialBytes);

        // Validate algorithm IDs match expected services
        var expectedDekAlgId = GetAlgorithmIdFromService(_dekEncryptionService);
        var expectedKekAlgId = GetAlgorithmIdFromService(_kekEncryptionService);
        if (dekAlgorithmId != expectedDekAlgId) {
            throw new InvalidDataException(
                $"DEK algorithm ID mismatch. Expected {expectedDekAlgId} ({(EncryptionAlgorithm)expectedDekAlgId}), got {dekAlgorithmId} ({(EncryptionAlgorithm)dekAlgorithmId}).");
        }

        if (kekAlgorithmId != expectedKekAlgId) {
            throw new InvalidDataException(
                $"KEK algorithm ID mismatch. Expected {expectedKekAlgId} ({(EncryptionAlgorithm)expectedKekAlgId}), got {kekAlgorithmId} ({(EncryptionAlgorithm)kekAlgorithmId}).");
        }

        var keyIdBytes = await ReadLengthPrefixedAsync(input, 1024, "key ID", ct).ConfigureAwait(false);
        var keyVersionBytes = await ReadLengthPrefixedAsync(input, 1024, "key version", ct).ConfigureAwait(false);
        var encryptedDek = await ReadLengthPrefixedAsync(input, MaxEncryptedDekLength, "encrypted DEK", ct).ConfigureAwait(false);
        if (encryptedDek.Length == 0)
            throw new InvalidDataException("Invalid two-key stream: encrypted DEK is missing.");

        var streamKeyId = keyIdBytes.Length > 0 ? Encoding.UTF8.GetString(keyIdBytes) : null;
        var keyVersion = keyVersionBytes.Length > 0 ? Encoding.UTF8.GetString(keyVersionBytes) : null;

        // Use keyId from parameter if provided, otherwise use keyId from stream
        var actualKeyId = keyId ?? streamKeyId;

        // Resolve the KEK - use keyId and version from stream for proper key rotation support
        byte[]? kekBytes = null;
        if (kek != null)
            kekBytes = kek;
        else if (actualKeyId != null) {
            if (!string.IsNullOrWhiteSpace(keyVersion))
                kekBytes = await _keyStore.GetKeyAsync(actualKeyId, keyVersion, ct).ConfigureAwait(false);
            else
                kekBytes = await _keyStore.GetCurrentKeyAsync(actualKeyId, ct).ConfigureAwait(false);
        }

        if (kekBytes == null) {
            var keyInfo = actualKeyId != null ? $"key ID '{actualKeyId}' " : "";
            var versionInfo = !string.IsNullOrWhiteSpace(keyVersion) ? $"version {keyVersion}" : "current version";
            var saltInfo = " Check keystore metadata for salt (though salt alone cannot derive KEK without the original password).";
            throw new KeyNotFoundException(
                $"No Key Encryption Key available for {keyInfo}{versionInfo} in KeyStore.{saltInfo} Ensure the keystore is properly initialized with the required key.");
        }

        // Recover the DEK using the encoding recorded in the header (AES Key Wrap or the KEK service's envelope).
        byte[]? dek;
        try {
            if (dekEncoding == DekEncodingAesKeyWrap) {
                if (!AesKeyWrap.IsValidKekLength(kekBytes.Length))
                    throw new InvalidDataException($"Stream uses AES Key Wrap DEK encoding, but the resolved KEK is {kekBytes.Length} bytes (expected 16, 24 or 32).");

                dek = AesKeyWrap.Unwrap(kekBytes, encryptedDek);
            }
            else
                dek = _kekEncryptionService.Decrypt(encryptedDek, null, kekBytes);

            TwoKeyDekValidation.ValidatePlaintextDekLength(dek, dekKeyMaterialBytes);
        }
        catch (DecryptionFailedException) {
            throw;
        }
        catch (InvalidDataException) {
            throw;
        }
        catch (Exception ex) {
            throw new DecryptionFailedException("Failed to decrypt Data Encryption Key. Possible causes: wrong KEK, corrupted data, or authentication failure.", ex);
        }

        try {
            using var cryptor = dekBase.CreateStreamCryptor(dek);

            // Read the per-stream nonce prefix, then rebuild the immutable header fields so each chunk can authenticate them as AAD (see class remarks).
            var noncePrefix = new byte[cryptor.NonceSize - AeadStreamProcessor.CounterSize];
            if (noncePrefix.Length > 0 && await AeadChunkCodec.ReadAtLeastAsync(input, noncePrefix, noncePrefix.Length, ct).ConfigureAwait(false) != noncePrefix.Length)
                throw new InvalidDataException("Invalid two-key stream: insufficient data for nonce prefix.");

            ReadOnlySpan<byte> immutableFields = [rawFormat, dekAlgorithmId, kekAlgorithmId, dekKeyMaterialBytes];
            var (aadNonFinal, aadFinal) = EncryptionServiceBase.BuildChunkAads(BuildImmutableAadHeader(immutableFields, noncePrefix), null);
            await AeadStreamProcessor.DecryptChunksAsync(input, output, cryptor, noncePrefix, aadNonFinal, aadFinal, ct).ConfigureAwait(false);
        }
        finally {
            // Securely clear the DEK from memory after decryption
            SecurityUtilities.Clear(dek);
        }
    }

    /// <summary>Reads a <c>[length:int32 LE][bytes]</c> field from <paramref name="input" />, rejecting negative lengths or lengths above <paramref name="maxLength" />.</summary>
    private static async Task<byte[]> ReadLengthPrefixedAsync(Stream input, int maxLength, string fieldName, CancellationToken ct)
    {
        var lengthBuffer = new byte[4];
        if (await AeadChunkCodec.ReadAtLeastAsync(input, lengthBuffer, 4, ct).ConfigureAwait(false) != 4)
            throw new EndOfStreamException($"Unexpected end of stream while reading {fieldName} length.");

        var length = BinaryPrimitives.ReadInt32LittleEndian(lengthBuffer);
        if (length < 0 || length > maxLength)
            throw new InvalidDataException($"Invalid {fieldName} length: {length}. Maximum allowed: {maxLength} bytes.");

        if (length == 0)
            return [];

        var value = new byte[length];
        if (await AeadChunkCodec.ReadAtLeastAsync(input, value, length, ct).ConfigureAwait(false) != length)
            throw new EndOfStreamException($"Unexpected end of stream while reading {fieldName}.");

        return value;
    }

    public async Task EncryptToFileAsync(byte[] data, string outputPath, string? keyId = null, byte[]? kek = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputPath);
        using var inputStream = new MemoryStream(data);
        using var outputStream = File.Create(outputPath);
        await EncryptToStreamAsync(inputStream, outputStream, keyId, kek, ct: ct).ConfigureAwait(false);
    }

    public async Task EncryptToFileAsync(Stream input, string outputPath, string? keyId = null, byte[]? kek = null, int chunkSize = 1024 * 1024, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputPath);
        using var outputStream = File.Create(outputPath);
        await EncryptToStreamAsync(input, outputStream, keyId, kek, chunkSize, ct).ConfigureAwait(false);
    }

    public async Task<byte[]> DecryptFromFileAsync(string inputPath, string? keyId = null, byte[]? kek = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentHelpers.ThrowIfFileNotFound(inputPath);
        using var inputStream = File.OpenRead(inputPath);
        using var outputStream = new MemoryStream();
        await DecryptToStreamAsync(inputStream, outputStream, keyId, kek, ct).ConfigureAwait(false);
        return outputStream.ToArray();
    }

    public byte[] ReEncryptDek(byte[] encryptedDek, string sourceKeyId, string sourceKeyVersion, string? targetKeyId = null, string? targetKeyVersion = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sourceKeyId);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sourceKeyVersion);

        // Determine target keyId (defaults to source if not specified)
        var actualTargetKeyId = targetKeyId ?? sourceKeyId;

        // Get the source KEK from keystore
        var sourceKek = _keyStore.GetKey(sourceKeyId, sourceKeyVersion);
        OperationHelpers.ThrowIfNull(
            sourceKek,
            $"No Key Encryption Key available for key ID '{sourceKeyId}' version {sourceKeyVersion} in KeyStore. Ensure the keystore is properly initialized with the required key version.");

        // Get the target KEK from keystore
        byte[]? targetKek;
        if (!string.IsNullOrWhiteSpace(targetKeyVersion)) {
            targetKek = _keyStore.GetKey(actualTargetKeyId, targetKeyVersion);
            OperationHelpers.ThrowIfNull(
                targetKek,
                $"No Key Encryption Key available for key ID '{actualTargetKeyId}' version {targetKeyVersion} in KeyStore. Ensure the keystore is properly initialized with the required key version.");
        }
        else {
            targetKek = _keyStore.GetCurrentKey(actualTargetKeyId);
            OperationHelpers.ThrowIfNull(
                targetKek,
                $"No current Key Encryption Key available for key ID '{actualTargetKeyId}' in KeyStore. Ensure the keystore is properly initialized with a current key.");

            // If same keyId and same version, throw exception
            if (sourceKeyId == actualTargetKeyId) {
                var currentVersion = _keyStore.GetCurrentVersion(actualTargetKeyId);
                OperationHelpers.ThrowIf(
                    currentVersion == sourceKeyVersion,
                    $"Current key version ({currentVersion}) is the same as the source key version ({sourceKeyVersion}). No re-encryption needed.");
            }
        }

        // Recover the DEK with the source KEK, then re-protect it with the target KEK (AES Key Wrap or envelope, matching WrapDek)
        byte[]? dek = null;
        try {
            dek = UnwrapDek(encryptedDek, sourceKek);
            TwoKeyDekValidation.ValidatePlaintextDekLength(dek, GetDekKeyMaterialSize(_dekEncryptionService));
            return WrapDek(dek, targetKek);
        }
        finally {
            // Securely clear the DEK from memory
            if (dek != null)
                SecurityUtilities.Clear(dek);
        }
    }

    public async Task<byte[]> ReEncryptDekAsync(
        byte[] encryptedDek,
        string sourceKeyId,
        string sourceKeyVersion,
        string? targetKeyId = null,
        string? targetKeyVersion = null,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sourceKeyId);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sourceKeyVersion);

        // Determine target keyId (defaults to source if not specified)
        var actualTargetKeyId = targetKeyId ?? sourceKeyId;

        // Get the source KEK from keystore
        var sourceKek = await _keyStore.GetKeyAsync(sourceKeyId, sourceKeyVersion, ct).ConfigureAwait(false);
        OperationHelpers.ThrowIfNull(
            sourceKek,
            $"No Key Encryption Key available for key ID '{sourceKeyId}' version {sourceKeyVersion} in KeyStore. Ensure the keystore is properly initialized with the required key version.");

        // Get the target KEK from keystore
        byte[]? targetKek;
        if (!string.IsNullOrWhiteSpace(targetKeyVersion)) {
            targetKek = await _keyStore.GetKeyAsync(actualTargetKeyId, targetKeyVersion, ct).ConfigureAwait(false);
            OperationHelpers.ThrowIfNull(
                targetKek,
                $"No Key Encryption Key available for key ID '{actualTargetKeyId}' version {targetKeyVersion} in KeyStore. Ensure the keystore is properly initialized with the required key version.");
        }
        else {
            targetKek = await _keyStore.GetCurrentKeyAsync(actualTargetKeyId, ct).ConfigureAwait(false);
            OperationHelpers.ThrowIfNull(
                targetKek,
                $"No current Key Encryption Key available for key ID '{actualTargetKeyId}' in KeyStore. Ensure the keystore is properly initialized with a current key.");

            // If same keyId and same version, throw exception
            if (sourceKeyId == actualTargetKeyId) {
                var currentVersion = await _keyStore.GetCurrentVersionAsync(actualTargetKeyId, ct).ConfigureAwait(false);
                OperationHelpers.ThrowIf(
                    currentVersion == sourceKeyVersion,
                    $"Current key version ({currentVersion}) is the same as the source key version ({sourceKeyVersion}). No re-encryption needed.");
            }
        }

        // Recover the DEK with the source KEK, then re-protect it with the target KEK (AES Key Wrap or envelope, matching WrapDek)
        byte[]? dek = null;
        try {
            dek = UnwrapDek(encryptedDek, sourceKek);
            TwoKeyDekValidation.ValidatePlaintextDekLength(dek, GetDekKeyMaterialSize(_dekEncryptionService));
            return WrapDek(dek, targetKek);
        }
        finally {
            // Securely clear the DEK from memory
            if (dek != null)
                SecurityUtilities.Clear(dek);
        }
    }

    /// <summary>
    /// Builds the AAD input for chunk authentication: the four immutable fixed-header bytes ([FormatVersion][DEKAlgorithmId][KEKAlgorithmId][DekKeyMaterialBytes]) followed by the
    /// per-stream nonce prefix. Mutable rotation fields (KeyId, KeyVersion, EncryptedDEK, DekEncoding) are excluded so headers can be rewritten in place during KEK rotation.
    /// </summary>
    private static byte[] BuildImmutableAadHeader(ReadOnlySpan<byte> immutableFixedFields, ReadOnlySpan<byte> noncePrefix)
    {
        var aad = new byte[immutableFixedFields.Length + noncePrefix.Length];
        immutableFixedFields.CopyTo(aad);
        noncePrefix.CopyTo(aad.AsSpan(immutableFixedFields.Length));
        return aad;
    }

    /// <summary>True when DEKs are AES-Key-Wrapped rather than envelope-encrypted: a symmetric AEAD KEK service plus a raw AES-sized KEK.</summary>
    private bool UsesAesKeyWrap(byte[]? kek) => _kekEncryptionService is EncryptionServiceBase && kek != null && AesKeyWrap.IsValidKekLength(kek.Length);

    /// <summary>Protects a DEK: AES Key Wrap (RFC 3394) when <see cref="UsesAesKeyWrap" />, otherwise the KEK service's single-shot envelope (e.g. RSA KEKs).</summary>
    private byte[] WrapDek(byte[] dek, byte[]? kek) => UsesAesKeyWrap(kek) ? AesKeyWrap.Wrap(kek!, dek) : _kekEncryptionService.Encrypt(dek, null, kek);

    /// <summary>
    /// Recovers a DEK produced by <see cref="WrapDek" />. The encoding decision is deterministic given the KEK (service type + key size), so this mirrors <see cref="WrapDek" />
    /// exactly: AES Key Wrap (integrity-checked by RFC 3394's IV) for raw AES-sized KEKs, otherwise the KEK service's envelope decoder.
    /// </summary>
    private byte[] UnwrapDek(byte[] encryptedDek, byte[]? kek)
        => UsesAesKeyWrap(kek) ? AesKeyWrap.Unwrap(kek!, encryptedDek) : _kekEncryptionService.Decrypt(encryptedDek, null, kek);

    private static int GetDekKeyMaterialSize(IEncryptionService dekEncryptionService)
    {
        if (dekEncryptionService is ISymmetricKeyMaterialSize s)
            return s.RequiredKeyBytes;

        return 32;
    }

    private static EncryptionAlgorithm? DetermineAlgorithm(IEncryptionService? encryptionService) => EncryptionAlgorithmDiscovery.FromEncryptionService(encryptionService);

    /// <summary>Gets the algorithm ID from an encryption service.</summary>
    private static byte GetAlgorithmIdFromService(IEncryptionService service)
    {
        if (service is IEncryptionAlgorithmProvider provider)
            return (byte)provider.AlgorithmKind;

        throw new InvalidOperationException("Cannot determine algorithm ID from encryption service. Service must implement IEncryptionAlgorithmProvider.");
    }
}