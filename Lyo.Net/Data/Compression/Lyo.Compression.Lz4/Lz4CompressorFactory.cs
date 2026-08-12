using System.IO.Compression;
using EasyCompressor;
using Lyo.Compression.Models;

namespace Lyo.Compression.Lz4;

/// <summary><see cref="ICompressorFactory" /> for <see cref="Lz4CompressionAlgorithm" /> backed by <see cref="LZ4Compressor" />.</summary>
public sealed class Lz4CompressorFactory : ICompressorFactory
{
    private const string CompressorName = "CompressionService";

    public CompressionAlgorithm Algorithm => Lz4CompressionAlgorithm.Instance;

    // StreamCompatible: the fast binary (byte[]) API emits the LZ4 frame format instead of the default block format, so binary Compress output stays readable by the
    // stream Decompress path (one wire format across all APIs) while keeping the direct buffer-to-buffer speed.
    public ICompressor Create(CompressionLevel level) => new LZ4Compressor(CompressorName, LZ4BinaryCompressionMode.StreamCompatible);
}