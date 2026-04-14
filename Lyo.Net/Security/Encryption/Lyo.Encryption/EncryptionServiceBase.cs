using System.Buffers.Binary;
using System.Text;
using Lyo.Encryption.Utilities;
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
public abstract class EncryptionServiceBase : IEncryptionService
{
    /// <summary>The KeyStore used for retrieving encryption keys. May be null if service doesn't use KeyStore.</summary>
    protected readonly IKeyStore? KeyStore;

    /// <summary>The options used to configure this encryption service.</summary>
    protected readonly EncryptionServiceOptions Options;

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

    /// <inheritdoc />
    public string FileExtension => Options.FileExtension;

    public virtual Encoding DefaultEncoding { get; set; } = Encoding.UTF8;

    /// <inheritdoc />
    public abstract byte[] Encrypt(byte[] bytes, string? keyId = null, byte[]? key = null);

    /// <inheritdoc />
    public abstract byte[] Decrypt(byte[] encryptedBytes, string? keyId = null, byte[]? key = null);

    public virtual byte[] EncryptString(string text, string? keyId = null, byte[]? key = null, Encoding? encoding = null)
        => Encrypt((encoding ?? DefaultEncoding).GetBytes(text), keyId, key);

    public virtual string DecryptString(byte[] encryptedBytes, string? keyId = null, byte[]? key = null, Encoding? encoding = null)
        => (encoding ?? DefaultEncoding).GetString(Decrypt(encryptedBytes, keyId, key));

    public virtual async Task EncryptToStreamAsync(
        Stream input,
        Stream output,
        string? keyId = null,
        byte[]? key = null,
        int chunkSize = 1024 * 1024,
        CancellationToken ct = default)
    {
        var effectiveChunkSize = chunkSize <= 0 ? StreamChunkSizeHelper.DetermineChunkSize(input) : chunkSize;
        var formatVersion = Options.CurrentFormatVersion ?? (byte)StreamFormatVersion.V1;
        await output.WriteAsync(new[] { formatVersion }, ct).ConfigureAwait(false);
        await output.WriteAsync(new[] { GetAlgorithmId() }, ct).ConfigureAwait(false);
        await output.WriteAsync(new byte[2], ct).ConfigureAwait(false);
        var buffer = BufferPool.Rent(effectiveChunkSize);
        try {
            int bytesRead;
            while ((bytesRead = await input.ReadAsync(buffer, 0, Math.Min(effectiveChunkSize, buffer.Length), ct).ConfigureAwait(false)) > 0) {
                ct.ThrowIfCancellationRequested();
                var encryptedChunk = EncryptChunk(buffer, 0, bytesRead, keyId, key);
                var lengthBytes = new byte[4];
                BinaryPrimitives.WriteInt32LittleEndian(lengthBytes, encryptedChunk.Length);
                await output.WriteAsync(lengthBytes, ct).ConfigureAwait(false);
                await output.WriteAsync(encryptedChunk, ct).ConfigureAwait(false);
            }
        }
        finally {
            BufferPool.Return(buffer);
        }
    }

    public virtual async Task DecryptToStreamAsync(Stream input, Stream output, string? keyId = null, byte[]? key = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(input, nameof(input));
        ArgumentHelpers.ThrowIfNull(output, nameof(output));
        OperationHelpers.ThrowIfNotReadable(input, $"Stream '{nameof(input)}' must be readable.");
        OperationHelpers.ThrowIfNotWritable(output, $"Stream '{nameof(output)}' must be writable.");
        // Maximum allowed encrypted chunk size (200 MB) to prevent denial-of-service attacks
        // This accounts for encryption overhead (nonce, tag, etc.) while preventing memory exhaustion
        const int maxEncryptedChunkSize = 200 * 1024 * 1024; // 200 MB

        // Read and validate stream format header
        // Use buffer pool for header buffer
        var headerBuffer = BufferPool.RentExact(4, true);
        try {
            var headerBytesRead = await input.ReadAsync(headerBuffer, 0, 4, ct).ConfigureAwait(false);
            if (headerBytesRead != 4)
                throw new InvalidDataException("Invalid encrypted stream format: insufficient data for header.");

            var firstByte = headerBuffer[0];
            var expectedFormatVersion = Options.CurrentFormatVersion ?? (byte)StreamFormatVersion.V1;
            if (firstByte != expectedFormatVersion)
                throw new InvalidDataException($"Invalid encrypted stream format: expected format version {expectedFormatVersion}, got {firstByte}.");

            var formatVersion = (StreamFormatVersion)firstByte;
            var algorithmId = headerBuffer[1];
            // Reserved bytes at [2] and [3] are ignored for now

            // Validate format version
            var maxSupportedVersion = Options.CurrentFormatVersion ?? (byte)StreamFormatVersion.V1;
            if (formatVersion > (StreamFormatVersion)maxSupportedVersion)
                throw new InvalidDataException($"Unsupported stream format version: {formatVersion}. Maximum supported version: {(StreamFormatVersion)maxSupportedVersion}.");

            // Validate algorithm ID matches this service
            var expectedAlgorithmId = GetAlgorithmId();
            if (algorithmId != expectedAlgorithmId) {
                throw new InvalidDataException(
                    $"Stream algorithm ID mismatch. Expected {expectedAlgorithmId} ({(EncryptionAlgorithm)expectedAlgorithmId}), got {algorithmId} ({(EncryptionAlgorithm)algorithmId}).");
            }

            // Use buffer pool for length buffer (reused across iterations)
            var lengthBuffer = BufferPool.RentExact(4, true);
            try {
                // Read first chunk length
                if (await input.ReadAsync(lengthBuffer, 0, 4, ct).ConfigureAwait(false) != 4)
                    return; // No chunks after header

                // Process chunks
                while (true) {
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
                        var totalRead = 0;
                        while (totalRead < chunkLength) {
                            ct.ThrowIfCancellationRequested();
                            var bytesRead = await input.ReadAsync(encryptedChunk, totalRead, chunkLength - totalRead, ct).ConfigureAwait(false);
                            if (bytesRead == 0)
                                throw new EndOfStreamException("Unexpected end of encrypted stream.");

                            totalRead += bytesRead;
                        }

                        var decryptedChunk = DecryptChunk(encryptedChunk, 0, chunkLength, keyId, key);
                        await output.WriteAsync(decryptedChunk, ct).ConfigureAwait(false);
                    }
                    finally {
                        BufferPool.Return(encryptedChunk);
                    }

                    // Read next chunk length
                    if (await input.ReadAsync(lengthBuffer, 0, 4, ct).ConfigureAwait(false) != 4)
                        break; // End of stream
                }
            }
            finally {
                BufferPool.Return(lengthBuffer);
            }
        }
        finally {
            BufferPool.Return(headerBuffer);
        }
    }

    // File operation methods from IEncryptionService
    public virtual async Task EncryptToFileAsync(byte[] data, string outputPath, string? keyId = null, byte[]? key = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(data, nameof(data));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputPath, nameof(outputPath));
        using var inputStream = new MemoryStream(data);
        await using var outputStream = File.Create(outputPath);
        await EncryptToStreamAsync(inputStream, outputStream, keyId, key, ct: ct).ConfigureAwait(false);
    }

    public virtual async Task EncryptToFileAsync(
        Stream input,
        string outputPath,
        string? keyId = null,
        byte[]? key = null,
        int chunkSize = 1024 * 1024,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(input, nameof(input));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputPath, nameof(outputPath));
        ArgumentHelpers.ThrowIfNegative(chunkSize, nameof(chunkSize));
        await using var outputStream = File.Create(outputPath);
        await EncryptToStreamAsync(input, outputStream, keyId, key, chunkSize, ct).ConfigureAwait(false);
    }

    public virtual async Task<byte[]> DecryptFromFileAsync(string inputPath, string? keyId = null, byte[]? key = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(inputPath, nameof(inputPath));
        await using var inputStream = File.OpenRead(inputPath);
        using var outputStream = new MemoryStream();
        await DecryptToStreamAsync(inputStream, outputStream, keyId, key, ct).ConfigureAwait(false);
        return outputStream.ToArray();
    }

    /// <summary>Algorithm for stream headers and discovery; matches the stream format algorithm byte.</summary>
    public EncryptionAlgorithm AlgorithmKind => (EncryptionAlgorithm)GetAlgorithmId();

    /// <summary>Gets the algorithm identifier for this encryption service. Used in stream format header for versioning and compatibility.</summary>
    protected virtual byte GetAlgorithmId() => 0; // Default, override in derived classes

    /// <summary>Encrypts a buffer slice. Override to avoid copying when the implementation supports span-based encryption.</summary>
    protected virtual byte[] EncryptChunk(byte[] buffer, int offset, int count, string? keyId, byte[]? key)
    {
        var chunk = new byte[count];
        Array.Copy(buffer, offset, chunk, 0, count);
        return Encrypt(chunk, keyId, key);
    }

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
        ArgumentHelpers.ThrowIfFileNotFound(inputPath, nameof(inputPath));
        var outputFile = string.IsNullOrEmpty(outputPath) ? inputPath + FileExtension : outputPath;
        await using var inputStream = File.OpenRead(inputPath);
        await using var outputStream = File.Create(outputFile);
        await EncryptToStreamAsync(inputStream, outputStream, keyId, key, ct: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Convenience method: Decrypts a file and writes it to an output file. This is a convenience method not part of IEncryptionService interface. For interface-compliant
    /// methods, use DecryptFromFileAsync instead.
    /// </summary>
    public virtual async Task DecryptFileAsync(string inputPath, string outputPath, string? keyId = null, byte[]? key = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfFileNotFound(inputPath, nameof(inputPath));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputPath, nameof(outputPath));
        await using var inputStream = File.OpenRead(inputPath);
        await using var outputStream = File.Create(outputPath);
        await DecryptToStreamAsync(inputStream, outputStream, keyId, key, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Convenience method: Encrypts a file and returns the encrypted bytes. This is a convenience method not part of IEncryptionService interface. For interface-compliant
    /// methods, use EncryptToFileAsync instead.
    /// </summary>
    public virtual byte[] EncryptFile(string inputPath, string? keyId = null, byte[]? key = null)
    {
        ArgumentHelpers.ThrowIfFileNotFound(inputPath, nameof(inputPath));
        var fileBytes = File.ReadAllBytes(inputPath);
        return Encrypt(fileBytes, keyId, key);
    }

    /// <summary>
    /// Convenience method: Encrypts a file synchronously and writes it to an output file. This is a convenience method not part of IEncryptionService interface. For
    /// interface-compliant methods, use EncryptToFileAsync instead.
    /// </summary>
    public virtual void EncryptToFile(string inputPath, string? outputPath = null, string? keyId = null, byte[]? key = null)
    {
        ArgumentHelpers.ThrowIfFileNotFound(inputPath, nameof(inputPath));
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
        ArgumentHelpers.ThrowIfFileNotFound(inputPath, nameof(inputPath));
        var encryptedBytes = File.ReadAllBytes(inputPath);
        return Decrypt(encryptedBytes, keyId, key);
    }

    /// <summary>
    /// Convenience method: Decrypts a file synchronously and writes it to an output file. This is a convenience method not part of IEncryptionService interface. For
    /// interface-compliant methods, use DecryptFromFileAsync instead.
    /// </summary>
    public virtual void DecryptToFile(string inputPath, string outputPath, string? keyId = null, byte[]? key = null)
    {
        ArgumentHelpers.ThrowIfFileNotFound(inputPath, nameof(inputPath));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(outputPath, nameof(outputPath));
        var decrypted = DecryptFile(inputPath, keyId, key);
        File.WriteAllBytes(outputPath, decrypted);
    }
}