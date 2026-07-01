using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using Lyo.Benchmarking;

// ReSharper disable InconsistentNaming

namespace Lyo.Hashing.Benchmarks;

/// <summary>Benchmarks for large file hashing/checksumming using the streaming APIs.</summary>
[BenchmarkDescription(
    "Streaming SHA-256 / SHA-512 digests and CRC-32 / CRC-64 checksums of 100 MB / 1 GB / 2 GB random data; large sizes stream through temp files to bound memory. Method names encode algorithm and size.")]
public class LargeFileStreamingBenchmarks
{
    private readonly IHashingService _hashing = HashingService.Shared;
    private Stream _data100MB = null!;
    private Stream _data1GB = null!;
    private Stream _data2GB = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Create test data streams (using FileStream for very large files to avoid memory issues)
        _data100MB = CreateTestDataStream(100 * 1024 * 1024); // 100 MB
        _data1GB = CreateTestDataStream(1024 * 1024 * 1024); // 1 GB
        _data2GB = CreateTestDataStream(2L * 1024 * 1024 * 1024); // 2 GB
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _data100MB.Dispose();
        _data1GB.Dispose();
        _data2GB.Dispose();
    }

    private static Stream CreateTestDataStream(long size)
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

    // SHA-256 Hashing Benchmarks
    [Benchmark]
    public byte[] Hash_Sha256_100MB()
    {
        _data100MB.Position = 0;
        return _hashing.Hash(ContentDigestAlgorithm.Sha256, _data100MB);
    }

    [Benchmark]
    public byte[] Hash_Sha256_1GB()
    {
        _data1GB.Position = 0;
        return _hashing.Hash(ContentDigestAlgorithm.Sha256, _data1GB);
    }

    [Benchmark]
    public byte[] Hash_Sha256_2GB()
    {
        _data2GB.Position = 0;
        return _hashing.Hash(ContentDigestAlgorithm.Sha256, _data2GB);
    }

    // SHA-512 Hashing Benchmarks
    [Benchmark]
    public byte[] Hash_Sha512_100MB()
    {
        _data100MB.Position = 0;
        return _hashing.Hash(ContentDigestAlgorithm.Sha512, _data100MB);
    }

    [Benchmark]
    public byte[] Hash_Sha512_1GB()
    {
        _data1GB.Position = 0;
        return _hashing.Hash(ContentDigestAlgorithm.Sha512, _data1GB);
    }

    [Benchmark]
    public byte[] Hash_Sha512_2GB()
    {
        _data2GB.Position = 0;
        return _hashing.Hash(ContentDigestAlgorithm.Sha512, _data2GB);
    }

    // CRC-32 Checksum Benchmarks
    [Benchmark]
    public byte[] Checksum_Crc32_100MB()
    {
        _data100MB.Position = 0;
        return _hashing.Checksum(ChecksumAlgorithm.Crc32, _data100MB);
    }

    [Benchmark]
    public byte[] Checksum_Crc32_1GB()
    {
        _data1GB.Position = 0;
        return _hashing.Checksum(ChecksumAlgorithm.Crc32, _data1GB);
    }

    [Benchmark]
    public byte[] Checksum_Crc32_2GB()
    {
        _data2GB.Position = 0;
        return _hashing.Checksum(ChecksumAlgorithm.Crc32, _data2GB);
    }

    // CRC-64 Checksum Benchmarks
    [Benchmark]
    public byte[] Checksum_Crc64_100MB()
    {
        _data100MB.Position = 0;
        return _hashing.Checksum(ChecksumAlgorithm.Crc64, _data100MB);
    }

    [Benchmark]
    public byte[] Checksum_Crc64_1GB()
    {
        _data1GB.Position = 0;
        return _hashing.Checksum(ChecksumAlgorithm.Crc64, _data1GB);
    }

    [Benchmark]
    public byte[] Checksum_Crc64_2GB()
    {
        _data2GB.Position = 0;
        return _hashing.Checksum(ChecksumAlgorithm.Crc64, _data2GB);
    }
}