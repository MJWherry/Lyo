using BenchmarkDotNet.Attributes;
using Lyo.Benchmark;
using Lyo.Benchmark.Data;
using Lyo.Compression.Compressors;
using Lyo.Compression.Models;
using Lyo.Compression.Zstd;
using Lyo.Streams;

namespace Lyo.Compression.Benchmarks;

/// <summary>Large-payload compress/decompress benchmarks for stream and file APIs.</summary>
[BenchmarkDescription(
    "Compress/decompress at 100 MiB–2 GiB with GZip and Zstd. Stream methods use DeterministicPayloadStream input and NullingStream output; file methods use IOTemp paths. Decompress setup reuses pre-compressed IOTemp files.")]
[BenchmarkParameter("DataSize", Unit = "bytes", Description = "Input size: 100, 250, 500, 750 MiB, 1 GiB, 1.5 GiB, 2 GiB.")]
public class LargeFileStreamingBenchmarks : LyoBenchmarkBase
{
    private string _compressedGZipPath = null!;
    private string _compressedZstdPath = null!;
    private CompressionService _gzipService = null!;
    private string _plaintextPath = null!;
    private CompressionService _zstdService = null!;

    [Params(
        BenchmarkData.StreamingSize100MiB, BenchmarkData.StreamingSize250MiB, BenchmarkData.StreamingSize500MiB, BenchmarkData.StreamingSize750MiB, BenchmarkData.StreamingSize1GiB,
        BenchmarkData.StreamingSize15GiB, BenchmarkData.StreamingSize2GiB)]
    public long DataSize { get; set; }

    /// <inheritdoc />
    protected override void OnGlobalSetup()
    {
        ICompressorFactory[] factories = [new GZipCompressorFactory(), new ZstdCompressorFactory()];
        _gzipService = new(factories, options: new() { DefaultAlgorithm = CompressionAlgorithm.GZip, EnableMetrics = false });
        _zstdService = new(factories, options: new() { DefaultAlgorithm = ZstdCompressionAlgorithm.Instance, EnableMetrics = false });
        _plaintextPath = CreateSeededFilePath(DataSize);
    }

    [GlobalSetup(Targets = [nameof(DecompressStream_GZip), nameof(DecompressFile_GZip)])]
    public void SetupGZipDecompress()
    {
        EnsureGlobalSetup();
        var gzipInfo = _gzipService.CompressFile(_plaintextPath, CreateTempOutputPath());
        _compressedGZipPath = gzipInfo.OutputFilePath;
    }

    [GlobalSetup(Targets = [nameof(DecompressStream_Zstd), nameof(DecompressFile_Zstd)])]
    public void SetupZstdDecompress()
    {
        EnsureGlobalSetup();
        var zstdInfo = _zstdService.CompressFile(_plaintextPath, CreateTempOutputPath());
        _compressedZstdPath = zstdInfo.OutputFilePath;
    }

    [Benchmark]
    [BenchmarkCategory("Stream")]
    public async Task CompressStream_GZip()
    {
        await using var input = new DeterministicPayloadStream(DataSize, BenchmarkData.PayloadSeed);
        await using var output = new NullingStream();
        await _gzipService.CompressAsync(input, output);
    }

    [Benchmark]
    [BenchmarkCategory("Stream")]
    public async Task DecompressStream_GZip()
    {
        await using var input = File.OpenRead(_compressedGZipPath);
        await using var output = new NullingStream();
        await _gzipService.DecompressAsync(input, output);
    }

    [Benchmark]
    [BenchmarkCategory("File")]
    public async Task CompressFile_GZip() => await _gzipService.CompressFileAsync(_plaintextPath, CreateIterationOutputPath());

    [Benchmark]
    [BenchmarkCategory("File")]
    public async Task DecompressFile_GZip() => await _gzipService.DecompressFileAsync(_compressedGZipPath, CreateIterationOutputPath());

    [Benchmark]
    [BenchmarkCategory("Stream")]
    public async Task CompressStream_Zstd()
    {
        await using var input = new DeterministicPayloadStream(DataSize, BenchmarkData.PayloadSeed);
        await using var output = new NullingStream();
        await _zstdService.CompressAsync(input, output);
    }

    [Benchmark]
    [BenchmarkCategory("Stream")]
    public async Task DecompressStream_Zstd()
    {
        await using var input = File.OpenRead(_compressedZstdPath);
        await using var output = new NullingStream();
        await _zstdService.DecompressAsync(input, output);
    }

    [Benchmark]
    [BenchmarkCategory("File")]
    public async Task CompressFile_Zstd() => await _zstdService.CompressFileAsync(_plaintextPath, CreateIterationOutputPath());

    [Benchmark]
    [BenchmarkCategory("File")]
    public async Task DecompressFile_Zstd() => await _zstdService.DecompressFileAsync(_compressedZstdPath, CreateIterationOutputPath());
}