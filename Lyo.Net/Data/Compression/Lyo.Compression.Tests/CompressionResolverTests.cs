using System.Text;
using Lyo.Compression.Compressors;
using Lyo.Compression.LZ4;
using Lyo.Compression.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Compression.Tests;

public class CompressionResolverTests
{
    private static readonly ICompressorFactory[] Factories = [new GZipCompressorFactory(), new Lz4CompressorFactory()];

    [Fact]
    public void Decompress_WithDifferentAlgorithmThanDefault_RoundTrips()
    {
        var service = new CompressionService(Factories, NullLogger<CompressionService>.Instance, new() { DefaultAlgorithm = Lz4CompressionAlgorithm.Instance });
        var original = Encoding.UTF8.GetBytes(new string('x', 8000));
        _ = service.Compress(original, CompressionAlgorithm.GZip, out var gzipBytes);
        var info = service.Decompress(gzipBytes, CompressionAlgorithm.GZip, out var roundTrip);
        Assert.Equal(original, roundTrip);
        Assert.True(info.DecompressedSize > 0);
    }

    [Fact]
    public void GetCompressor_CachesPerAlgorithm()
    {
        var service = new CompressionService(Factories, NullLogger<CompressionService>.Instance, new() { DefaultAlgorithm = CompressionAlgorithm.GZip });
        var gzip1 = service.GetCompressor(CompressionAlgorithm.GZip);
        var gzip2 = service.GetCompressor(CompressionAlgorithm.GZip);
        Assert.Same(gzip1, gzip2);
        Assert.NotSame(gzip1, service.GetCompressor(Lz4CompressionAlgorithm.Instance));
    }
}
