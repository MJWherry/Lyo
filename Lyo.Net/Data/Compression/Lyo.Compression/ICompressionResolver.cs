using EasyCompressor;
using Lyo.Compression.Models;

namespace Lyo.Compression;

/// <summary>
/// Dispatches compress/decompress to a specific <see cref="CompressionAlgorithm" /> using registered <see cref="ICompressorFactory" /> instances.
/// Default implementation: <see cref="CompressionService" /> (register via <see cref="Extensions.AddCompressionService" /> or <see cref="Extensions.AddCompressionResolver" />).
/// Uses <see cref="CompressionServiceOptions.Default" /> when constructed without explicit options.
/// </summary>
public interface ICompressionResolver
{
    /// <summary>Gets or creates a cached <see cref="ICompressor" /> for <paramref name="algorithm" />.</summary>
    ICompressor GetCompressor(CompressionAlgorithm algorithm);

    /// <summary>Compresses <paramref name="bytes" /> with <paramref name="algorithm" />.</summary>
    CompressionInfo Compress(byte[] bytes, CompressionAlgorithm algorithm, out byte[] compressed);

    /// <summary>Decompresses <paramref name="compressedBytes" /> with <paramref name="algorithm" />.</summary>
    DecompressionInfo Decompress(byte[] compressedBytes, CompressionAlgorithm algorithm, out byte[] decompressed);

    /// <summary>Compresses streams with <paramref name="algorithm" />.</summary>
    void Compress(Stream inputStream, Stream outputStream, CompressionAlgorithm algorithm);

    /// <summary>Decompresses streams with <paramref name="algorithm" />.</summary>
    void Decompress(Stream inputStream, Stream outputStream, CompressionAlgorithm algorithm);

    /// <summary>Asynchronously compresses streams with <paramref name="algorithm" />.</summary>
    Task CompressAsync(Stream inputStream, Stream outputStream, CompressionAlgorithm algorithm, int? chunkSize = null, CancellationToken ct = default);

    /// <summary>Asynchronously decompresses streams with <paramref name="algorithm" />.</summary>
    Task DecompressAsync(Stream inputStream, Stream outputStream, CompressionAlgorithm algorithm, int? chunkSize = null, CancellationToken ct = default);
}
