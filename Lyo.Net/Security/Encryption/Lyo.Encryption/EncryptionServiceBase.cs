using System.Buffers;
using System.Buffers.Binary;
using System.Text;
using Lyo.Common.Extensions;
using Lyo.Encryption.Streaming;
using Lyo.Exceptions;
using Lyo.Keystore;
using Lyo.Streams;

namespace Lyo.Encryption;

/// <summary>
/// Abstract base class providing common helper methods for encryption services. Implements IEncryptionService and provides default implementations for string, stream, and
/// file operations. Thread-safe: Multiple threads can safely call methods concurrently on the same instance. Each method call uses its own cryptographic context (nonce, key
/// material), so there are no shared mutable state concerns. However, if using a KeyStore or other dependencies that aren't thread-safe, ensure proper synchronization at those
/// levels.
/// </summary>
/// <remarks>
/// <para>
/// <b>Nonce collision bounds and key rotation.</b> Algorithms with 96-bit (12-byte) random nonces (AES-GCM, ChaCha20-Poly1305, AES-CCM) are subject to the birthday bound: after
/// roughly 2^32 encryptions under one key the probability of a nonce collision — which is catastrophic for GCM/Poly1305 — becomes non-negligible. NIST SP 800-38D recommends staying
/// well below that (collision probability ≤ 2^-32, i.e. at most ~2^32 random-nonce operations per key). Each single-shot <c>Encrypt</c> call and each encrypted stream consumes one
/// random nonce (streams derive per-chunk nonces from a counter, so chunk count does not matter). Rotate keys (via the KeyStore key versioning) long before approaching these volumes.
/// </para>
/// <para>
/// <b>High-volume workloads.</b> When a single key must protect very large numbers of messages, prefer XChaCha20-Poly1305 (192-bit nonces make random collisions negligible,
/// ~2^80 messages) or AES-SIV (nonce misuse-resistant: a repeated nonce only reveals whether two plaintexts are identical, never the key stream).
/// </para>
/// </remarks>
public abstract class EncryptionServiceBase : IEncryptionService, IEncryptionAlgorithmProvider
{
    /// <summary>The KeyStore used for retrieving encryption keys. May be null if service doesn't use KeyStore.</summary>
    protected readonly IKeyStore? KeyStore;

    /// <summary>The options used to configure this encryption service.</summary>
    protected readonly EncryptionServiceOptions Options;

    private Encoding _decryptionEncoding = Encoding.UTF8;

    private Encoding _encryptionEncoding = Encoding.UTF8;

    /// <summary>Initializes a new instance of EncryptionServiceBase.</summary>
    /// <param name="options">The options to configure this encryption service. Must not be null.</param>
    /// <param name="keyStore">The key store to use for retrieving encryption keys. Can be null if service doesn't use KeyStore.</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    protected EncryptionServiceBase(EncryptionServiceOptions options, IKeyStore? keyStore = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.FileExtension, nameof(options.FileExtension));
        Options = options;
        KeyStore = keyStore;
    }

    /// <summary>Algorithm for stream headers and discovery; matches the stream format algorithm byte.</summary>
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

    /// <inheritdoc />
    public abstract byte[] Encrypt(byte[] bytes, string? keyId = null, byte[]? key = null, byte[]? associatedData = null);

    /// <inheritdoc />
    public abstract byte[] Decrypt(byte[] encryptedBytes, string? keyId = null, byte[]? key = null, byte[]? associatedData = null);

    /// <inheritdoc />
    public virtual byte[] Encrypt(ReadOnlySpan<byte> plaintext, string? keyId = null, byte[]? key = null, byte[]? associatedData = null)
        => Encrypt(plaintext.ToArray(), keyId, key, associatedData);

    /// <inheritdoc />
    public virtual byte[] Decrypt(byte[] buffer, int offset, int count, string? keyId = null, byte[]? key = null, byte[]? associatedData = null)
    {
        var chunk = new byte[count];
        Array.Copy(buffer, offset, chunk, 0, count);
        return Decrypt(chunk, keyId, key, associatedData);
    }

    /// <inheritdoc />
    public virtual byte[] EncryptString(string text, string? keyId = null, byte[]? key = null, Encoding? encoding = null)
        => Encrypt((encoding ?? GetEncryptionEncoding()).GetBytes(text), keyId, key);

    /// <inheritdoc />
    public virtual string DecryptString(byte[] encryptedBytes, string? keyId = null, byte[]? key = null, Encoding? encoding = null)
        => (encoding ?? GetDecryptionEncoding()).GetString(Decrypt(encryptedBytes, keyId, key));

    /// <inheritdoc />
    /// <remarks>
    /// Writes <c>[version:1][algorithmId:1][keyIdLen:int32][keyId][keyVersionLen:int32][keyVersion][noncePrefix: NonceSize-4 bytes]</c> followed by compact chunk frames.
    /// Per-chunk nonces are derived from the header nonce prefix plus a chunk counter, the last chunk carries a final-flag, and every chunk authenticates the full header (plus
    /// <paramref name="associatedData" />) as AAD — so header tampering, chunk reordering, replay, and truncation all fail authentication.
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

        // Resolve the key once for the whole stream (mirrors the single-shot Encrypt key resolution).
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

        // keyId/version are persisted exactly when the single-shot format would embed them (KeyStore-resolved key).
        var embedKeyInfo = key == null && keyId != null && keyVersion != null;
        var keyIdBytes = embedKeyInfo ? Encoding.UTF8.GetBytes(keyId!) : [];
        var keyVersionBytes = embedKeyInfo ? Encoding.UTF8.GetBytes(keyVersion!) : [];
        using var cryptor = CreateStreamCryptor(actualKey);

        // Per-stream random nonce prefix; per-chunk nonces are derived as prefix || counter and never written to the wire.
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

        // Read the one-time header: [version:1][algorithmId:1][keyIdLen:int32][keyId][keyVersionLen:int32][keyVersion][noncePrefix].
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

        // Resolve the key once (mirrors the single-shot DecryptFromStream resolution).
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

        // Read the per-stream nonce prefix, then rebuild the exact header bytes so each chunk can authenticate them as AAD.
        var noncePrefix = new byte[cryptor.NonceSize - AeadStreamProcessor.CounterSize];
        if (noncePrefix.Length > 0 && await AeadChunkCodec.ReadAtLeastAsync(input, noncePrefix, noncePrefix.Length, ct).ConfigureAwait(false) != noncePrefix.Length)
            throw new InvalidDataException("Invalid encrypted stream format: insufficient data for nonce prefix.");

        var header = BuildStreamHeader(formatVersion, GetAlgorithmId(), keyIdBytes, keyVersionBytes, noncePrefix);
        var (aadNonFinal, aadFinal) = BuildChunkAads(header, associatedData);
        await AeadStreamProcessor.DecryptChunksAsync(input, output, cryptor, noncePrefix, aadNonFinal, aadFinal, ct).ConfigureAwait(false);
    }

    // File operation methods from IEncryptionService
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

        // Pre-size to the ciphertext length (plaintext is always smaller: header + per-chunk tag overhead),
        // avoiding the repeated grow-and-copy of an unsized MemoryStream. One exact-size copy remains in ToArray.
        var capacity = (int)Math.Min(inputStream.Length, int.MaxValue);
        using var outputStream = new MemoryStream(capacity);
        await DecryptToStreamAsync(inputStream, outputStream, keyId, key, ct: ct).ConfigureAwait(false);
        return outputStream.ToArray();
    }

    /// <summary>Composes the full stream header (<c>[version][algorithmId][keyIdLen][keyId][keyVersionLen][keyVersion][noncePrefix]</c>) as a single buffer.</summary>
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
    /// Builds the two per-chunk AAD variants for a stream: <c>header || finalFlagByte (0/1) || callerAssociatedData</c>. Binding the final-flag byte into the AAD prevents an
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
    /// Creates a per-stream AEAD cipher bound to <paramref name="key" /> for the lifetime of one streaming operation. Implementations validate the key length and throw if it is
    /// invalid. Drives the allocation-reduced compact streaming frame used by <see cref="EncryptToStreamAsync" />/<see cref="DecryptToStreamAsync" />.
    /// </summary>
    public abstract IAeadStreamCryptor CreateStreamCryptor(ReadOnlySpan<byte> key);

    /// <summary>Reads a <c>[length:int32][utf8 bytes]</c> header field (length capped at 1024 bytes). Returns the raw bytes (possibly empty) so V2 header AAD can be rebuilt exactly.</summary>
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

    /// <summary>Gets the algorithm identifier for this encryption service. Used in stream format header for versioning and compatibility.</summary>
    protected virtual byte GetAlgorithmId() => 0; // Default, override in derived classes

    /// <summary>Decrypts a buffer slice. Override to avoid copying when the implementation can decrypt in-place from a buffer.</summary>
    protected virtual byte[] DecryptChunk(byte[] buffer, int offset, int count, string? keyId, byte[]? key, byte[]? associatedData = null)
    {
        var chunk = new byte[count];
        Array.Copy(buffer, offset, chunk, 0, count);
        return Decrypt(chunk, keyId, key, associatedData);
    }

    /// <summary>
    /// Byte count of a <see cref="BinaryWriter.Write(string)" /> payload (7-bit encoded UTF-8 length + UTF-8 bytes). Used by single-shot framing to pre-size output buffers.
    /// </summary>
    protected static int GetBinaryWriterStringByteCount(string value)
    {
        var utf8Len = Encoding.UTF8.GetByteCount(value);
        return Get7BitEncodedIntByteCount(utf8Len) + utf8Len;
    }

    /// <summary>Writes a <see cref="BinaryWriter.Write(string)" />-compatible string into <paramref name="destination" />. Returns bytes written.</summary>
    protected static int WriteBinaryWriterString(Span<byte> destination, string value)
    {
        var utf8 = Encoding.UTF8.GetBytes(value);
        var lenBytes = Write7BitEncodedInt(destination, utf8.Length);
        utf8.CopyTo(destination[lenBytes..]);
        return lenBytes + utf8.Length;
    }

    /// <summary>Reads a <see cref="BinaryReader.ReadString" />-compatible string from <paramref name="source" />.</summary>
    protected static string ReadBinaryWriterString(ReadOnlySpan<byte> source, out int bytesConsumed)
    {
        var utf8Len = Read7BitEncodedInt(source, out var lenBytes);
        if (utf8Len < 0 || lenBytes + utf8Len > source.Length)
            throw new InvalidDataException("Invalid encrypted data format: truncated BinaryWriter string.");

        bytesConsumed = lenBytes + utf8Len;
        return utf8Len == 0 ? string.Empty : Encoding.UTF8.GetString(source.Slice(lenBytes, utf8Len).ToArray());
    }

    private static int Get7BitEncodedIntByteCount(int value)
    {
        ArgumentHelpers.ThrowIfNegative(value);
        var count = 1;
        var v = (uint)value;
        while (v >= 0x80) {
            v >>= 7;
            count++;
        }

        return count;
    }

    private static int Write7BitEncodedInt(Span<byte> destination, int value)
    {
        ArgumentHelpers.ThrowIfNegative(value);
        var v = (uint)value;
        var written = 0;
        while (v >= 0x80) {
            destination[written++] = (byte)(v | 0x80);
            v >>= 7;
        }

        destination[written++] = (byte)v;
        return written;
    }

    private static int Read7BitEncodedInt(ReadOnlySpan<byte> source, out int bytesConsumed)
    {
        var result = 0;
        var shift = 0;
        bytesConsumed = 0;
        while (shift < 35) {
            if (bytesConsumed >= source.Length)
                throw new InvalidDataException("Invalid encrypted data format: truncated 7-bit encoded integer.");

            var b = source[bytesConsumed++];
            result |= (b & 0x7F) << shift;
            if ((b & 0x80) == 0)
                return result;

            shift += 7;
        }

        throw new InvalidDataException("Invalid encrypted data format: malformed 7-bit encoded integer.");
    }

    // File helpers
    /// <summary>
    /// Convenience method: Encrypts a file and writes it to an output file. This is a convenience method not part of IEncryptionService interface. For interface-compliant
    /// methods, use EncryptToFileAsync instead.
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
    /// Convenience method: Decrypts a file and writes it to an output file. This is a convenience method not part of IEncryptionService interface. For interface-compliant
    /// methods, use DecryptFromFileAsync instead.
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
    /// Convenience method: Encrypts a file and returns the encrypted bytes. This is a convenience method not part of IEncryptionService interface. For interface-compliant
    /// methods, use EncryptToFileAsync instead.
    /// </summary>
    public virtual byte[] EncryptFile(string inputPath, string? keyId = null, byte[]? key = null)
    {
        ArgumentHelpers.ThrowIfFileNotFound(inputPath);
        var fileBytes = File.ReadAllBytes(inputPath);
        return Encrypt(fileBytes, keyId, key);
    }

    /// <summary>
    /// Convenience method: Encrypts a file synchronously and writes it to an output file. This is a convenience method not part of IEncryptionService interface. For
    /// interface-compliant methods, use EncryptToFileAsync instead.
    /// </summary>
    public virtual void EncryptToFile(string inputPath, string? outputPath = null, string? keyId = null, byte[]? key = null)
    {
        ArgumentHelpers.ThrowIfFileNotFound(inputPath);
        var encrypted = EncryptFile(inputPath, keyId, key);
        var outputFile = string.IsNullOrEmpty(outputPath) ? inputPath + FileExtension : outputPath;
        File.WriteAllBytes(outputFile, encrypted);
    }

    /// <summary>
    /// Convenience method: Decrypts a file and returns the decrypted bytes. This is a convenience method not part of IEncryptionService interface. For interface-compliant
    /// methods, use DecryptFromFileAsync instead.
    /// </summary>
    public virtual byte[] DecryptFile(string inputPath, string? keyId = null, byte[]? key = null)
    {
        ArgumentHelpers.ThrowIfFileNotFound(inputPath);
        var encryptedBytes = File.ReadAllBytes(inputPath);
        return Decrypt(encryptedBytes, keyId, key);
    }

    /// <summary>
    /// Convenience method: Decrypts a file synchronously and writes it to an output file. This is a convenience method not part of IEncryptionService interface. For
    /// interface-compliant methods, use DecryptFromFileAsync instead.
    /// </summary>
    public virtual void DecryptToFile(string inputPath, string outputPath, string? keyId = null, byte[]? key = null)
    {
        ArgumentHelpers.ThrowIfFileNotFound(inputPath);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputPath);
        var decrypted = DecryptFile(inputPath, keyId, key);
        File.WriteAllBytes(outputPath, decrypted);
    }
}