using System.IO.Compression;
using EasyCompressor;
using Lyo.Compression.Models;

namespace Lyo.Compression.LZMA;

/// <summary><see cref="ICompressorFactory" /> for <see cref="LzmaCompressionAlgorithm" /> backed by <see cref="LZMACompressor" />.</summary>
public sealed class LzmaCompressorFactory : ICompressorFactory
{
    private const string CompressorName = "CompressionService";

    public CompressionAlgorithm Algorithm => LzmaCompressionAlgorithm.Instance;

    public ICompressor Create(CompressionLevel level) => new LZMACompressor(CompressorName);
}
