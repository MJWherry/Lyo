using System.IO.Compression;
using EasyCompressor;
using Lyo.Compression.Models;

namespace Lyo.Compression.Snappier;

/// <summary><see cref="ICompressorFactory" /> for <see cref="SnappierCompressionAlgorithm" /> backed by <see cref="SnappierCompressor" />.</summary>
public sealed class SnappierCompressorFactory : ICompressorFactory
{
    private const string CompressorName = "CompressionService";

    public CompressionAlgorithm Algorithm => SnappierCompressionAlgorithm.Instance;

    public ICompressor Create(CompressionLevel level) => new SnappierCompressor(CompressorName);
}
