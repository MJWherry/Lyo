using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Lyo.Compression.Models;

namespace Lyo.Compression.Benchmarks;

[SimpleJob(RuntimeMoniker.HostProcess)]
[MemoryDiagnoser]
public class GZipCompressionBenchmarks
{
    private byte[] _compressedLarge = null!;
    private byte[] _compressedMedium = null!;
    private byte[] _compressedSmall = null!;
    private CompressionService _compressionService = null!;
    private byte[] _largeData = null!;
    private byte[] _mediumData = null!;
    private byte[] _smallData = null!;

    [GlobalSetup]
    public void Setup()
    {
        var options = new CompressionServiceOptions { DefaultAlgorithm = CompressionAlgorithm.GZip, EnableMetrics = false };
        _compressionService = new(options: options);

        // Generate test data
        _smallData = new byte[1024]; // 1 KB
        _mediumData = new byte[1024 * 1024]; // 1 MB
        _largeData = new byte[10 * 1024 * 1024]; // 10 MB
        RandomNumberGenerator.Fill(_smallData);
        RandomNumberGenerator.Fill(_mediumData);
        RandomNumberGenerator.Fill(_largeData);

        // Pre-compress data for decompression benchmarks
        _ = _compressionService.Compress(_smallData, out _compressedSmall);
        _ = _compressionService.Compress(_mediumData, out _compressedMedium);
        _ = _compressionService.Compress(_largeData, out _compressedLarge);
    }

    [Benchmark]
    public byte[] Compress_1KB()
    {
        _ = _compressionService.Compress(_smallData, out var compressed);
        return compressed;
    }

    [Benchmark]
    public byte[] Compress_1MB()
    {
        _ = _compressionService.Compress(_mediumData, out var compressed);
        return compressed;
    }

    [Benchmark]
    public byte[] Compress_10MB()
    {
        _ = _compressionService.Compress(_largeData, out var compressed);
        return compressed;
    }

    [Benchmark]
    public byte[] Decompress_1KB()
    {
        _ = _compressionService.Decompress(_compressedSmall, out var decompressed);
        return decompressed;
    }

    [Benchmark]
    public byte[] Decompress_1MB()
    {
        _ = _compressionService.Decompress(_compressedMedium, out var decompressed);
        return decompressed;
    }

    [Benchmark]
    public byte[] Decompress_10MB()
    {
        _ = _compressionService.Decompress(_compressedLarge, out var decompressed);
        return decompressed;
    }
}