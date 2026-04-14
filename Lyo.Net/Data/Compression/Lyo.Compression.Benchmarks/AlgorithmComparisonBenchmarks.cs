using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Lyo.Compression.Models;

namespace Lyo.Compression.Benchmarks;

/// <summary>Benchmarks comparing different compression algorithms</summary>
[SimpleJob(RuntimeMoniker.HostProcess)]
[MemoryDiagnoser]
public class AlgorithmComparisonBenchmarks
{
    private CompressionService _bzip2Service = null!;
    private byte[] _compressedBZip2 = null!;
    private byte[] _compressedDeflate = null!;
    private byte[] _compressedGZip = null!;
    private byte[] _compressedLZ4 = null!;
    private byte[] _compressedLZMA = null!;
    private byte[] _compressedSnappier = null!;
    private byte[] _compressedXZ = null!;
    private byte[] _compressedZstd = null!;
    private CompressionService _deflateService = null!;
    private CompressionService _gzipService = null!;
    private CompressionService _lz4Service = null!;
    private CompressionService _lzmaService = null!;
    private CompressionService _snappierService = null!;
    private byte[] _testData = null!;
    private CompressionService _xzService = null!;
    private CompressionService _zstdService = null!;

    [Params(1024, 1024 * 1024, 10 * 1024 * 1024, 100 * 1024 * 1024)] // 1 KB, 1 MB, 10 MB, 100 MB
    public int DataSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var gzipOptions = new CompressionServiceOptions { DefaultAlgorithm = CompressionAlgorithm.GZip, EnableMetrics = false };
        _gzipService = new(options: gzipOptions);
        var deflateOptions = new CompressionServiceOptions { DefaultAlgorithm = CompressionAlgorithm.Deflate, EnableMetrics = false };
        _deflateService = new(options: deflateOptions);
        var zstdOptions = new CompressionServiceOptions { DefaultAlgorithm = CompressionAlgorithm.ZstdSharp, EnableMetrics = false };
        _zstdService = new(options: zstdOptions);
        var snappierOptions = new CompressionServiceOptions { DefaultAlgorithm = CompressionAlgorithm.Snappier, EnableMetrics = false };
        _snappierService = new(options: snappierOptions);
        var lz4Options = new CompressionServiceOptions { DefaultAlgorithm = CompressionAlgorithm.LZ4, EnableMetrics = false };
        _lz4Service = new(options: lz4Options);
        var lzmaOptions = new CompressionServiceOptions { DefaultAlgorithm = CompressionAlgorithm.LZMA, EnableMetrics = false };
        _lzmaService = new(options: lzmaOptions);
        var bzip2Options = new CompressionServiceOptions { DefaultAlgorithm = CompressionAlgorithm.BZip2, EnableMetrics = false };
        _bzip2Service = new(options: bzip2Options);
        var xzOptions = new CompressionServiceOptions { DefaultAlgorithm = CompressionAlgorithm.XZ, EnableMetrics = false };
        _xzService = new(options: xzOptions);
#if !NETSTANDARD2_0
        var brotliOptions = new CompressionServiceOptions { DefaultAlgorithm = CompressionAlgorithm.Brotli, EnableMetrics = false };
        _brotliService = new(options: brotliOptions);
        var zlibOptions = new CompressionServiceOptions { DefaultAlgorithm = CompressionAlgorithm.ZLib, EnableMetrics = false };
        _zlibService = new(options: zlibOptions);
#endif
        _testData = new byte[DataSize];
        RandomNumberGenerator.Fill(_testData);

        // Pre-compress data for decompression benchmarks
        _ = _gzipService.Compress(_testData, out _compressedGZip);
        _ = _deflateService.Compress(_testData, out _compressedDeflate);
        _ = _zstdService.Compress(_testData, out _compressedZstd);
        _ = _snappierService.Compress(_testData, out _compressedSnappier);
        _ = _lz4Service.Compress(_testData, out _compressedLZ4);
        _ = _lzmaService.Compress(_testData, out _compressedLZMA);
        _ = _bzip2Service.Compress(_testData, out _compressedBZip2);
        _ = _xzService.Compress(_testData, out _compressedXZ);
#if !NETSTANDARD2_0
        _ = _brotliService.Compress(_testData, out _compressedBrotli);
        _ = _zlibService.Compress(_testData, out _compressedZlib);
#endif
    }

    // Compression Benchmarks
    [Benchmark(Baseline = true)]
    public byte[] GZip_Compress()
    {
        _ = _gzipService.Compress(_testData, out var compressed);
        return compressed;
    }

    [Benchmark]
    public byte[] Deflate_Compress()
    {
        _ = _deflateService.Compress(_testData, out var compressed);
        return compressed;
    }

    [Benchmark]
    public byte[] Zstd_Compress()
    {
        _ = _zstdService.Compress(_testData, out var compressed);
        return compressed;
    }

    [Benchmark]
    public byte[] Snappier_Compress()
    {
        _ = _snappierService.Compress(_testData, out var compressed);
        return compressed;
    }

    [Benchmark]
    public byte[] LZ4_Compress()
    {
        _ = _lz4Service.Compress(_testData, out var compressed);
        return compressed;
    }

    [Benchmark]
    public byte[] LZMA_Compress()
    {
        _ = _lzmaService.Compress(_testData, out var compressed);
        return compressed;
    }

    [Benchmark]
    public byte[] BZip2_Compress()
    {
        _ = _bzip2Service.Compress(_testData, out var compressed);
        return compressed;
    }

    [Benchmark]
    public byte[] XZ_Compress()
    {
        _ = _xzService.Compress(_testData, out var compressed);
        return compressed;
    }

    // Decompression Benchmarks
    [Benchmark]
    public byte[] GZip_Decompress()
    {
        _ = _gzipService.Decompress(_compressedGZip, out var decompressed);
        return decompressed;
    }

    [Benchmark]
    public byte[] Deflate_Decompress()
    {
        _ = _deflateService.Decompress(_compressedDeflate, out var decompressed);
        return decompressed;
    }

    [Benchmark]
    public byte[] Zstd_Decompress()
    {
        _ = _zstdService.Decompress(_compressedZstd, out var decompressed);
        return decompressed;
    }

    [Benchmark]
    public byte[] Snappier_Decompress()
    {
        _ = _snappierService.Decompress(_compressedSnappier, out var decompressed);
        return decompressed;
    }

    [Benchmark]
    public byte[] LZ4_Decompress()
    {
        _ = _lz4Service.Decompress(_compressedLZ4, out var decompressed);
        return decompressed;
    }

    [Benchmark]
    public byte[] LZMA_Decompress()
    {
        _ = _lzmaService.Decompress(_compressedLZMA, out var decompressed);
        return decompressed;
    }

    [Benchmark]
    public byte[] BZip2_Decompress()
    {
        _ = _bzip2Service.Decompress(_compressedBZip2, out var decompressed);
        return decompressed;
    }

    [Benchmark]
    public byte[] XZ_Decompress()
    {
        _ = _xzService.Decompress(_compressedXZ, out var decompressed);
        return decompressed;
    }
#if !NETSTANDARD2_0
    private CompressionService _brotliService = null!;
    private CompressionService _zlibService = null!;
#endif
#if !NETSTANDARD2_0
    private byte[] _compressedBrotli = null!;
    private byte[] _compressedZlib = null!;
#endif

#if !NETSTANDARD2_0
    [Benchmark]
    public byte[] Brotli_Compress()
    {
        _ = _brotliService.Compress(_testData, out var compressed);
        return compressed;
    }

    [Benchmark]
    public byte[] ZLib_Compress()
    {
        _ = _zlibService.Compress(_testData, out var compressed);
        return compressed;
    }
#endif

#if !NETSTANDARD2_0
    [Benchmark]
    public byte[] Brotli_Decompress()
    {
        _ = _brotliService.Decompress(_compressedBrotli, out var decompressed);
        return decompressed;
    }

    [Benchmark]
    public byte[] ZLib_Decompress()
    {
        _ = _zlibService.Decompress(_compressedZlib, out var decompressed);
        return decompressed;
    }
#endif
}