using System.IO.Compression;
using EasyCompressor;
using Lyo.Compression.Models;

namespace Lyo.Compression;

/// <summary>
/// Produces an <see cref="ICompressor" /> for exactly one <see cref="CompressionAlgorithm" />. Implementations live in either the base <c>Lyo.Compression</c> package (for
/// built-in algorithms) or addon packages such as <c>Lyo.Compression.LZ4</c>; <see cref="CompressionService" /> resolves them via DI by their <see cref="Algorithm" /> key.
/// </summary>
public interface ICompressorFactory
{
    /// <summary>The compression algorithm this factory creates compressors for.</summary>
    CompressionAlgorithm Algorithm { get; }

    /// <summary>Creates a configured <see cref="ICompressor" /> for the requested <paramref name="level" /> (codecs that ignore level should accept the argument anyway).</summary>
    ICompressor Create(CompressionLevel level);
}