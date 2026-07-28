using System.IO.Pipelines;
using Lyo.Compression;
using Lyo.Compression.Models;
using Lyo.Encryption;
using Lyo.Exceptions;

namespace Lyo.FileStorage;

/// <summary>
/// Composes an <see cref="ICompressionService" /> and an <see cref="IEncryptionService" /> over streams in the correct order — compress-then-encrypt on write,
/// decrypt-then-decompress on read — as a single pass over the data. The two stages run concurrently connected by a <see cref="Pipe" /> with backpressure, so the intermediate
/// (compressed) representation is never buffered in full.
/// </summary>
/// <remarks>
/// Compressing after encryption is useless (ciphertext is incompressible), so the order here is the only correct one. For the envelope-encryption (two-key) equivalent used
/// by the storage backends, see <c>FileStorageStreamingPipelines</c>.
/// </remarks>
public static class CompressEncryptPipeline
{
    /// <summary>
    /// Reads plaintext from <paramref name="input" />, compresses it with <paramref name="compression" /> (using <paramref name="algorithm" /> or the service default) and
    /// encrypts the compressed bytes into <paramref name="output" /> in one pass. Decrypt with <see cref="DecryptThenDecompressAsync" /> using the same algorithm and key.
    /// </summary>
    /// <param name="input">Readable plaintext stream.</param>
    /// <param name="output">Writable destination for the encrypted stream.</param>
    /// <param name="compression">Compression service.</param>
    /// <param name="encryption">Encryption service (any single-key AEAD service).</param>
    /// <param name="keyId">Key store key ID; mutually optional with <paramref name="key" />.</param>
    /// <param name="key">Raw encryption key; mutually optional with <paramref name="keyId" />.</param>
    /// <param name="algorithm">Compression algorithm; null uses the service's configured algorithm.</param>
    /// <param name="chunkSize">Encryption chunk size in bytes.</param>
    /// <param name="associatedData">Optional AAD authenticated with every encrypted chunk.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async Task CompressThenEncryptAsync(
        Stream input,
        Stream output,
        ICompressionService compression,
        IEncryptionService encryption,
        string? keyId = null,
        byte[]? key = null,
        CompressionAlgorithm? algorithm = null,
        int chunkSize = 1024 * 1024,
        byte[]? associatedData = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(input);
        ArgumentHelpers.ThrowIfNull(output);
        ArgumentHelpers.ThrowIfNull(compression);
        ArgumentHelpers.ThrowIfNull(encryption);
        var pipe = new Pipe();
        var compressTask = CompressIntoPipeAsync(compression, algorithm, input, pipe, ct);
        var encryptTask = EncryptFromPipeAsync(encryption, pipe, output, keyId, key, chunkSize, associatedData, ct);
        await Task.WhenAll(compressTask, encryptTask).ConfigureAwait(false);
    }

    /// <summary>
    /// Reads an encrypted stream produced by <see cref="CompressThenEncryptAsync" /> from <paramref name="input" />, decrypts it with <paramref name="encryption" /> and
    /// decompresses the result into <paramref name="output" /> in one pass.
    /// </summary>
    /// <param name="input">Readable encrypted stream.</param>
    /// <param name="output">Writable destination for the restored plaintext.</param>
    /// <param name="compression">Compression service.</param>
    /// <param name="encryption">Encryption service used for the original encrypt.</param>
    /// <param name="keyId">Key store key ID; mutually optional with <paramref name="key" />.</param>
    /// <param name="key">Raw decryption key; mutually optional with <paramref name="keyId" />.</param>
    /// <param name="algorithm">Compression algorithm used on write; null uses the service's configured algorithm.</param>
    /// <param name="associatedData">AAD supplied on encrypt, if any; a mismatch fails authentication.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async Task DecryptThenDecompressAsync(
        Stream input,
        Stream output,
        ICompressionService compression,
        IEncryptionService encryption,
        string? keyId = null,
        byte[]? key = null,
        CompressionAlgorithm? algorithm = null,
        byte[]? associatedData = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(input);
        ArgumentHelpers.ThrowIfNull(output);
        ArgumentHelpers.ThrowIfNull(compression);
        ArgumentHelpers.ThrowIfNull(encryption);
        var pipe = new Pipe();
        var decryptTask = DecryptIntoPipeAsync(encryption, input, pipe, keyId, key, associatedData, ct);
        var decompressTask = DecompressFromPipeAsync(compression, algorithm, pipe, output, ct);
        await Task.WhenAll(decryptTask, decompressTask).ConfigureAwait(false);
    }

    /// <summary>
    /// Reading stage of <see cref="CompressThenEncryptAsync" />. Always completes <see cref="Pipe.Reader" /> — on failure with the exception — so the writing stage's
    /// backpressure-blocked flush is released and the fault propagates instead of deadlocking <c>Task.WhenAll</c>.
    /// </summary>
    private static async Task EncryptFromPipeAsync(
        IEncryptionService encryption,
        Pipe pipe,
        Stream output,
        string? keyId,
        byte[]? key,
        int chunkSize,
        byte[]? associatedData,
        CancellationToken ct)
    {
        try {
            using (var compressedReader = pipe.Reader.AsStream(true))
                await encryption.EncryptToStreamAsync(compressedReader, output, keyId, key, chunkSize, associatedData, ct).ConfigureAwait(false);

            await pipe.Reader.CompleteAsync().ConfigureAwait(false);
        }
        catch (Exception ex) {
            await pipe.Reader.CompleteAsync(ex).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Reading stage of <see cref="DecryptThenDecompressAsync" />. Always completes <see cref="Pipe.Reader" /> — on failure (e.g. the decompression bomb guard) with the
    /// exception — so the decrypting stage's backpressure-blocked flush is released and the fault propagates instead of deadlocking <c>Task.WhenAll</c>.
    /// </summary>
    private static async Task DecompressFromPipeAsync(ICompressionService compression, CompressionAlgorithm? algorithm, Pipe pipe, Stream output, CancellationToken ct)
    {
        try {
            using (var compressedReader = pipe.Reader.AsStream(true)) {
                if (algorithm == null)
                    await compression.DecompressAsync(compressedReader, output, ct: ct).ConfigureAwait(false);
                else
                    await compression.Resolver.DecompressAsync(compressedReader, output, algorithm, ct: ct).ConfigureAwait(false);
            }

            await pipe.Reader.CompleteAsync().ConfigureAwait(false);
        }
        catch (Exception ex) {
            await pipe.Reader.CompleteAsync(ex).ConfigureAwait(false);
            throw;
        }
    }

    private static async Task CompressIntoPipeAsync(ICompressionService compression, CompressionAlgorithm? algorithm, Stream input, Pipe pipe, CancellationToken ct)
    {
        try {
            using (var compressedWriter = pipe.Writer.AsStream(true)) {
                if (algorithm == null)
                    await compression.CompressAsync(input, compressedWriter, ct: ct).ConfigureAwait(false);
                else
                    await compression.Resolver.CompressAsync(input, compressedWriter, algorithm, ct: ct).ConfigureAwait(false);
            }

            await pipe.Writer.CompleteAsync().ConfigureAwait(false);
        }
        catch (Exception ex) {
            await pipe.Writer.CompleteAsync(ex).ConfigureAwait(false);
            throw;
        }
    }

    private static async Task DecryptIntoPipeAsync(IEncryptionService encryption, Stream input, Pipe pipe, string? keyId, byte[]? key, byte[]? associatedData, CancellationToken ct)
    {
        try {
            using (var compressedWriter = pipe.Writer.AsStream(true))
                await encryption.DecryptToStreamAsync(input, compressedWriter, keyId, key, associatedData, ct).ConfigureAwait(false);

            await pipe.Writer.CompleteAsync().ConfigureAwait(false);
        }
        catch (Exception ex) {
            await pipe.Writer.CompleteAsync(ex).ConfigureAwait(false);
            throw;
        }
    }
}