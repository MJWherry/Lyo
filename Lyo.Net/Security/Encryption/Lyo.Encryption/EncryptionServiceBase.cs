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
    public abstract byte[] Encrypt(byte[] bytes, string? keyId = null, byte[]? key = null);

    /// <inheritdoc />
    public abstract byte[] Decrypt(byte[] encryptedBytes, string? keyId = null, byte[]? key = null);

    /// <inheritdoc />
    public virtual byte[] Encrypt(ReadOnlySpan<byte> plaintext, string? keyId = null, byte[]? key = null) => Encrypt(plaintext.ToArray(), keyId, key);

    /// <inheritdoc />
    public virtual byte[] Decrypt(byte[] buffer, int offset, int count, string? keyId = null, byte[]? key = null)
    {
        var chunk = new byte[count];
        Array.Copy(buffer, offset, chunk, 0, count);
        return Decrypt(chunk, keyId, key);
    }

    /// <inheritdoc />
    public virtual byte[] EncryptString(string text, string? keyId = null, byte[]? key = null, Encoding? encoding = null)
        => Encrypt((encoding ?? GetEncryptionEncoding()).GetBytes(text), keyId, key);

    /// <inheritdoc />
    public virtual string DecryptString(byte[] encryptedBytes, string? keyId = null, byte[]? key = null, Encoding? encoding = null)
        => (encoding ?? GetDecryptionEncoding()).GetString(Decrypt(encryptedBytes, keyId, key));

    /// <inheritdoc />
    public virtual async Task EncryptToStreamAsync(
        Stream input,
        Stream output,
        string? keyId = null,
        byte[]? key = null,
        int chunkSize = 1024 * 1024,
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
            actualKey = KeyStore.GetCurrentKey(keyId);
            OperationHelpers.ThrowIfNull(actualKey, $"No encryption key available for key ID '{keyId}'. Ensure a key is configured.");
            keyVersion = KeyStore.GetCurrentVersion(keyId);
        }
        else {
            OperationHelpers.ThrowIf(true, "No encryption key available. Provide either a keyId or a key parameter.");
            return;
        }

        // keyId/version are persisted exactly when the single-shot format would embed them (KeyStore-resolved key).
        var embedKeyInfo = key == null && keyId != null && keyVersion != null;
        var headerKeyId = embedKeyInfo ? keyId! : "";
        var headerKeyVersion = embedKeyInfo ? keyVersion! : "";

        // One-time stream header: [version:1][algorithmId:1][keyIdLen:int32][keyId][keyVersionLen:int32][keyVersion].
        var keyIdBytes = Encoding.UTF8.GetBytes(headerKeyId);
        var keyVersionBytes = Encoding.UTF8.GetBytes(headerKeyVersion);
        var formatVersion = Options.CurrentFormatVersion ?? (byte)StreamFormatVersion.V1;
        var headerLength = 1 + 1 + 4 + keyIdBytes.Length + 4 + keyVersionBytes.Length;
        var header = ArrayPool<byte>.Shared.Rent(headerLength);
        try {
            header[0] = formatVersion;
            header[1] = GetAlgorithmId();
            BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(2, 4), keyIdBytes.Length);
            keyIdBytes.CopyTo(header.AsSpan(6));
            var versionLengthOffset = 6 + keyIdBytes.Length;
            BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(versionLengthOffset, 4), keyVersionBytes.Length);
            keyVersionBytes.CopyTo(header.AsSpan(versionLengthOffset + 4));
            await output.WriteAsync(header, 0, headerLength, ct).ConfigureAwait(false);
        }
        finally {
            ArrayPool<byte>.Shared.Return(header);
        }

        // Nonces are produced by the stream processor itself (per-stream random prefix + per-chunk counter),
        // so this path no longer needs a nonce callback or any KeyStore round-trips.
        using var cryptor = CreateStreamCryptor(actualKey);
        await AeadStreamProcessor.EncryptChunksAsync(input, output, cryptor, effectiveChunkSize, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task DecryptToStreamAsync(Stream input, Stream output, string? keyId = null, byte[]? key = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(input);
        ArgumentHelpers.ThrowIfNull(output);
        OperationHelpers.ThrowIfNotReadable(input, $"Stream '{nameof(input)}' must be readable.");
        OperationHelpers.ThrowIfNotWritable(output, $"Stream '{nameof(output)}' must be writable.");

        // Read the one-time header: [version:1][algorithmId:1][keyIdLen:int32][keyId][keyVersionLen:int32][keyVersion].
        var fixedHeader = ArrayPool<byte>.Shared.Rent(2);
        string? headerKeyId;
        string? headerKeyVersion;
        try {
            if (await AeadChunkCodec.ReadAtLeastAsync(input, fixedHeader, 2, ct).ConfigureAwait(false) != 2)
                throw new InvalidDataException("Invalid encrypted stream format: insufficient data for header.");

            var firstByte = fixedHeader[0];
            var expectedFormatVersion = Options.CurrentFormatVersion ?? (byte)StreamFormatVersion.V1;
            if (firstByte != expectedFormatVersion)
                throw new InvalidDataException($"Invalid encrypted stream format: expected format version {expectedFormatVersion}, got {firstByte}.");

            var algorithmId = fixedHeader[1];
            var expectedAlgorithmId = GetAlgorithmId();
            if (algorithmId != expectedAlgorithmId) {
                throw new InvalidDataException(
                    $"Stream algorithm ID mismatch. Expected {expectedAlgorithmId} ({(EncryptionAlgorithm)expectedAlgorithmId}), got {algorithmId} ({(EncryptionAlgorithm)algorithmId}).");
            }

            headerKeyId = await ReadHeaderStringAsync(input, ct).ConfigureAwait(false);
            headerKeyVersion = await ReadHeaderStringAsync(input, ct).ConfigureAwait(false);
        }
        finally {
            ArrayPool<byte>.Shared.Return(fixedHeader);
        }

        if (string.IsNullOrEmpty(headerKeyId))
            headerKeyId = null;

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
        await AeadStreamProcessor.DecryptChunksAsync(input, output, cryptor, ct).ConfigureAwait(false);
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
        await EncryptToStreamAsync(input, outputStream, keyId, key, chunkSize, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task<byte[]> DecryptFromFileAsync(string inputPath, string? keyId = null, byte[]? key = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(inputPath);
        using var inputStream = File.OpenRead(inputPath);
        using var outputStream = new MemoryStream();
        await DecryptToStreamAsync(inputStream, outputStream, keyId, key, ct).ConfigureAwait(false);
        return outputStream.ToArray();
    }

    /// <summary>
    /// Creates a per-stream AEAD cipher bound to <paramref name="key" /> for the lifetime of one streaming operation. Implementations validate the key length and throw if it is
    /// invalid. Drives the allocation-reduced compact streaming frame used by <see cref="EncryptToStreamAsync" />/<see cref="DecryptToStreamAsync" />.
    /// </summary>
    public abstract IAeadStreamCryptor CreateStreamCryptor(ReadOnlySpan<byte> key);

    /// <summary>Reads an <c>[length:int32][utf8 bytes]</c> header string (length capped at 1024 bytes). Returns the decoded string (possibly empty).</summary>
    private static async Task<string> ReadHeaderStringAsync(Stream input, CancellationToken ct)
    {
        var lengthBuffer = ArrayPool<byte>.Shared.Rent(4);
        try {
            if (await AeadChunkCodec.ReadAtLeastAsync(input, lengthBuffer, 4, ct).ConfigureAwait(false) != 4)
                throw new InvalidDataException("Invalid encrypted stream format: insufficient data for header string length.");

            var length = BinaryPrimitives.ReadInt32LittleEndian(lengthBuffer);
            if (length < 0 || length > 1024)
                throw new InvalidDataException($"Invalid header string length: {length}. Maximum allowed: 1024 bytes.");

            if (length == 0)
                return "";

            var valueBuffer = ArrayPool<byte>.Shared.Rent(length);
            try {
                if (await AeadChunkCodec.ReadAtLeastAsync(input, valueBuffer, length, ct).ConfigureAwait(false) != length)
                    throw new InvalidDataException("Invalid encrypted stream format: header string truncated.");

                return Encoding.UTF8.GetString(valueBuffer, 0, length);
            }
            finally {
                ArrayPool<byte>.Shared.Return(valueBuffer);
            }
        }
        finally {
            ArrayPool<byte>.Shared.Return(lengthBuffer);
        }
    }

    /// <summary>Gets the algorithm identifier for this encryption service. Used in stream format header for versioning and compatibility.</summary>
    protected virtual byte GetAlgorithmId() => 0; // Default, override in derived classes

    /// <summary>Decrypts a buffer slice. Override to avoid copying when the implementation can decrypt in-place from a buffer.</summary>
    protected virtual byte[] DecryptChunk(byte[] buffer, int offset, int count, string? keyId, byte[]? key)
    {
        var chunk = new byte[count];
        Array.Copy(buffer, offset, chunk, 0, count);
        return Decrypt(chunk, keyId, key);
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
        await DecryptToStreamAsync(inputStream, outputStream, keyId, key, ct).ConfigureAwait(false);
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