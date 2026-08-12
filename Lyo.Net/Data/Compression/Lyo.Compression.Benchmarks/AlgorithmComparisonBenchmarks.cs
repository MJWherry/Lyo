using BenchmarkDotNet.Attributes;
using Lyo.Benchmark;
using Lyo.Benchmark.Data;
using Lyo.Compression.BZip2;
using Lyo.Compression.Compressors;
using Lyo.Compression.Lz4;
using Lyo.Compression.Lzma;
using Lyo.Compression.Models;
using Lyo.Compression.Snappier;
using Lyo.Compression.Xz;
using Lyo.Compression.Zstd;

namespace Lyo.Compression.Benchmarks;

/// <summary>Benchmarks comparing different compression algorithms</summary>
[ComparisonSuite(Baseline = "GZip")]
[BenchmarkDescription(
    "Compresses and decompresses the same seeded deterministic (incompressible) buffer with every supported algorithm to compare raw speed at each payload size. Decompress cases reuse output compressed once in setup.")]
[BenchmarkParameter(
    "DataSize", Unit = "bytes", Description = "Size of the seeded input buffer (100 MiB, 250 MiB, 500 MiB); data is incompressible so ratio is not meaningful here.")]
[BenchmarkSla(
    MinThroughputMbps = 30, SizeParam = "DataSize", MinThroughputSizeBytes = BenchmarkData.BufferedSize100MiB,
    Standard =
        "General-purpose codecs (GZip/Deflate/Zstd/LZ4/Snappy/Brotli/ZLib) should sustain >= 30 MB/s on bulk (>= 100 MiB) data. High-ratio codecs (LZMA/XZ/BZip2) declare their own, lower per-method floors because they trade speed for ratio.")]
public class AlgorithmComparisonBenchmarks : LyoBenchmarkBase
{
    private static readonly ICompressorFactory[] AllFactories = [
        new GZipCompressorFactory(), new DeflateCompressorFactory(),
#if !NETSTANDARD2_0
        new BrotliCompressorFactory(), new ZLibCompressorFactory(),
#endif
        new Lz4CompressorFactory(), new LzmaCompressorFactory(), new SnappierCompressorFactory(), new ZstdCompressorFactory(), new BZip2CompressorFactory(),
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

    [Params(BenchmarkData.BufferedSize100MiB, BenchmarkData.BufferedSize250MiB, BenchmarkData.BufferedSize500MiB)]
    public int DataSize { get; set; }

    /// <inheritdoc />
    protected override void OnGlobalSetup()
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
        _testData = BenchmarkData.DeterministicBytes(DataSize);
    }

    [GlobalSetup(Target = nameof(GZip_Decompress))]
    public void SetupGZipDecompress()
    {
        EnsureGlobalSetup();
        _ = _gzipService.Compress(_testData, out _compressedGZip);
    }

    [GlobalSetup(Target = nameof(Deflate_Decompress))]
    public void SetupDeflateDecompress()
    {
        EnsureGlobalSetup();
        _ = _deflateService.Compress(_testData, out _compressedDeflate);
    }

    [GlobalSetup(Target = nameof(Zstd_Decompress))]
    public void SetupZstdDecompress()
    {
        EnsureGlobalSetup();
        _ = _zstdService.Compress(_testData, out _compressedZstd);
    }

    [GlobalSetup(Target = nameof(Snappier_Decompress))]
    public void SetupSnappierDecompress()
    {
        EnsureGlobalSetup();
        _ = _snappierService.Compress(_testData, out _compressedSnappier);
    }

    [GlobalSetup(Target = nameof(LZ4_Decompress))]
    public void SetupLz4Decompress()
    {
        EnsureGlobalSetup();
        _ = _lz4Service.Compress(_testData, out _compressedLZ4);
    }

    [GlobalSetup(Target = nameof(LZMA_Decompress))]
    public void SetupLzmaDecompress()
    {
        EnsureGlobalSetup();
        _ = _lzmaService.Compress(_testData, out _compressedLZMA);
    }

    [GlobalSetup(Target = nameof(BZip2_Decompress))]
    public void SetupBZip2Decompress()
    {
        EnsureGlobalSetup();
        _ = _bzip2Service.Compress(_testData, out _compressedBZip2);
    }

    [GlobalSetup(Target = nameof(XZ_Decompress))]
    public void SetupXzDecompress()
    {
        EnsureGlobalSetup();
        _ = _xzService.Compress(_testData, out _compressedXZ);
    }

#if !NETSTANDARD2_0
    [GlobalSetup(Target = nameof(Brotli_Decompress))]
    public void SetupBrotliDecompress()
    {
        EnsureGlobalSetup();
        _ = _brotliService.Compress(_testData, out _compressedBrotli);
    }

    [GlobalSetup(Target = nameof(ZLib_Decompress))]
    public void SetupZLibDecompress()
    {
        EnsureGlobalSetup();
        _ = _zlibService.Compress(_testData, out _compressedZlib);
    }
#endif

    // Compression Benchmarks
    [Benchmark(Baseline = true)]
    [ComparisonAxis("Compress")]
    public byte[] GZip_Compress()
    {
        _ = _gzipService.Compress(_testData, out var compressed);
        return compressed;
    }

    [Benchmark]
    [ComparisonAxis("Compress")]
    public byte[] Deflate_Compress()
    {
        _ = _deflateService.Compress(_testData, out var compressed);
        return compressed;
    }

    [Benchmark]
    [ComparisonAxis("Compress")]
    public byte[] Zstd_Compress()
    {
        _ = _zstdService.Compress(_testData, out var compressed);
        return compressed;
    }

    [Benchmark]
    [ComparisonAxis("Compress")]
    public byte[] Snappier_Compress()
    {
        _ = _snappierService.Compress(_testData, out var compressed);
        return compressed;
    }

    [Benchmark]
    [ComparisonAxis("Compress")]
    public byte[] LZ4_Compress()
    {
        _ = _lz4Service.Compress(_testData, out var compressed);
        return compressed;
    }

    [Benchmark]
    [ComparisonAxis("Compress")]
    [BenchmarkSla(
        MinThroughputMbps = 2, SizeParam = "DataSize", MinThroughputSizeBytes = BenchmarkData.BufferedSize100MiB,
        Standard =
            "LZMA is a high-ratio dictionary codec tuned for size, not speed; single-digit MB/s on incompressible data is expected. Choose it when storage/bandwidth savings outweigh CPU.")]
    public byte[] LZMA_Compress()
    {
        _ = _lzmaService.Compress(_testData, out var compressed);
        return compressed;
    }

    [Benchmark]
    [ComparisonAxis("Compress")]
    [BenchmarkSla(
        MinThroughputMbps = 4, SizeParam = "DataSize", MinThroughputSizeBytes = BenchmarkData.BufferedSize100MiB,
        Standard =
            "BZip2 (Burrows-Wheeler) favors ratio over speed; a few MB/s is expected. NOTE: the current SharpZipLib compressor also allocates ~700x the input on compress (decompress is normal) - see the BZip2 allocation investigation.")]
    public byte[] BZip2_Compress()
    {
        _ = _bzip2Service.Compress(_testData, out var compressed);
        return compressed;
    }

    [Benchmark]
    [ComparisonAxis("Compress")]
    public byte[] XZ_Compress()
    {
        _ = _xzService.Compress(_testData, out var compressed);
        return compressed;
    }

    // Decompression Benchmarks
    [Benchmark]
    [ComparisonAxis("Decompress")]
    public byte[] GZip_Decompress()
    {
        _ = _gzipService.Decompress(_compressedGZip, out var decompressed);
        return decompressed;
    }

    [Benchmark]
    [ComparisonAxis("Decompress")]
    public byte[] Deflate_Decompress()
    {
        _ = _deflateService.Decompress(_compressedDeflate, out var decompressed);
        return decompressed;
    }

    [Benchmark]
    [ComparisonAxis("Decompress")]
    public byte[] Zstd_Decompress()
    {
        _ = _zstdService.Decompress(_compressedZstd, out var decompressed);
        return decompressed;
    }

    [Benchmark]
    [ComparisonAxis("Decompress")]
    public byte[] Snappier_Decompress()
    {
        _ = _snappierService.Decompress(_compressedSnappier, out var decompressed);
        return decompressed;
    }

    [Benchmark]
    [ComparisonAxis("Decompress")]
    public byte[] LZ4_Decompress()
    {
        _ = _lz4Service.Decompress(_compressedLZ4, out var decompressed);
        return decompressed;
    }

    [Benchmark]
    [ComparisonAxis("Decompress")]
    [BenchmarkSla(
        MinThroughputMbps = 4, SizeParam = "DataSize", MinThroughputSizeBytes = BenchmarkData.BufferedSize100MiB,
        Standard = "LZMA decode is range-coder bound and runs single-digit MB/s; acceptable for a high-ratio codec chosen for size.")]
    public byte[] LZMA_Decompress()
    {
        _ = _lzmaService.Decompress(_compressedLZMA, out var decompressed);
        return decompressed;
    }

    [Benchmark]
    [ComparisonAxis("Decompress")]
    [BenchmarkSla(
        MinThroughputMbps = 12, SizeParam = "DataSize", MinThroughputSizeBytes = BenchmarkData.BufferedSize100MiB,
        Standard = "BZip2 decode (inverse Burrows-Wheeler) is bounded by its block transform; ~15 MB/s is the expected range for this high-ratio codec.")]
    public byte[] BZip2_Decompress()
    {
        _ = _bzip2Service.Decompress(_compressedBZip2, out var decompressed);
        return decompressed;
    }

    [Benchmark]
    [ComparisonAxis("Decompress")]
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
    [ComparisonAxis("Compress")]
    public byte[] Brotli_Compress()
    {
        _ = _brotliService.Compress(_testData, out var compressed);
        return compressed;
    }

    [Benchmark]
    [ComparisonAxis("Compress")]
    public byte[] ZLib_Compress()
    {
        _ = _zlibService.Compress(_testData, out var compressed);
        return compressed;
    }
#endif

#if !NETSTANDARD2_0
    [Benchmark]
    [ComparisonAxis("Decompress")]
    public byte[] Brotli_Decompress()
    {
        _ = _brotliService.Decompress(_compressedBrotli, out var decompressed);
        return decompressed;
    }

    [Benchmark]
    [ComparisonAxis("Decompress")]
    public byte[] ZLib_Decompress()
    {
        _ = _zlibService.Decompress(_compressedZlib, out var decompressed);
        return decompressed;
    }
#endif
}