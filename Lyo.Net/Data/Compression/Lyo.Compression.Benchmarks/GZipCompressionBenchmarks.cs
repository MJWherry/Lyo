using BenchmarkDotNet.Attributes;
using Lyo.Benchmarking;
using Lyo.Benchmarking.Data;
using Lyo.Compression.Compressors;
using Lyo.Compression.Models;

namespace Lyo.Compression.Benchmarks;

[BenchmarkDescription(
    "Buffered GZip compress/decompress of seeded deterministic (incompressible) buffers (100 / 250 / 500 MiB); decompress cases reuse output from setup.")]
[BenchmarkParameter("DataSize", Unit = "bytes", Description = "Input size: 100, 250, or 500 MiB.")]
public class GZipCompressionBenchmarks : LyoBenchmarkBase
{
    private byte[] _compressed = null!;
    private CompressionService _compressionService = null!;
    private byte[] _testData = null!;

    [Params(BenchmarkData.BufferedSize100MiB, BenchmarkData.BufferedSize250MiB, BenchmarkData.BufferedSize500MiB)]
    public int DataSize { get; set; }

    /// <inheritdoc />
    protected override void OnGlobalSetup()
    {
        ICompressorFactory[] factories = [new GZipCompressorFactory()];
        _compressionService = new(factories, options: new CompressionServiceOptions { DefaultAlgorithm = CompressionAlgorithm.GZip, EnableMetrics = false });
        _testData = BenchmarkData.DeterministicBytes(DataSize);
    }

    [GlobalSetup(Target = nameof(Decompress))]
    public void SetupDecompress()
    {
        EnsureGlobalSetup();
        _ = _compressionService.Compress(_testData, out _compressed);
    }

    [Benchmark]
    public byte[] Compress()
    {
        _ = _compressionService.Compress(_testData, out var compressed);
        return compressed;
    }

    [Benchmark]
    public byte[] Decompress()
    {
        _ = _compressionService.Decompress(_compressed, out var decompressed);
        return decompressed;
    }
}