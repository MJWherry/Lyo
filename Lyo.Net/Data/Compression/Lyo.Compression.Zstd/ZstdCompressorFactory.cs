using System.IO.Compression;
using EasyCompressor;
using Lyo.Compression.Models;

namespace Lyo.Compression.Zstd;

/// <summary><see cref="ICompressorFactory" /> for <see cref="ZstdCompressionAlgorithm" /> backed by <see cref="ZstdSharpCompressor" />.</summary>
public sealed class ZstdCompressorFactory : ICompressorFactory
{
    private const string CompressorName = "CompressionService";

    public CompressionAlgorithm Algorithm => ZstdCompressionAlgorithm.Instance;

    public ICompressor Create(CompressionLevel level) => new ZstdSharpCompressor(CompressorName);
}
