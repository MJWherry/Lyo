using System.IO.Compression;
using EasyCompressor;
using Lyo.Compression.Models;

namespace Lyo.Compression.BZip2;

/// <summary><see cref="ICompressorFactory" /> for <see cref="BZip2CompressionAlgorithm" /> backed by <see cref="BZip2Compressor" />.</summary>
public sealed class BZip2CompressorFactory : ICompressorFactory
{
    private const string CompressorName = "CompressionService";

    public CompressionAlgorithm Algorithm => BZip2CompressionAlgorithm.Instance;

    public ICompressor Create(CompressionLevel level) => new BZip2Compressor(CompressorName, MapCompressionLevel(level));

    private static int MapCompressionLevel(CompressionLevel level)
        => level switch {
            CompressionLevel.Fastest => 3,
            CompressionLevel.NoCompression => 1,
            CompressionLevel.Optimal => 6,
            var _ => 6
        };
}
