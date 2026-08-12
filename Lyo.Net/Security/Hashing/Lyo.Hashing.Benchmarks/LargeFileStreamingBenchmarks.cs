using BenchmarkDotNet.Attributes;
using Lyo.Benchmark;
using Lyo.Benchmark.Data;

namespace Lyo.Hashing.Benchmarks;

/// <summary>Benchmarks for large file hashing/checksumming using the streaming APIs.</summary>
[BenchmarkDescription(
    "Streaming SHA-256 / SHA-512 digests and CRC-32 / CRC-64 checksums of seeded deterministic payloads (100 MiB–2 GiB); all file I/O uses the suite IOTemp session.")]
[BenchmarkParameter("DataSize", Unit = "bytes", Description = "Input size: 100, 250, 500, 750 MiB, 1 GiB, 1.5 GiB, 2 GiB.")]
public class LargeFileStreamingBenchmarks : LyoBenchmarkBase
{
    private readonly IHashingService _hashing = HashingService.Shared;
    private Stream _data = null!;

    [Params(
        BenchmarkData.StreamingSize100MiB,
        BenchmarkData.StreamingSize250MiB,
        BenchmarkData.StreamingSize500MiB,
        BenchmarkData.StreamingSize750MiB,
        BenchmarkData.StreamingSize1GiB,
        BenchmarkData.StreamingSize15GiB,
        BenchmarkData.StreamingSize2GiB)]
    public long DataSize { get; set; }

    /// <inheritdoc />
    protected override void OnGlobalSetup() => _data = CreateSeededFile(DataSize);

    /// <inheritdoc />
    protected override void OnGlobalCleanup() => _data?.Dispose();

    [Benchmark]
    public byte[] Hash_Sha256()
    {
        _data.Position = 0;
        return _hashing.Hash(ContentDigestAlgorithm.Sha256, _data);
    }

    [Benchmark]
    public byte[] Hash_Sha512()
    {
        _data.Position = 0;
        return _hashing.Hash(ContentDigestAlgorithm.Sha512, _data);
    }

    [Benchmark]
    public byte[] Checksum_Crc32()
    {
        _data.Position = 0;
        return _hashing.Checksum(ChecksumAlgorithm.Crc32, _data);
    }

    [Benchmark]
    public byte[] Checksum_Crc64()
    {
        _data.Position = 0;
        return _hashing.Checksum(ChecksumAlgorithm.Crc64, _data);
    }
}
