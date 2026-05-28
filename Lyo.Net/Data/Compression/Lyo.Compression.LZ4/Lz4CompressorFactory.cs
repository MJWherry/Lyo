using System.IO.Compression;
using EasyCompressor;
using Lyo.Compression.Models;

namespace Lyo.Compression.LZ4;

/// <summary><see cref="ICompressorFactory" /> for <see cref="Lz4CompressionAlgorithm" /> backed by <see cref="LZ4Compressor" />.</summary>
public sealed class Lz4CompressorFactory : ICompressorFactory
{
    private const string CompressorName = "CompressionService";

    public CompressionAlgorithm Algorithm => Lz4CompressionAlgorithm.Instance;

    public ICompressor Create(CompressionLevel level) => new LZ4Compressor(CompressorName);
}
