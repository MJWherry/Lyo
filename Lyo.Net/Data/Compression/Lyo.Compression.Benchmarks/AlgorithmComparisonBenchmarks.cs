using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Lyo.Compression.BZip2;
using Lyo.Compression.Compressors;
using Lyo.Compression.LZ4;
using Lyo.Compression.LZMA;
using Lyo.Compression.Models;
using Lyo.Compression.Snappier;
using Lyo.Compression.XZ;
using Lyo.Compression.Zstd;

namespace Lyo.Compression.Benchmarks;

/// <summary>Benchmarks comparing different compression algorithms</summary>
[SimpleJob(RuntimeMoniker.HostProcess)]
[MemoryDiagnoser]
public class AlgorithmComparisonBenchmarks
{
    private static readonly ICompressorFactory[] AllFactories = [
        new GZipCompressorFactory(),
        new DeflateCompressorFactory(),
#if !NETSTANDARD2_0
        new BrotliCompressorFactory(),
        new ZLibCompressorFactory(),
#endif
        new Lz4CompressorFactory(),
        new LzmaCompressorFactory(),
        new SnappierCompressorFactory(),
        new ZstdCompressorFactory(),
        new BZip2CompressorFactory(),
        new XzCompressorFactory()
    ];

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
        _gzipService = new(AllFactories, options: new() { DefaultAlgorithm = CompressionAlgorithm.GZip, EnableMetrics = false });
        _deflateService = new(AllFactories, options: new() { DefaultAlgorithm = CompressionAlgorithm.Deflate, EnableMetrics = false });
        _zstdService = new(AllFactories, options: new() { DefaultAlgorithm = ZstdCompressionAlgorithm.Instance, EnableMetrics = false });
        _snappierService = new(AllFactories, options: new() { DefaultAlgorithm = SnappierCompressionAlgorithm.Instance, EnableMetrics = false });
        _lz4Service = new(AllFactories, options: new() { DefaultAlgorithm = Lz4CompressionAlgorithm.Instance, EnableMetrics = false });
        _lzmaService = new(AllFactories, options: new() { DefaultAlgorithm = LzmaCompressionAlgorithm.Instance, EnableMetrics = false });
        _bzip2Service = new(AllFactories, options: new() { DefaultAlgorithm = BZip2CompressionAlgorithm.Instance, EnableMetrics = false });
        _xzService = new(AllFactories, options: new() { DefaultAlgorithm = XzCompressionAlgorithm.Instance, EnableMetrics = false });
#if !NETSTANDARD2_0
        _brotliService = new(AllFactories, options: new() { DefaultAlgorithm = CompressionAlgorithm.Brotli, EnableMetrics = false });
        _zlibService = new(AllFactories, options: new() { DefaultAlgorithm = CompressionAlgorithm.ZLib, EnableMetrics = false });
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
