using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Lyo.Compression.Models;

namespace Lyo.Compression.Benchmarks;

/// <summary>Benchmarks for large file compression/decompression using streaming APIs</summary>
[SimpleJob(RuntimeMoniker.HostProcess)]
[MemoryDiagnoser]
public class LargeFileStreamingBenchmarks
{
    private Stream _compressed100MBGZip = null!;
    private Stream _compressed100MBZstd = null!;
    private Stream _compressed1GBGZip = null!;
    private Stream _compressed1GBZstd = null!;
    private Stream _compressed2GBGZip = null!;
    private Stream _compressed2GBZstd = null!;
    private Stream _data100MB = null!;
    private Stream _data1GB = null!;
    private Stream _data2GB = null!;
    private CompressionService _gzipService = null!;
    private CompressionService _zstdService = null!;

    [GlobalSetup]
    public void Setup()
    {
        var gzipOptions = new CompressionServiceOptions { DefaultAlgorithm = CompressionAlgorithm.GZip, EnableMetrics = false };
        _gzipService = new(options: gzipOptions);
        var zstdOptions = new CompressionServiceOptions { DefaultAlgorithm = CompressionAlgorithm.ZstdSharp, EnableMetrics = false };
        _zstdService = new(options: zstdOptions);

        // Create test data streams (using FileStream for very large files to avoid memory issues)
        _data100MB = CreateTestDataStream(100 * 1024 * 1024); // 100 MB
        _data1GB = CreateTestDataStream(1024 * 1024 * 1024); // 1 GB
        _data2GB = CreateTestDataStream(2L * 1024 * 1024 * 1024); // 2 GB

        // Pre-compress data for decompression benchmarks
        // Use FileStream for large compressed files (1GB+) to avoid MemoryStream size limits
        _compressed100MBGZip = new MemoryStream();
        _compressed1GBGZip = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024 * 1024, FileOptions.DeleteOnClose);
        _compressed2GBGZip = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024 * 1024, FileOptions.DeleteOnClose);
        _compressed100MBZstd = new MemoryStream();
        _compressed1GBZstd = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024 * 1024, FileOptions.DeleteOnClose);
        _compressed2GBZstd = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024 * 1024, FileOptions.DeleteOnClose);
        _data100MB.Position = 0;
        _gzipService.CompressAsync(_data100MB, _compressed100MBGZip).Wait();
        _data1GB.Position = 0;
        _gzipService.CompressAsync(_data1GB, _compressed1GBGZip).Wait();
        _data2GB.Position = 0;
        _gzipService.CompressAsync(_data2GB, _compressed2GBGZip).Wait();
        _data100MB.Position = 0;
        _zstdService.CompressAsync(_data100MB, _compressed100MBZstd).Wait();
        _data1GB.Position = 0;
        _zstdService.CompressAsync(_data1GB, _compressed1GBZstd).Wait();
        _data2GB.Position = 0;
        _zstdService.CompressAsync(_data2GB, _compressed2GBZstd).Wait();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _data100MB?.Dispose();
        _data1GB?.Dispose();
        _data2GB?.Dispose();
        _compressed100MBGZip?.Dispose();
        _compressed1GBGZip?.Dispose();
        _compressed2GBGZip?.Dispose();
        _compressed100MBZstd?.Dispose();
        _compressed1GBZstd?.Dispose();
        _compressed2GBZstd?.Dispose();
    }

    private Stream CreateTestDataStream(long size)
    {
        // For very large files (1GB+), use a FileStream to avoid memory issues
        if (size >= 1024 * 1024 * 1024) // 1 GB or larger
        {
            var tempFile = Path.GetTempFileName();
            using (var fileStream = File.Create(tempFile)) {
                var buffer = new byte[1024 * 1024]; // 1 MB buffer
                var rng = RandomNumberGenerator.Create();
                var remaining = size;
                while (remaining > 0) {
                    var toWrite = (int)Math.Min(remaining, buffer.Length);
                    rng.GetBytes(buffer, 0, toWrite);
                    fileStream.Write(buffer, 0, toWrite);
                    remaining -= toWrite;
                }
            }

            return new FileStream(tempFile, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, FileOptions.DeleteOnClose);
        }

        // For smaller files, use MemoryStream
        var data = new byte[size];
        RandomNumberGenerator.Fill(data);
        return new MemoryStream(data);
    }

    // GZip Compression Benchmarks
    [Benchmark]
    public async Task Compress_GZip_100MB()
    {
        _data100MB.Position = 0;
        var output = new MemoryStream();
        await _gzipService.CompressAsync(_data100MB, output);
    }

    [Benchmark]
    public async Task Compress_GZip_1GB()
    {
        _data1GB.Position = 0;
        var output = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024 * 1024, FileOptions.DeleteOnClose);
        try {
            await _gzipService.CompressAsync(_data1GB, output);
        }
        finally {
            await output.DisposeAsync();
        }
    }

    [Benchmark]
    public async Task Compress_GZip_2GB()
    {
        _data2GB.Position = 0;
        var output = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024 * 1024, FileOptions.DeleteOnClose);
        try {
            await _gzipService.CompressAsync(_data2GB, output);
        }
        finally {
            await output.DisposeAsync();
        }
    }

    // GZip Decompression Benchmarks
    [Benchmark]
    public async Task Decompress_GZip_100MB()
    {
        _compressed100MBGZip.Position = 0;
        var output = new MemoryStream();
        await _gzipService.DecompressAsync(_compressed100MBGZip, output);
    }

    [Benchmark]
    public async Task Decompress_GZip_1GB()
    {
        _compressed1GBGZip.Position = 0;
        var output = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024 * 1024, FileOptions.DeleteOnClose);
        try {
            await _gzipService.DecompressAsync(_compressed1GBGZip, output);
        }
        finally {
            await output.DisposeAsync();
        }
    }

    [Benchmark]
    public async Task Decompress_GZip_2GB()
    {
        _compressed2GBGZip.Position = 0;
        var output = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024 * 1024, FileOptions.DeleteOnClose);
        try {
            await _gzipService.DecompressAsync(_compressed2GBGZip, output);
        }
        finally {
            await output.DisposeAsync();
        }
    }

    // Zstd Compression Benchmarks
    [Benchmark]
    public async Task Compress_Zstd_100MB()
    {
        _data100MB.Position = 0;
        var output = new MemoryStream();
        await _zstdService.CompressAsync(_data100MB, output);
    }

    [Benchmark]
    public async Task Compress_Zstd_1GB()
    {
        _data1GB.Position = 0;
        var output = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024 * 1024, FileOptions.DeleteOnClose);
        try {
            await _zstdService.CompressAsync(_data1GB, output);
        }
        finally {
            await output.DisposeAsync();
        }
    }

    [Benchmark]
    public async Task Compress_Zstd_2GB()
    {
        _data2GB.Position = 0;
        var output = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024 * 1024, FileOptions.DeleteOnClose);
        try {
            await _zstdService.CompressAsync(_data2GB, output);
        }
        finally {
            await output.DisposeAsync();
        }
    }

    // Zstd Decompression Benchmarks
    [Benchmark]
    public async Task Decompress_Zstd_100MB()
    {
        _compressed100MBZstd.Position = 0;
        var output = new MemoryStream();
        await _zstdService.DecompressAsync(_compressed100MBZstd, output);
    }

    [Benchmark]
    public async Task Decompress_Zstd_1GB()
    {
        _compressed1GBZstd.Position = 0;
        var output = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024 * 1024, FileOptions.DeleteOnClose);
        try {
            await _zstdService.DecompressAsync(_compressed1GBZstd, output);
        }
        finally {
            await output.DisposeAsync();
        }
    }

    [Benchmark]
    public async Task Decompress_Zstd_2GB()
    {
        _compressed2GBZstd.Position = 0;
        var output = new FileStream(Path.GetTempFileName(), FileMode.Create, FileAccess.ReadWrite, FileShare.None, 1024 * 1024, FileOptions.DeleteOnClose);
        try {
            await _zstdService.DecompressAsync(_compressed2GBZstd, output);
        }
        finally {
            await output.DisposeAsync();
        }
    }
}