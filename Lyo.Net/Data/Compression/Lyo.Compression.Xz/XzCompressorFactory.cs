using System.IO.Compression;
using EasyCompressor;
using Lyo.Compression.Models;

namespace Lyo.Compression.Xz;

/// <summary><see cref="ICompressorFactory" /> for <see cref="XzCompressionAlgorithm" /> backed by <see cref="XzCompressor" />.</summary>
public sealed class XzCompressorFactory : ICompressorFactory
{
    private const string CompressorName = "CompressionService";

    public CompressionAlgorithm Algorithm => XzCompressionAlgorithm.Instance;

    public ICompressor Create(CompressionLevel level) => new XzCompressor(CompressorName);
}