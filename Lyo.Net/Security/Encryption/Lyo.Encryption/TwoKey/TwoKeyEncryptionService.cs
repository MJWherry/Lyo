using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Lyo.Common.Records;
using Lyo.Encryption.Exceptions;
using Lyo.Encryption.Security;
using Lyo.Encryption.Utilities;
using Lyo.Exceptions;
using Lyo.Keystore;
using Lyo.Streams;

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
    // Stream format V1: [FormatVersion: 1 byte][DEKAlgorithmId: 1 byte][KEKAlgorithmId: 1 byte][DekKeyMaterialBytes: 1 byte][KeyIdLength: 4 bytes][KeyId][KeyVersionLength: 4 bytes][KeyVersion][EncryptedDEKLength: 4 bytes][EncryptedDEK][Chunks...]
    private const byte CurrentFormatVersion = (byte)StreamFormatVersion.V1;

    private static int GetDekKeyMaterialSize(IEncryptionService dekEncryptionService)
    {
        if (dekEncryptionService is ISymmetricKeyMaterialSize s)
            return s.RequiredKeyBytes;
        return 32;
    }

    private readonly TDataEncryptionService _dekEncryptionService; // For encrypting data with DEK

    private readonly TKeyEncryptionService _kekEncryptionService; // For encrypting DEK with KEK

    private readonly IKeyStore _keyStore;

    private bool _disposed;

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

    /// <summary>Disposes of the encryption service. Note: This service doesn't hold any unmanaged resources, but implements IDisposable for interface compliance.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        // Dispose encryption services if they implement IDisposable
        if (_dekEncryptionService is IDisposable dekDisposable)
            dekDisposable.Dispose();

        if (_kekEncryptionService is IDisposable kekDisposable && !ReferenceEquals(_kekEncryptionService, _dekEncryptionService))
            kekDisposable.Dispose();

        _disposed = true;
    }

    public string FileExtension => _dekEncryptionService.FileExtension + FileTypeInfo.TwoKeyEnvelopeSuffix;

    public Encoding DefaultEncoding { get; set; } = Encoding.UTF8;

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
        ArgumentHelpers.ThrowIfNullOrEmpty(bytes, nameof(bytes));
        OperationHelpers.ThrowIf(keyId == null && kek == null, "Either keyId or kek must be provided.");

        // For very small files (< 4KB), optimize by using a single encryption operation
        // This reduces overhead from DEK generation and KEK encryption for tiny payloads
        const int smallFileThreshold = 4096;
        if (bytes.Length <= smallFileThreshold) {
            // For small files, we can optimize by batching operations
            // Still use two-key encryption but minimize allocations
            return EncryptSmall(bytes, keyId, kek);
        }

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

            // Encrypt DEK with KEK using the KEK encryption service
            var encryptedDek = _kekEncryptionService.Encrypt(dek, null, kekBytes);

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
    /// <param name="encoding">Optional encoding. If null, uses DefaultEncoding.</param>
    /// <returns>Encryption result containing encrypted data and encrypted DEK</returns>
    public TwoKeyEncryptionResult EncryptString(string text, string? keyId = null, byte[]? kek = null, Encoding? encoding = null)
        => Encrypt((encoding ?? DefaultEncoding).GetBytes(text), keyId, kek);

    public byte[] Decrypt(byte[] encryptedData, byte[] encryptedDataEncryptionKey, string? keyId = null, byte[]? kek = null, string? keyVersion = null, byte[]? salt = null)
    {
        ArgumentHelpers.ThrowIfNotInRange(encryptedData, 1, long.MaxValue, nameof(encryptedData));
        ArgumentHelpers.ThrowIfNotInRange(encryptedDataEncryptionKey, 1, long.MaxValue, nameof(encryptedDataEncryptionKey));
        byte[]? kekBytes;
        if (kek != null)
            kekBytes = kek;
        else if (keyId != null) {
            // Get KEK from keystore using keyId and optional version
            kekBytes = !string.IsNullOrWhiteSpace(keyVersion) 
                ? _keyStore.GetKey(keyId, keyVersion) 
                : _keyStore.GetCurrentKey(keyId);
        }
        else
            throw new InvalidOperationException("Either keyId or kek must be provided for decryption.");

        if (kekBytes == null) {
            var versionInfo = !string.IsNullOrWhiteSpace(keyVersion) ? $"version {keyVersion}" : "current version";
            var keyInfo = keyId != null ? $"key ID '{keyId}' " : "";
            var saltInfo = salt != null ? " Salt is available but cannot be used to derive KEK without the original password." : "";
            throw new InvalidOperationException(
                $"No Key Encryption Key available for {keyInfo}{versionInfo} in KeyStore.{saltInfo} Ensure the keystore is properly initialized with the required key.");
        }

        // Decrypt DEK using KEK encryption service
        byte[]? dek = null;
        try {
            dek = _kekEncryptionService.Decrypt(encryptedDataEncryptionKey, null, kekBytes);
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
    /// <param name="encoding">Encoding to use for decoding the decrypted bytes. If null, uses DefaultEncoding.</param>
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
        => (encoding ?? DefaultEncoding).GetString(Decrypt(encryptedData, encryptedDataEncryptionKey, keyId, kek, keyVersion, salt));

    public async Task<TwoKeyEncryptionResult> EncryptStreamAsync(Stream input, string? keyId = null, byte[]? kek = null, int chunkSize = 1024 * 1024)
    {
        ArgumentHelpers.ThrowIfNull(input, nameof(input));
        OperationHelpers.ThrowIfNotReadable(input, $"Stream '{nameof(input)}' must be readable.");
        OperationHelpers.ThrowIf(keyId == null && kek == null, "Either keyId or kek must be provided.");
        var dek = CryptographicRandom.GetBytes(GetDekKeyMaterialSize(_dekEncryptionService));
        try {
            using var encryptedDataStream = new MemoryStream();
            using var encryptedDataWriter = new BinaryWriter(encryptedDataStream);

            // Encrypt data chunks using DEK encryption service (no keyId for DEK - it's random)
            // Use buffer pool to reduce allocations
            var buffer = BufferPool.Rent(chunkSize);
            try {
                int bytesRead;
                while ((bytesRead = await input.ReadAsync(buffer, 0, Math.Min(chunkSize, buffer.Length)).ConfigureAwait(false)) > 0) {
                    // Create exact-size array for encryption
                    var chunk = new byte[bytesRead];
                    Array.Copy(buffer, 0, chunk, 0, bytesRead);
                    var encryptedChunk = _dekEncryptionService.Encrypt(chunk, null, dek);
                    encryptedDataWriter.Write(encryptedChunk.Length);
                    encryptedDataWriter.Write(encryptedChunk);
                }
            }
            finally {
                BufferPool.Return(buffer);
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

            // Encrypt the DEK using KEK encryption service
            var encryptedDek = _kekEncryptionService.Encrypt(dek, null, kekBytes);

            // Get salt from keystore metadata to include in result
            byte[]? salt = null;
            if (actualKeyId != null && !string.IsNullOrWhiteSpace(keyVersion)) {
                var keyMetadata = await _keyStore.GetKeyMetadataAsync(actualKeyId, keyVersion).ConfigureAwait(false);
                if (keyMetadata?.AdditionalData != null && keyMetadata.AdditionalData.TryGetValue("Pbkdf2Salt", out var saltBase64)) {
                    try {
                        salt = Convert.FromBase64String(saltBase64);
                    }
                    catch (FormatException) {
                        // Invalid base64, salt will remain null
                    }
                }
            }

            return new(encryptedDataStream.ToArray(), encryptedDek, actualKeyId ?? "", keyVersion ?? "", salt, (byte)GetDekKeyMaterialSize(_dekEncryptionService));
        }
        finally {
            // Securely clear the DEK from memory
            SecurityUtilities.Clear(dek);
        }
    }

    public async Task DecryptStreamAsync(TwoKeyEncryptionResult result, Stream output, string? keyId = null, byte[]? kek = null)
    {
        ArgumentHelpers.ThrowIfNull(result, nameof(result));
        ArgumentHelpers.ThrowIfNull(output, nameof(output));
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
            throw new InvalidOperationException(
                $"No Key Encryption Key available for ID {actualKeyId} {versionInfo} in KeyStore.{saltInfo} Ensure the keystore is properly initialized with the required key.");
        }

        // Decrypt the DEK using KEK encryption service
        byte[]? dek;
        try {
            dek = _kekEncryptionService.Decrypt(result.EncryptedDataEncryptionKey, null, kekBytes);
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

            // Get DEK and KEK algorithm IDs
            var dekAlgorithmId = GetAlgorithmIdFromService(_dekEncryptionService);
            var kekAlgorithmId = GetAlgorithmIdFromService(_kekEncryptionService);

            // Write stream format header: [FormatVersion][DEKAlgorithmId][KEKAlgorithmId][DekKeyMaterialBytes][KeyIdLength][KeyId][KeyVersionLength][KeyVersion]
            await output.WriteAsync(new[] { CurrentFormatVersion }, 0, 1, ct).ConfigureAwait(false);
            await output.WriteAsync(new[] { dekAlgorithmId }, 0, 1, ct).ConfigureAwait(false);
            await output.WriteAsync(new[] { kekAlgorithmId }, 0, 1, ct).ConfigureAwait(false);
            await output.WriteAsync(new[] { (byte)GetDekKeyMaterialSize(_dekEncryptionService) }, 0, 1, ct).ConfigureAwait(false);

            // Write keyId (UTF-8 encoded)
            var keyIdBytes = actualKeyId != null ? Encoding.UTF8.GetBytes(actualKeyId) : [];
            var keyIdLenBytes = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(keyIdLenBytes, keyIdBytes.Length);
            await output.WriteAsync(keyIdLenBytes, 0, 4, ct).ConfigureAwait(false);
            if (keyIdBytes.Length > 0)
                await output.WriteAsync(keyIdBytes, 0, keyIdBytes.Length, ct).ConfigureAwait(false);

            // Write keyVersion (UTF-8 encoded)
            var keyVersionBytes = keyVersion != null ? Encoding.UTF8.GetBytes(keyVersion) : [];
            var keyVersionLenBytes = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(keyVersionLenBytes, keyVersionBytes.Length);
            await output.WriteAsync(keyVersionLenBytes, 0, 4, ct).ConfigureAwait(false);
            if (keyVersionBytes.Length > 0)
                await output.WriteAsync(keyVersionBytes, 0, keyVersionBytes.Length, ct).ConfigureAwait(false);

            // Encrypt the DEK using KEK encryption service
            var encryptedDek = _kekEncryptionService.Encrypt(dek, null, kekBytes);
            var encryptedDekLenBytes = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(encryptedDekLenBytes, encryptedDek.Length);
            await output.WriteAsync(encryptedDekLenBytes, 0, 4, ct).ConfigureAwait(false);
            await output.WriteAsync(encryptedDek, 0, encryptedDek.Length, ct).ConfigureAwait(false);

            // Encrypt and write data chunks using DEK encryption service
            // Use buffer pool to reduce allocations
            var effectiveChunkSize = chunkSize <= 0 ? StreamChunkSizeHelper.DetermineChunkSize(input) : chunkSize;
            var buffer = BufferPool.Rent(effectiveChunkSize);
            try {
                int bytesRead;
                while ((bytesRead = await input.ReadAsync(buffer, 0, Math.Min(effectiveChunkSize, buffer.Length), ct).ConfigureAwait(false)) > 0) {
                    ct.ThrowIfCancellationRequested();
                    var encryptedChunk = _dekEncryptionService.Encrypt(buffer.AsSpan(0, bytesRead), null, dek);
                    var chunkLenBytes = new byte[4];
                    BinaryPrimitives.WriteInt32LittleEndian(chunkLenBytes, encryptedChunk.Length);
                    await output.WriteAsync(chunkLenBytes, 0, 4, ct).ConfigureAwait(false);
                    await output.WriteAsync(encryptedChunk, 0, encryptedChunk.Length, ct).ConfigureAwait(false);
                }
            }
            finally {
                BufferPool.Return(buffer);
            }
        }
        finally {
            // Securely clear the DEK from memory
            SecurityUtilities.Clear(dek);
        }
    }

    public async Task DecryptToStreamAsync(Stream input, Stream output, string? keyId = null, byte[]? kek = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(input, nameof(input));
        ArgumentHelpers.ThrowIfNull(output, nameof(output));
        OperationHelpers.ThrowIfNotReadable(input, $"Stream '{nameof(input)}' must be readable.");
        OperationHelpers.ThrowIfNotWritable(output, $"Stream '{nameof(output)}' must be writable.");
        // Read stream format header: [FormatVersion][DEKAlgorithmId][KEKAlgorithmId][DekKeyMaterialBytes][KeyIdLength][KeyId][KeyVersionLength][KeyVersion]
        // Use buffer pool for small buffers
        var versionBuffer = BufferPool.RentExact(1, true);
        try {
            if (await input.ReadAsync(versionBuffer, 0, 1, ct).ConfigureAwait(false) != 1)
                throw new EndOfStreamException("Unexpected end of stream while reading format version.");

            var rawFormat = versionBuffer[0];
            if (rawFormat != (byte)StreamFormatVersion.V1)
                throw new NotSupportedException($"Unsupported stream format version: {rawFormat}. Supported: {(byte)StreamFormatVersion.V1}.");

            // Read algorithm IDs - reuse versionBuffer for reading single bytes
            if (await input.ReadAsync(versionBuffer, 0, 1, ct).ConfigureAwait(false) != 1)
                throw new EndOfStreamException("Unexpected end of stream while reading DEK algorithm ID.");

            var dekAlgorithmId = versionBuffer[0];
            if (await input.ReadAsync(versionBuffer, 0, 1, ct).ConfigureAwait(false) != 1)
                throw new EndOfStreamException("Unexpected end of stream while reading KEK algorithm ID.");

            var kekAlgorithmId = versionBuffer[0];

            if (await input.ReadAsync(versionBuffer, 0, 1, ct).ConfigureAwait(false) != 1)
                throw new EndOfStreamException("Unexpected end of stream while reading DEK key material length.");

            var dekKeyMaterialBytes = versionBuffer[0];

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

            // Read keyId
            var keyIdLengthBuffer = BufferPool.RentExact(4, true);
            try {
                if (await input.ReadAsync(keyIdLengthBuffer, 0, 4, ct).ConfigureAwait(false) != 4)
                    throw new EndOfStreamException("Unexpected end of stream while reading key ID length.");

                var keyIdLength = BinaryPrimitives.ReadInt32LittleEndian(keyIdLengthBuffer);
                string? streamKeyId = null;
                if (keyIdLength > 0) {
                    if (keyIdLength > 1024) // Reasonable limit
                        throw new InvalidDataException($"Invalid key ID length: {keyIdLength}. Maximum allowed: 1024 bytes.");

                    var keyIdBytes = BufferPool.Rent(keyIdLength);
                    try {
                        var keyIdBytesRead = 0;
                        while (keyIdBytesRead < keyIdLength) {
                            ct.ThrowIfCancellationRequested();
                            var bytesRead = await input.ReadAsync(keyIdBytes, keyIdBytesRead, keyIdLength - keyIdBytesRead, ct).ConfigureAwait(false);
                            if (bytesRead == 0)
                                throw new EndOfStreamException("Unexpected end of stream while reading key ID.");

                            keyIdBytesRead += bytesRead;
                        }

                        // Create exact-size array for string conversion
                        var keyIdBytesExact = new byte[keyIdLength];
                        Array.Copy(keyIdBytes, 0, keyIdBytesExact, 0, keyIdLength);
                        streamKeyId = Encoding.UTF8.GetString(keyIdBytesExact);
                    }
                    finally {
                        BufferPool.Return(keyIdBytes);
                    }
                }

                // Read keyVersion length and data
                var keyVersionLengthBuffer = BufferPool.RentExact(4, true);
                string? keyVersion = null;
                try {
                    if (await input.ReadAsync(keyVersionLengthBuffer, 0, 4, ct).ConfigureAwait(false) != 4)
                        throw new EndOfStreamException("Unexpected end of stream while reading key version length.");

                    var keyVersionLength = BinaryPrimitives.ReadInt32LittleEndian(keyVersionLengthBuffer);
                    if (keyVersionLength < 0 || keyVersionLength > 1024)
                        throw new InvalidDataException($"Invalid key version length: {keyVersionLength}. Maximum allowed: 1024 bytes.");

                    if (keyVersionLength > 0) {
                        var keyVersionBytes = BufferPool.Rent(keyVersionLength);
                        try {
                            var keyVersionBytesRead = 0;
                            while (keyVersionBytesRead < keyVersionLength) {
                                ct.ThrowIfCancellationRequested();
                                var bytesRead = await input.ReadAsync(keyVersionBytes, keyVersionBytesRead, keyVersionLength - keyVersionBytesRead, ct).ConfigureAwait(false);
                                if (bytesRead == 0)
                                    throw new EndOfStreamException("Unexpected end of stream while reading key version.");

                                keyVersionBytesRead += bytesRead;
                            }

                            // Create exact-size array for string conversion
                            var keyVersionBytesExact = new byte[keyVersionLength];
                            Array.Copy(keyVersionBytes, 0, keyVersionBytesExact, 0, keyVersionLength);
                            keyVersion = Encoding.UTF8.GetString(keyVersionBytesExact);
                        }
                        finally {
                            BufferPool.Return(keyVersionBytes);
                        }
                    }

                    // Use keyId from parameter if provided, otherwise use keyId from stream
                    var actualKeyId = keyId ?? streamKeyId;

                    // Read the encrypted DEK length and data
                    var dekLengthBuffer = BufferPool.RentExact(4, true);
                    try {
                        if (await input.ReadAsync(dekLengthBuffer, 0, 4, ct).ConfigureAwait(false) != 4)
                            throw new EndOfStreamException("Unexpected end of stream while reading encrypted DEK length.");

                        var encryptedDekLength = BinaryPrimitives.ReadInt32LittleEndian(dekLengthBuffer);
                        var encryptedDek = BufferPool.Rent(encryptedDekLength);
                        byte[]? encryptedDekExact;
                        try {
                            var dekBytesRead = 0;
                            while (dekBytesRead < encryptedDekLength) {
                                ct.ThrowIfCancellationRequested();
                                var bytesRead = await input.ReadAsync(encryptedDek, dekBytesRead, encryptedDekLength - dekBytesRead, ct).ConfigureAwait(false);
                                if (bytesRead == 0)
                                    throw new EndOfStreamException("Unexpected end of stream while reading encrypted DEK.");

                                dekBytesRead += bytesRead;
                            }

                            // Create exact-size array for decryption
                            encryptedDekExact = new byte[encryptedDekLength];
                            Array.Copy(encryptedDek, 0, encryptedDekExact, 0, encryptedDekLength);
                        }
                        finally {
                            BufferPool.Return(encryptedDek);
                        }

                        // Decrypt the DEK - use keyId and version from stream for proper key rotation support
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
                            throw new InvalidOperationException(
                                $"No Key Encryption Key available for {keyInfo}{versionInfo} in KeyStore.{saltInfo} Ensure the keystore is properly initialized with the required key.");
                        }

                        // Decrypt DEK using KEK encryption service
                        byte[]? dek;
                        try {
                            dek = _kekEncryptionService.Decrypt(encryptedDekExact, null, kekBytes);
                            TwoKeyDekValidation.ValidatePlaintextDekLength(dek, dekKeyMaterialBytes);
                        }
                        catch (DecryptionFailedException) {
                            throw;
                        }
                        catch (InvalidDataException) {
                            throw;
                        }
                        catch (Exception ex) {
                            throw new DecryptionFailedException(
                                "Failed to decrypt Data Encryption Key. Possible causes: wrong KEK, corrupted data, or authentication failure.", ex);
                        }

                        try {
                            // Maximum allowed encrypted chunk size (200 MB) to prevent denial-of-service attacks
                            const int maxEncryptedChunkSize = 200 * 1024 * 1024; // 200 MB

                            // Read and decrypt data chunks using DEK encryption service
                            // Use buffer pool for length buffer and encrypted chunks
                            var lengthBuffer = BufferPool.RentExact(4, true);
                            try {
                                while (await input.ReadAsync(lengthBuffer, 0, 4, ct).ConfigureAwait(false) == 4) {
                                    ct.ThrowIfCancellationRequested();
                                    var chunkLength = BinaryPrimitives.ReadInt32LittleEndian(lengthBuffer);

                                    // Validate chunk length to prevent DoS attacks
                                    if (chunkLength <= 0)
                                        throw new InvalidDataException($"Invalid chunk length: {chunkLength}. Chunk length must be positive.");

                                    if (chunkLength > maxEncryptedChunkSize) {
                                        throw new InvalidDataException(
                                            $"Invalid chunk length: {chunkLength} bytes. Maximum allowed: {maxEncryptedChunkSize} bytes ({maxEncryptedChunkSize / (1024 * 1024)} MB).");
                                    }

                                    // Check if stream has enough remaining data for this chunk
                                    if (input.CanSeek) {
                                        var remainingBytes = input.Length - input.Position;
                                        if (remainingBytes < chunkLength) {
                                            throw new InvalidDataException(
                                                $"Invalid encrypted data format: chunk length ({chunkLength} bytes) exceeds remaining stream size ({remainingBytes} bytes).");
                                        }
                                    }

                                    // Use buffer pool for encrypted chunk
                                    var encryptedChunk = BufferPool.Rent(chunkLength);
                                    try {
                                        var chunkBytesRead = 0;
                                        while (chunkBytesRead < chunkLength) {
                                            ct.ThrowIfCancellationRequested();
                                            var bytesRead = await input.ReadAsync(encryptedChunk, chunkBytesRead, chunkLength - chunkBytesRead, ct).ConfigureAwait(false);
                                            if (bytesRead == 0)
                                                throw new EndOfStreamException("Unexpected end of stream while reading encrypted chunk.");

                                            chunkBytesRead += bytesRead;
                                        }

                                        byte[] decryptedChunk;
                                        try {
                                            decryptedChunk = _dekEncryptionService.Decrypt(encryptedChunk, 0, chunkLength, null, dek);
                                        }
                                        catch (DecryptionFailedException) {
                                            throw;
                                        }
                                        catch (Exception ex) {
                                            throw new DecryptionFailedException(
                                                "Failed to decrypt data chunk. Possible causes: wrong key, corrupted data, or authentication failure.", ex);
                                        }

                                        await output.WriteAsync(decryptedChunk, 0, decryptedChunk.Length, ct).ConfigureAwait(false);
                                    }
                                    finally {
                                        BufferPool.Return(encryptedChunk);
                                    }
                                }
                            }
                            finally {
                                BufferPool.Return(lengthBuffer);
                            }
                        }
                        finally {
                            // Securely clear the DEK from memory after decryption
                            SecurityUtilities.Clear(dek);
                        }
                    }
                    finally {
                        BufferPool.Return(dekLengthBuffer);
                    }
                }
                finally {
                    BufferPool.Return(keyVersionLengthBuffer);
                }
            }
            finally {
                BufferPool.Return(keyIdLengthBuffer);
            }
        }
        finally {
            BufferPool.Return(versionBuffer);
        }
    }

    public async Task EncryptToFileAsync(byte[] data, string outputPath, string? keyId = null, byte[]? kek = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputPath, nameof(outputPath));
        using var inputStream = new MemoryStream(data);
        using var outputStream = File.Create(outputPath);
        await EncryptToStreamAsync(inputStream, outputStream, keyId, kek, ct: ct).ConfigureAwait(false);
    }

    public async Task EncryptToFileAsync(Stream input, string outputPath, string? keyId = null, byte[]? kek = null, int chunkSize = 1024 * 1024, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputPath, nameof(outputPath));
        using var outputStream = File.Create(outputPath);
        await EncryptToStreamAsync(input, outputStream, keyId, kek, chunkSize, ct).ConfigureAwait(false);
    }

    public async Task<byte[]> DecryptFromFileAsync(string inputPath, string? keyId = null, byte[]? kek = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(inputPath, nameof(inputPath));
        ArgumentHelpers.ThrowIfFileNotFound(inputPath, nameof(inputPath));
        using var inputStream = File.OpenRead(inputPath);
        using var outputStream = new MemoryStream();
        await DecryptToStreamAsync(inputStream, outputStream, keyId, kek, ct).ConfigureAwait(false);
        return outputStream.ToArray();
    }

    public byte[] ReEncryptDek(byte[] encryptedDek, string sourceKeyId, string sourceKeyVersion, string? targetKeyId = null, string? targetKeyVersion = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sourceKeyId, nameof(sourceKeyId));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sourceKeyVersion, nameof(sourceKeyVersion));

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

        // Decrypt the DEK using the source KEK
        byte[]? dek = null;
        try {
            dek = _kekEncryptionService.Decrypt(encryptedDek, null, sourceKek);
            TwoKeyDekValidation.ValidatePlaintextDekLength(dek, GetDekKeyMaterialSize(_dekEncryptionService));

            // Encrypt the DEK using the target KEK
            return _kekEncryptionService.Encrypt(dek, null, targetKek);
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
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sourceKeyId, nameof(sourceKeyId));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(sourceKeyVersion, nameof(sourceKeyVersion));

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

        // Decrypt the DEK using the source KEK
        byte[]? dek = null;
        try {
            dek = _kekEncryptionService.Decrypt(encryptedDek, null, sourceKek);
            TwoKeyDekValidation.ValidatePlaintextDekLength(dek, GetDekKeyMaterialSize(_dekEncryptionService));

            // Encrypt the DEK using the target KEK
            return _kekEncryptionService.Encrypt(dek, null, targetKek);
        }
        finally {
            // Securely clear the DEK from memory
            if (dek != null)
                SecurityUtilities.Clear(dek);
        }
    }

    private static EncryptionAlgorithm? DetermineAlgorithm(IEncryptionService? encryptionService)
        => EncryptionAlgorithmDiscovery.FromEncryptionService(encryptionService);

    /// <summary>Optimized encryption path for small files to reduce overhead.</summary>
    private TwoKeyEncryptionResult EncryptSmall(byte[] bytes, string? keyId, byte[]? kek)
    {
        var dek = CryptographicRandom.GetBytes(GetDekKeyMaterialSize(_dekEncryptionService));
        try {
            // Encrypt data with DEK
            var encryptedData = _dekEncryptionService.Encrypt(bytes, null, dek);

            // Get KEK
            byte[]? kekBytes = null;
            string? actualKeyId = null;
            string? keyVersion = null;
            if (kek != null)
                kekBytes = kek;
            else if (keyId != null) {
                actualKeyId = keyId;
                kekBytes = _keyStore.GetCurrentKey(keyId);
                OperationHelpers.ThrowIfNull(kekBytes, $"No Key Encryption Key available for key ID '{keyId}'. Ensure a key is configured.");
                keyVersion = _keyStore.GetCurrentVersion(keyId) ?? "";
            }

            // Encrypt DEK with KEK
            var encryptedDek = _kekEncryptionService.Encrypt(dek, null, kekBytes);

            // Get salt from keystore metadata
            byte[]? salt = null;
            if (actualKeyId != null && !string.IsNullOrWhiteSpace(keyVersion)) {
                var keyMetadata = _keyStore.GetKeyMetadata(actualKeyId, keyVersion);
                if (keyMetadata?.AdditionalData != null && keyMetadata.AdditionalData.TryGetValue("Pbkdf2Salt", out var saltBase64)) {
                    try {
                        salt = Convert.FromBase64String(saltBase64);
                    }
                    catch (FormatException) {
                        // Invalid base64, salt will remain null
                    }
                }
            }

            return new(encryptedData, encryptedDek, actualKeyId ?? "", keyVersion ?? "", salt, (byte)GetDekKeyMaterialSize(_dekEncryptionService));
        }
        finally {
            SecurityUtilities.Clear(dek);
        }
    }

    /// <summary>Gets the algorithm ID from an encryption service.</summary>
    private static byte GetAlgorithmIdFromService(IEncryptionService service)
    {
        if (service is EncryptionServiceBase baseService)
            return (byte)baseService.AlgorithmKind;

        throw new InvalidOperationException("Cannot determine algorithm ID from encryption service. Service must inherit from EncryptionServiceBase.");
    }
}