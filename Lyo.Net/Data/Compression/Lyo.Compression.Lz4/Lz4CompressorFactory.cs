using System.IO.Compression;
using EasyCompressor;
using Lyo.Compression.Models;

namespace Lyo.Compression.Lz4;

/// <summary><see cref="ICompressorFactory" /> for <see cref="Lz4CompressionAlgorithm" />, implemented by <see cref="LZ4Compressor" />.</summary>
public sealed class Lz4CompressorFactory : ICompressorFactory
{
    private const string CompressorName = "CompressionService";

    public CompressionAlgorithm Algorithm => Lz4CompressionAlgorithm.Instance;

    // StreamCompatible: the fast binary (byte[]) API writes the LZ4 frame format instead of the default block format, so Compress bytes stay readable by the
    // stream Decompress path. One wire format across all APIs, still buffer-to-buffer fast.
    public ICompressor Create(CompressionLevel level) => new LZ4Compressor(CompressorName, LZ4BinaryCompressionMode.StreamCompatible);
}