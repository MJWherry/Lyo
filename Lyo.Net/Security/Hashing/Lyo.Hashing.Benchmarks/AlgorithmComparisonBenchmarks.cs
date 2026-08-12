using BenchmarkDotNet.Attributes;
using Lyo.Benchmark;
using Lyo.Benchmark.Data;

namespace Lyo.Hashing.Benchmarks;

/// <summary>Benchmarks comparing the content-digest algorithms exposed by <see cref="IHashingService" />.</summary>
[ComparisonSuite(Baseline = "Sha256")]
[BenchmarkDescription("Hashes the same seeded deterministic byte buffer with SHA-256/384/512 and MD5 to compare digest throughput at each payload size.")]
[BenchmarkParameter("DataSize", Unit = "bytes", Description = "Size of the seeded input buffer being hashed (1 KB, 1 MB, 10 MB).")]
[BenchmarkSla(
    MinThroughputMbps = 150, SizeParam = "DataSize",
    Standard = "Modern content hashing (SHA-2 family) on hardware-accelerated CPUs should sustain hundreds of MB/s; >= 150 MB/s is a conservative production floor.")]
public class AlgorithmComparisonBenchmarks
{
    private readonly IHashingService _hashing = HashingService.Shared;
    private byte[] _data = null!;

    [Params(1024, BenchmarkData.MiB, 10 * BenchmarkData.MiB)]
    public int DataSize { get; set; }

    [GlobalSetup]
    public void Setup() => _data = BenchmarkData.DeterministicBytes(DataSize);

    [Benchmark(Baseline = true)]
    [ComparisonAxis("Hash")]
    [BenchmarkDescription("SHA-256 digest of the payload (baseline).")]
    public byte[] Sha256_Hash() => _hashing.Hash(ContentDigestAlgorithm.Sha256, _data);

    [Benchmark]
    [ComparisonAxis("Hash")]
    [BenchmarkDescription("SHA-384 digest of the payload.")]
    public byte[] Sha384_Hash() => _hashing.Hash(ContentDigestAlgorithm.Sha384, _data);

    [Benchmark]
    [ComparisonAxis("Hash")]
    [BenchmarkDescription("SHA-512 digest of the payload.")]
    public byte[] Sha512_Hash() => _hashing.Hash(ContentDigestAlgorithm.Sha512, _data);

    [Benchmark]
    [ComparisonAxis("Hash")]
    [BenchmarkDescription("MD5 digest of the payload (legacy, non-cryptographic comparison point).")]
    public byte[] Md5_Hash() => _hashing.Hash(ContentDigestAlgorithm.Md5, _data);
}
