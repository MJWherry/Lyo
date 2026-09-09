using System.Buffers;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Lyo.Common.Core.Extensions;
using Lyo.Encryption.Exceptions;
using Lyo.Encryption.Models;
using Lyo.Encryption.Streaming;
using Lyo.Exceptions;
using Lyo.KeyStore;
using Lyo.Streams;

namespace Lyo.Encryption;

/// <summary>
/// Shared helpers for encryption services. Implements IEncryptionService and supplies default string, stream, and
/// file methods. Thread-safe: concurrent calls on one instance are safe because each call owns its cryptographic context (nonce, key
/// material) and shares no mutable crypto state. If a KeyStore or other dependency is not thread-safe, synchronize at those
/// levels.
/// </summary>
/// <remarks>
/// <para>
/// <b>Nonce collision bounds and key rotation.</b> Algorithms with 96-bit (12-byte) random nonces (AES-GCM, ChaCha20-Poly1305, AES-CCM) hit the birthday bound: after
/// about 2^32 encrypts under one key, a nonce collision — catastrophic for GCM/Poly1305 — is no longer negligible. NIST SP 800-38D recommends staying
/// well below that (collision probability ≤ 2^-32, i.e. at most ~2^32 random-nonce operations per key). Each single-shot <c>Encrypt</c> and each encrypted stream consumes one
/// random nonce (streams derive per-chunk nonces from a counter, so chunk count does not matter). Rotate keys (KeyStore versioning) long before those volumes.
/// </para>
/// <para>
/// <b>High-volume workloads.</b> When one key must cover a huge message count, prefer XChaCha20-Poly1305 (192-bit nonces make random collisions negligible,
/// ~2^80 messages) or AES-SIV (nonce misuse-resistant: a repeated nonce only reveals whether two plaintexts match, never the key stream).
/// </para>
/// </remarks>
public abstract class EncryptionServiceBase : IEncryptionService, IEncryptionAlgorithmProvider, IRawAead
{
    /// <summary>KeyStore used to fetch encryption keys. Null when the service does not use a store.</summary>
    protected readonly IKeyStore? KeyStore;

    /// <summary>Options that configure this encryption service.</summary>
    protected readonly EncryptionServiceOptions Options;

    private Encoding _decryptionEncoding = Encoding.UTF8;

    private Encoding _encryptionEncoding = Encoding.UTF8;

    /// <summary>Creates an EncryptionServiceBase.</summary>
    /// <param name="options">Service options. Must not be null.</param>
    /// <param name="keyStore">Store used to fetch keys. Null when the service does not use a store.</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    protected EncryptionServiceBase(EncryptionServiceOptions options, IKeyStore? keyStore = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.FileExtension, nameof(options.FileExtension));
        Options = options;
        KeyStore = keyStore;
    }

    /// <summary>Algorithm used in stream headers and discovery; same as the stream-format algorithm byte.</summary>
    public EncryptionAlgorithm AlgorithmKind => (EncryptionAlgorithm)GetAlgorithmId();

    /// <inheritdoc />
    public string FileExtension => Options.FileExtension;

    /// <inheritdoc />
    public virtual Encoding GetEncryptionEncoding() => _encryptionEncoding;

    /// <inheritdoc />
    public virtual void SetEncryptionEncoding(Encoding encoding) => _encryptionEncoding = encoding ?? throw new ArgumentNullException(nameof(encoding));

    /// <inheritdoc />
    public virtual Encoding GetDecryptionEncoding() => _decryptionEncoding;

    /// <inheritdoc />
    public virtual void SetDecryptionEncoding(Encoding encoding) => _decryptionEncoding = encoding ?? throw new ArgumentNullException(nameof(encoding));

    /// <summary>Wire facts for this service's AEAD (nonce, tag, minimum framed size).</summary>
    protected abstract EncryptionAlgorithmInfo AlgorithmInfo { get; }

    /// <summary>
    /// Nonce (or synthetic IV) bytes written after a framed header and at the start of a raw blob. AES-SIV reports <see cref="EncryptionAlgorithmInfo.NonceSize" />
    /// as 0 and stores the 16-byte IV in this width instead.
    /// </summary>
    protected int AeadNonceSize
    {
        get
        {
            var info = AlgorithmInfo;
            return info.NonceSize > 0 ? info.NonceSize : info.TagSize;
        }
    }

    /// <summary>Separate tag bytes; <c>0</c> when the authenticator is the synthetic IV (AES-SIV).</summary>
    protected int AeadTagSize => AlgorithmInfo.NonceSize > 0 ? AlgorithmInfo.TagSize : 0;

    /// <summary>Smallest legal raw blob: nonce (or synthetic IV) plus tag, with an empty payload.</summary>
    protected int MinRawAeadSize => AeadNonceSize + AeadTagSize;

    /// <summary><see langword="false" /> when the primitive fills the nonce region (AES-SIV synthetic IV).</summary>
    protected virtual bool RandomizeNonce => AlgorithmInfo.NonceSize > 0;

    /// <inheritdoc />
    public virtual byte[] Encrypt(byte[] bytes, string? keyId = null, byte[]? key = null, byte[]? associatedData = null)
    {
        ArgumentHelpers.ThrowIfNotInRange(bytes, Options.MinInputSize, Options.MaxInputSize);
        ValidateEncryptPlaintext(bytes.Length, nameof(bytes));
        return EncryptFramed(bytes, keyId, key, associatedData);
    }

    /// <inheritdoc />
    public virtual byte[] Decrypt(byte[] encryptedBytes, string? keyId = null, byte[]? key = null, byte[]? associatedData = null)
    {
        ArgumentHelpers.ThrowIfNotInRange(encryptedBytes, AlgorithmInfo.MinSingleShotSize, Options.MaxInputSize);
        return DecryptFramed(encryptedBytes, keyId, key, associatedData);
    }

    /// <inheritdoc />
    public virtual byte[] Encrypt(ReadOnlySpan<byte> plaintext, string? keyId = null, byte[]? key = null, byte[]? associatedData = null)
    {
        ArgumentHelpers.ThrowIfNotInRange((long)plaintext.Length, Options.MinInputSize, Options.MaxInputSize, nameof(plaintext));
        ValidateEncryptPlaintext(plaintext.Length, nameof(plaintext));
        return EncryptFramed(plaintext, keyId, key, associatedData);
    }

    /// <inheritdoc />
    public virtual byte[] Decrypt(byte[] buffer, int offset, int count, string? keyId = null, byte[]? key = null, byte[]? associatedData = null)
    {
        ArgumentHelpers.ThrowIfNotInRange((long)count, AlgorithmInfo.MinSingleShotSize, Options.MaxInputSize, nameof(count));
        return DecryptFramed(buffer.AsSpan(offset, count), keyId, key, associatedData);
    }

    /// <inheritdoc />
    public virtual byte[] EncryptRaw(byte[] bytes, string? keyId = null, byte[]? key = null, byte[]? associatedData = null)
    {
        ArgumentHelpers.ThrowIfNotInRange(bytes, Options.MinInputSize, Options.MaxInputSize);
        ValidateEncryptPlaintext(bytes.Length, nameof(bytes));
        return EncryptRaw((ReadOnlySpan<byte>)bytes, keyId, key, associatedData);
    }

    /// <inheritdoc />
    public virtual byte[] EncryptRaw(ReadOnlySpan<byte> plaintext, string? keyId = null, byte[]? key = null, byte[]? associatedData = null)
    {
        ArgumentHelpers.ThrowIfNotInRange((long)plaintext.Length, Options.MinInputSize, Options.MaxInputSize, nameof(plaintext));
        ValidateEncryptPlaintext(plaintext.Length, nameof(plaintext));
        var actualKey = ResolveEncryptionKey(keyId, key, ValidateSymmetricKey, out _);
        var envelope = RawAeadEnvelope.Allocate(AeadNonceSize, AeadTagSize, plaintext.Length, RandomizeNonce);
        Seal(plaintext, actualKey, envelope.Nonce, envelope.Authenticator, envelope.Payload, envelope.Body, associatedData);
        return envelope.Buffer;
    }

    /// <inheritdoc />
    public virtual byte[] DecryptRaw(byte[] encryptedBytes, string? keyId = null, byte[]? key = null, string? keyVersion = null, byte[]? associatedData = null)
    {
        ArgumentHelpers.ThrowIfNotInRange(encryptedBytes, MinRawAeadSize, Options.MaxInputSize);
        return DecryptRaw((ReadOnlySpan<byte>)encryptedBytes, keyId, key, keyVersion, associatedData);
    }

    /// <inheritdoc />
    public virtual byte[] DecryptRaw(ReadOnlySpan<byte> encrypted, string? keyId = null, byte[]? key = null, string? keyVersion = null, byte[]? associatedData = null)
    {
        ArgumentHelpers.ThrowIfNotInRange((long)encrypted.Length, (long)MinRawAeadSize, Options.MaxInputSize, nameof(encrypted));
        var header = RawAeadEnvelope.Read(encrypted, AeadNonceSize, AeadTagSize, AeadNonceFieldName);
        var actualKey = ResolveDecryptionKey(null, keyVersion, keyId, key, ValidateSymmetricKey);
        return OpenWrapped(header.Payload(encrypted), header.Authenticator(encrypted), header.Nonce(encrypted), header.Body(encrypted), actualKey, associatedData);
    }

    /// <inheritdoc />
    public virtual byte[] EncryptString(string text, string? keyId = null, byte[]? key = null, Encoding? encoding = null)
        => Encrypt((encoding ?? GetEncryptionEncoding()).GetBytes(text), keyId, key);

    /// <inheritdoc />
    public virtual string DecryptString(byte[] encryptedBytes, string? keyId = null, byte[]? key = null, Encoding? encoding = null)
        => (encoding ?? GetDecryptionEncoding()).GetString(Decrypt(encryptedBytes, keyId, key));

    /// <inheritdoc />
    /// <remarks>
    /// Writes <c>[version:1][algorithmId:1][keyIdLen:int32][keyId][keyVersionLen:int32][keyVersion][noncePrefix: NonceSize-4 bytes]</c> then compact chunk frames.
    /// Per-chunk nonces come from the header nonce prefix plus a chunk counter; the last chunk is flagged final; every chunk authenticates the full header (plus
    /// <paramref name="associatedData" />) as AAD — so header tampering, reordering, replay, and truncation all fail authentication.
    /// </remarks>
    public virtual async Task EncryptToStreamAsync(
        Stream input,
        Stream output,
        string? keyId = null,
        byte[]? key = null,
        int chunkSize = 1024 * 1024,
        byte[]? associatedData = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(input);
        ArgumentHelpers.ThrowIfNull(output);
        OperationHelpers.ThrowIfNotReadable(input, $"Stream '{nameof(input)}' must be readable.");
        OperationHelpers.ThrowIfNotWritable(output, $"Stream '{nameof(output)}' must be writable.");
        var effectiveChunkSize = chunkSize <= 0 ? StreamChunkSizeHelper.DetermineChunkSize(input) : chunkSize;

        // resolve the key once for the stream (same rule as single-shot Encrypt).
        byte[]? actualKey;
        string? keyVersion = null;
        if (key != null)
            actualKey = key;
        else if (keyId != null && KeyStore != null) {
            actualKey = await KeyStore.GetCurrentKeyAsync(keyId, ct).ConfigureAwait(false);
            OperationHelpers.ThrowIfNull(actualKey, $"No encryption key available for key ID '{keyId}'. Ensure a key is configured.");
            keyVersion = await KeyStore.GetCurrentVersionAsync(keyId, ct).ConfigureAwait(false);
        }
        else {
            OperationHelpers.ThrowIf(true, "No encryption key available. Provide either a keyId or a key parameter.");
            return;
        }

        // persist keyId/version only when single-shot would embed them (KeyStore-resolved key).
        var embedKeyInfo = key == null && keyId != null && keyVersion != null;
        var keyIdBytes = embedKeyInfo ? Encoding.UTF8.GetBytes(keyId!) : [];
        var keyVersionBytes = embedKeyInfo ? Encoding.UTF8.GetBytes(keyVersion!) : [];
        using var cryptor = CreateStreamCryptor(actualKey);

        // random per-stream nonce prefix; per-chunk nonces are prefix || counter and are never written on the wire.
        var noncePrefix = CryptographicRandom.GetBytes(cryptor.NonceSize - AeadStreamProcessor.CounterSize);
        var header = BuildStreamHeader((byte)StreamFormatVersion.V1, GetAlgorithmId(), keyIdBytes, keyVersionBytes, noncePrefix);
        var (aadNonFinal, aadFinal) = BuildChunkAads(header, associatedData);
        await output.WriteAsync(header, 0, header.Length, ct).ConfigureAwait(false);
        await AeadStreamProcessor.EncryptChunksAsync(input, output, cryptor, effectiveChunkSize, noncePrefix, aadNonFinal, aadFinal, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task DecryptToStreamAsync(
        Stream input,
        Stream output,
        string? keyId = null,
        byte[]? key = null,
        byte[]? associatedData = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(input);
        ArgumentHelpers.ThrowIfNull(output);
        OperationHelpers.ThrowIfNotReadable(input, $"Stream '{nameof(input)}' must be readable.");
        OperationHelpers.ThrowIfNotWritable(output, $"Stream '{nameof(output)}' must be writable.");

        // read the one-time header: [version:1][algorithmId:1][keyIdLen:int32][keyId][keyVersionLen:int32][keyVersion][noncePrefix].
        var fixedHeader = ArrayPool<byte>.Shared.Rent(2);
        byte formatVersion;
        byte[] keyIdBytes;
        byte[] keyVersionBytes;
        try {
            if (await AeadChunkCodec.ReadAtLeastAsync(input, fixedHeader, 2, ct).ConfigureAwait(false) != 2)
                throw new InvalidDataException("Invalid encrypted stream format: insufficient data for header.");

            formatVersion = fixedHeader[0];
            if (formatVersion != (byte)StreamFormatVersion.V1)
                throw new InvalidDataException($"Unsupported stream format version: {formatVersion}. Supported version: {(byte)StreamFormatVersion.V1}.");

            var algorithmId = fixedHeader[1];
            var expectedAlgorithmId = GetAlgorithmId();
            if (algorithmId != expectedAlgorithmId) {
                throw new InvalidDataException(
                    $"Stream algorithm ID mismatch. Expected {expectedAlgorithmId} ({(EncryptionAlgorithm)expectedAlgorithmId}), got {algorithmId} ({(EncryptionAlgorithm)algorithmId}).");
            }

            keyIdBytes = await ReadHeaderBytesAsync(input, ct).ConfigureAwait(false);
            keyVersionBytes = await ReadHeaderBytesAsync(input, ct).ConfigureAwait(false);
        }
        finally {
            ArrayPool<byte>.Shared.Return(fixedHeader);
        }

        var headerKeyId = keyIdBytes.Length > 0 ? Encoding.UTF8.GetString(keyIdBytes) : null;
        var headerKeyVersion = keyVersionBytes.Length > 0 ? Encoding.UTF8.GetString(keyVersionBytes) : null;
        if (string.IsNullOrWhiteSpace(headerKeyVersion))
            headerKeyVersion = null;

        // resolve the key once (same rule as single-shot DecryptFromStream).
        byte[]? actualKey;
        if (key != null)
            actualKey = key;
        else {
            var actualKeyId = headerKeyId ?? keyId;
            if (actualKeyId != null && KeyStore != null) {
                if (!headerKeyVersion.IsNullOrWhitespace()) {
                    actualKey = await KeyStore.GetKeyAsync(actualKeyId, headerKeyVersion, ct);
                    OperationHelpers.ThrowIfNull(
                        actualKey, $"No decryption key available for key ID '{actualKeyId}' version {headerKeyVersion}. Ensure the key version exists in KeyStore.");
                }
                else {
                    actualKey = await KeyStore.GetCurrentKeyAsync(actualKeyId, ct);
                    OperationHelpers.ThrowIfNull(actualKey, $"No decryption key available for key ID '{actualKeyId}'. Ensure a key is configured.");
                }
            }
            else {
                OperationHelpers.ThrowIf(true, "No decryption key available. Provide either a keyId or a key parameter.");
                return;
            }
        }

        using var cryptor = CreateStreamCryptor(actualKey!);

        // read the per-stream nonce prefix, then rebuild the exact header bytes so each chunk can authenticate them as AAD.
        var noncePrefix = new byte[cryptor.NonceSize - AeadStreamProcessor.CounterSize];
        if (noncePrefix.Length > 0 && await AeadChunkCodec.ReadAtLeastAsync(input, noncePrefix, noncePrefix.Length, ct).ConfigureAwait(false) != noncePrefix.Length)
            throw new InvalidDataException("Invalid encrypted stream format: insufficient data for nonce prefix.");

        var header = BuildStreamHeader(formatVersion, GetAlgorithmId(), keyIdBytes, keyVersionBytes, noncePrefix);
        var (aadNonFinal, aadFinal) = BuildChunkAads(header, associatedData);
        await AeadStreamProcessor.DecryptChunksAsync(input, output, cryptor, noncePrefix, aadNonFinal, aadFinal, ct).ConfigureAwait(false);
    }

    // IEncryptionService file methods
    /// <inheritdoc />
    public virtual async Task EncryptToFileAsync(byte[] data, string outputPath, string? keyId = null, byte[]? key = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputPath);
        using var inputStream = new MemoryStream(data);
        using var outputStream = File.Create(outputPath);
        await EncryptToStreamAsync(inputStream, outputStream, keyId, key, ct: ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task EncryptToFileAsync(
        Stream input,
        string outputPath,
        string? keyId = null,
        byte[]? key = null,
        int chunkSize = 1024 * 1024,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(input);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentHelpers.ThrowIfNegative(chunkSize);
        using var outputStream = File.Create(outputPath);
        await EncryptToStreamAsync(input, outputStream, keyId, key, chunkSize, ct: ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task<byte[]> DecryptFromFileAsync(string inputPath, string? keyId = null, byte[]? key = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(inputPath);
        using var inputStream = File.OpenRead(inputPath);

        // pre-size to ciphertext length (plaintext is always smaller: header + per-chunk tag overhead),
        // skipping the grow-and-copy of an unsized MemoryStream. ToArray still does one exact-size copy.
        var capacity = (int)Math.Min(inputStream.Length, int.MaxValue);
        using var outputStream = new MemoryStream(capacity);
        await DecryptToStreamAsync(inputStream, outputStream, keyId, key, ct: ct).ConfigureAwait(false);
        return outputStream.ToArray();
    }

    /// <summary>Builds the full stream header (<c>[version][algorithmId][keyIdLen][keyId][keyVersionLen][keyVersion][noncePrefix]</c>) as one buffer.</summary>
    internal static byte[] BuildStreamHeader(byte formatVersion, byte algorithmId, byte[] keyIdBytes, byte[] keyVersionBytes, byte[] noncePrefix)
    {
        var header = new byte[1 + 1 + 4 + keyIdBytes.Length + 4 + keyVersionBytes.Length + noncePrefix.Length];
        header[0] = formatVersion;
        header[1] = algorithmId;
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(2, 4), keyIdBytes.Length);
        keyIdBytes.CopyTo(header.AsSpan(6));
        var offset = 6 + keyIdBytes.Length;
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(offset, 4), keyVersionBytes.Length);
        keyVersionBytes.CopyTo(header.AsSpan(offset + 4));
        noncePrefix.CopyTo(header.AsSpan(offset + 4 + keyVersionBytes.Length));
        return header;
    }

    /// <summary>
    /// Builds the two per-chunk AAD variants: <c>header || finalFlagByte (0/1) || callerAssociatedData</c>. Putting the final-flag byte in AAD stops an
    /// attacker from clearing or setting the (plaintext) final marker on an existing chunk.
    /// </summary>
    internal static (byte[] AadNonFinal, byte[] AadFinal) BuildChunkAads(byte[] header, byte[]? associatedData)
    {
        var userLength = associatedData?.Length ?? 0;
        var aadNonFinal = new byte[header.Length + 1 + userLength];
        header.CopyTo(aadNonFinal.AsSpan());
        aadNonFinal[header.Length] = 0;
        associatedData?.CopyTo(aadNonFinal.AsSpan(header.Length + 1));
        var aadFinal = (byte[])aadNonFinal.Clone();
        aadFinal[header.Length] = 1;
        return (aadNonFinal, aadFinal);
    }

    /// <summary>
    /// Creates a per-stream AEAD cipher bound to <paramref name="key" /> for one streaming call. Implementations check key length and throw if it is
    /// invalid. Powers the low-allocation compact frame used by <see cref="EncryptToStreamAsync" />/<see cref="DecryptToStreamAsync" />.
    /// </summary>
    public abstract IAeadStreamCryptor CreateStreamCryptor(ReadOnlySpan<byte> key);

    /// <summary>Algorithm id for this service, written in the stream-format header for versioning and compatibility.</summary>
    protected virtual byte GetAlgorithmId() => 0; // Default, override in derived classes

    /// <summary>Field name used when a raw or framed blob is short of nonce/IV bytes.</summary>
    protected string AeadNonceFieldName => AlgorithmInfo.NonceSize > 0 ? "nonce" : "synthetic IV";

    /// <summary>Rejects plaintext the algorithm cannot seal in one shot (AES-CCM packet cap). No-op for the other AEADs.</summary>
    protected virtual void ValidateEncryptPlaintext(int length, string paramName) { }

    /// <summary>Algorithm-specific length check applied to a resolved key before any AEAD call.</summary>
    /// <param name="key">Key material chosen by <see cref="ResolveEncryptionKey" /> or <see cref="ResolveDecryptionKey" />.</param>
    protected abstract void ValidateSymmetricKey(byte[] key);

    /// <summary>
    /// Writes the AEAD ciphertext into <paramref name="payload" /> and the tag into <paramref name="authenticator" />. Combined-output algorithms (AES-SIV)
    /// write RFC 5297 <c>SIV || ciphertext</c> into <paramref name="body" /> instead.
    /// </summary>
    protected abstract void Seal(
        ReadOnlySpan<byte> plaintext,
        byte[] key,
        Span<byte> nonce,
        Span<byte> authenticator,
        Span<byte> payload,
        Span<byte> body,
        byte[]? associatedData);

    /// <summary>
    /// Decrypts one AEAD message into <paramref name="plaintext" />. Combined-input algorithms (AES-SIV) read RFC 5297 <c>SIV || ciphertext</c> from
    /// <paramref name="body" />.
    /// </summary>
    protected abstract void Open(
        ReadOnlySpan<byte> ciphertext,
        ReadOnlySpan<byte> authenticator,
        ReadOnlySpan<byte> nonce,
        ReadOnlySpan<byte> body,
        byte[] key,
        Span<byte> plaintext,
        byte[]? associatedData);

    /// <summary>Decrypts a buffer slice. Override to skip a copy when the implementation can decrypt from the buffer in place.</summary>
    protected virtual byte[] DecryptChunk(byte[] buffer, int offset, int count, string? keyId, byte[]? key, byte[]? associatedData = null)
    {
        ArgumentHelpers.ThrowIfNotInRange((long)count, AlgorithmInfo.MinSingleShotSize, Options.MaxInputSize, nameof(count));
        return DecryptFramed(buffer.AsSpan(offset, count), keyId, key, associatedData);
    }

    private byte[] EncryptFramed(ReadOnlySpan<byte> plaintext, string? keyId, byte[]? key, byte[]? associatedData)
    {
        var actualKey = ResolveEncryptionKey(keyId, key, ValidateSymmetricKey, out var keyVersion);
        var envelope = SingleShotEnvelope.Allocate(
            Options.CurrentFormatVersion ?? (byte)StreamFormatVersion.V1, keyId, keyVersion, AeadNonceSize, AeadTagSize, plaintext.Length, RandomizeNonce);
        Seal(plaintext, actualKey, envelope.Nonce, envelope.Authenticator, envelope.Payload, envelope.Body, associatedData);
        return envelope.Buffer;
    }

    private byte[] DecryptFramed(ReadOnlySpan<byte> encrypted, string? keyId, byte[]? key, byte[]? associatedData)
    {
        var header = SingleShotEnvelope.Read(
            encrypted, Options.CurrentFormatVersion ?? (byte)StreamFormatVersion.V1, AeadNonceSize, AeadTagSize, AeadNonceFieldName);
        var actualKey = ResolveDecryptionKey(header.KeyId, header.KeyVersion, keyId, key, ValidateSymmetricKey);
        return OpenWrapped(header.Payload(encrypted), header.Authenticator(encrypted), header.Nonce(encrypted), header.Body(encrypted), actualKey, associatedData);
    }

    private byte[] OpenWrapped(
        ReadOnlySpan<byte> ciphertext,
        ReadOnlySpan<byte> authenticator,
        ReadOnlySpan<byte> nonce,
        ReadOnlySpan<byte> body,
        byte[] key,
        byte[]? associatedData)
    {
        var plaintext = new byte[ciphertext.Length];
        try {
            Open(ciphertext, authenticator, nonce, body, key, plaintext, associatedData);
            return plaintext;
        }
#if NET10_0_OR_GREATER
        catch (AuthenticationTagMismatchException ex) {
            throw new DecryptionFailedException("Decryption failed due to authentication tag mismatch. Possible causes: wrong key, corrupted data, or tampered data.", ex);
        }
#endif
        catch (CryptographicException ex) {
            throw new DecryptionFailedException("Decryption failed. Possible causes: wrong key, corrupted data, or authentication failure.", ex);
        }
    }

    /// <summary>Reads a <c>[length:int32][utf8 bytes]</c> field (length capped at 1024). Returns raw bytes (maybe empty) so V2 header AAD can be rebuilt exactly.</summary>
    private static async Task<byte[]> ReadHeaderBytesAsync(Stream input, CancellationToken ct)
    {
        var lengthBuffer = ArrayPool<byte>.Shared.Rent(4);
        try {
            if (await AeadChunkCodec.ReadAtLeastAsync(input, lengthBuffer, 4, ct).ConfigureAwait(false) != 4)
                throw new InvalidDataException("Invalid encrypted stream format: insufficient data for header string length.");

            var length = BinaryPrimitives.ReadInt32LittleEndian(lengthBuffer);
            if (length < 0 || length > 1024)
                throw new InvalidDataException($"Invalid header string length: {length}. Maximum allowed: 1024 bytes.");

            if (length == 0)
                return [];

            var value = new byte[length];
            if (await AeadChunkCodec.ReadAtLeastAsync(input, value, length, ct).ConfigureAwait(false) != length)
                throw new InvalidDataException("Invalid encrypted stream format: header string truncated.");

            return value;
        }
        finally {
            ArrayPool<byte>.Shared.Return(lengthBuffer);
        }
    }

    /// <summary>Byte count of a <see cref="BinaryWriter.Write(string)" /> payload (7-bit UTF-8 length + bytes). Single-shot framing uses this to pre-size output buffers.</summary>
    protected static int GetBinaryWriterStringByteCount(string value) => BinaryWriterString.GetByteCount(value);

    /// <summary>Writes a <see cref="BinaryWriter.Write(string)" />-compatible string into <paramref name="destination" />. Returns bytes written.</summary>
    protected static int WriteBinaryWriterString(Span<byte> destination, string value) => BinaryWriterString.Write(destination, value);

    /// <summary>Reads a <see cref="BinaryReader.ReadString" />-compatible string from <paramref name="source" />.</summary>
    protected static string ReadBinaryWriterString(ReadOnlySpan<byte> source, out int bytesConsumed) => BinaryWriterString.Read(source, out bytesConsumed);

    /// <summary>
    /// Picks the encrypt key: an explicit <paramref name="key" /> wins; otherwise the current key for <paramref name="keyId" /> is loaded from the
    /// <see cref="KeyStore" /> together with its version so the envelope can record which version produced it.
    /// </summary>
    /// <param name="keyId">Id to look up when <paramref name="key" /> is null.</param>
    /// <param name="key">Caller key, or null to use the store.</param>
    /// <param name="validateKeyLength">Algorithm-specific length check applied to the chosen key.</param>
    /// <param name="keyVersion">Resolved key version, or null when the caller supplied the key.</param>
    protected byte[] ResolveEncryptionKey(string? keyId, byte[]? key, Action<byte[]> validateKeyLength, out string? keyVersion)
    {
        keyVersion = null;
        if (key != null) {
            validateKeyLength(key);
            return key;
        }

        OperationHelpers.ThrowIf(keyId == null || KeyStore == null, "No encryption key available. Provide either a keyId or a key parameter.");
        var resolved = KeyStore!.GetCurrentKey(keyId!);
        OperationHelpers.ThrowIfNull(resolved, $"No encryption key available for key ID '{keyId}'. Ensure a key is configured.");
        validateKeyLength(resolved!);
        keyVersion = KeyStore.GetCurrentVersion(keyId!);
        return resolved!;
    }

    /// <summary>
    /// Picks the decrypt key: an explicit <paramref name="key" /> wins; otherwise the envelope key id beats
    /// <paramref name="fallbackKeyId" />, and a recorded version pins that exact key so rotated keys still open old data.
    /// </summary>
    /// <param name="headerKeyId">Key id from the envelope, or null when it has none.</param>
    /// <param name="headerKeyVersion">Key version from the envelope, or null to use the current version.</param>
    /// <param name="fallbackKeyId">Caller key id, used only when the envelope has none.</param>
    /// <param name="key">Caller key, or null to use the store.</param>
    /// <param name="validateKeyLength">Algorithm-specific length check applied to the chosen key.</param>
    protected byte[] ResolveDecryptionKey(string? headerKeyId, string? headerKeyVersion, string? fallbackKeyId, byte[]? key, Action<byte[]> validateKeyLength)
    {
        if (key != null) {
            validateKeyLength(key);
            return key;
        }

        var actualKeyId = headerKeyId ?? fallbackKeyId;
        OperationHelpers.ThrowIf(actualKeyId == null || KeyStore == null, "No decryption key available. Provide either a keyId or a key parameter.");
        byte[]? resolved;
        if (!string.IsNullOrWhiteSpace(headerKeyVersion)) {
            resolved = KeyStore!.GetKey(actualKeyId!, headerKeyVersion!);
            OperationHelpers.ThrowIfNull(
                resolved, $"No decryption key available for key ID '{actualKeyId}' version {headerKeyVersion}. Ensure the key version exists in KeyStore.");
        }
        else {
            resolved = KeyStore!.GetCurrentKey(actualKeyId!);
            OperationHelpers.ThrowIfNull(resolved, $"No decryption key available for key ID '{actualKeyId}'. Ensure a key is configured.");
        }

        validateKeyLength(resolved!);
        return resolved!;
    }

    // file helpers
    /// <summary>
    /// Convenience: encrypts a file onto another file. Not on IEncryptionService. For the interface
    /// methods, call EncryptToFileAsync instead.
    /// </summary>
    public virtual async Task EncryptFileAsync(string inputPath, string? outputPath = null, string? keyId = null, byte[]? key = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(inputPath);
        var outputFile = string.IsNullOrEmpty(outputPath) ? inputPath + FileExtension : outputPath;
        using var inputStream = File.OpenRead(inputPath);
        using var outputStream = File.Create(outputFile);
        await EncryptToStreamAsync(inputStream, outputStream, keyId, key, ct: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Convenience: decrypts a file onto another file. Not on IEncryptionService. For the interface
    /// methods, call DecryptFromFileAsync instead.
    /// </summary>
    public virtual async Task DecryptFileAsync(string inputPath, string outputPath, string? keyId = null, byte[]? key = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(inputPath);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputPath);
        using var inputStream = File.OpenRead(inputPath);
        using var outputStream = File.Create(outputPath);
        await DecryptToStreamAsync(inputStream, outputStream, keyId, key, ct: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Convenience: encrypts a file and returns the ciphertext bytes. Not on IEncryptionService. For the interface
    /// methods, call EncryptToFileAsync instead.
    /// </summary>
    public virtual byte[] EncryptFile(string inputPath, string? keyId = null, byte[]? key = null)
    {
        ArgumentHelpers.ThrowIfFileNotFound(inputPath);
        var fileBytes = File.ReadAllBytes(inputPath);
        return Encrypt(fileBytes, keyId, key);
    }

    /// <summary>
    /// Convenience: encrypts a file synchronously onto another file. Not on IEncryptionService. For the
    /// interface methods, call EncryptToFileAsync instead.
    /// </summary>
    public virtual void EncryptToFile(string inputPath, string? outputPath = null, string? keyId = null, byte[]? key = null)
    {
        ArgumentHelpers.ThrowIfFileNotFound(inputPath);
        var encrypted = EncryptFile(inputPath, keyId, key);
        var outputFile = string.IsNullOrEmpty(outputPath) ? inputPath + FileExtension : outputPath;
        File.WriteAllBytes(outputFile, encrypted);
    }

    /// <summary>
    /// Convenience: decrypts a file and returns the plaintext bytes. Not on IEncryptionService. For the interface
    /// methods, call DecryptFromFileAsync instead.
    /// </summary>
    public virtual byte[] DecryptFile(string inputPath, string? keyId = null, byte[]? key = null)
    {
        ArgumentHelpers.ThrowIfFileNotFound(inputPath);
        var encryptedBytes = File.ReadAllBytes(inputPath);
        return Decrypt(encryptedBytes, keyId, key);
    }

    /// <summary>
    /// Convenience: decrypts a file synchronously onto another file. Not on IEncryptionService. For the
    /// interface methods, call DecryptFromFileAsync instead.
    /// </summary>
    public virtual void DecryptToFile(string inputPath, string outputPath, string? keyId = null, byte[]? key = null)
    {
        ArgumentHelpers.ThrowIfFileNotFound(inputPath);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputPath);
        var decrypted = DecryptFile(inputPath, keyId, key);
        File.WriteAllBytes(outputPath, decrypted);
    }
}