using BenchmarkDotNet.Attributes;
using Lyo.Benchmarking;
using Lyo.Benchmarking.Data;

namespace Lyo.Hashing.Benchmarks;

/// <summary>Compares the static <see cref="Hasher" /> hot path against the injectable <see cref="HashingService" /> facade for SHA-256.</summary>
[BenchmarkDescription("Measures the overhead of the injectable HashingService facade against the static Hasher hot path for the same SHA-256 digest.")]
[BenchmarkParameter("DataSize", Unit = "bytes", Description = "Size of the seeded input buffer being hashed (1 KB, 1 MB).")]
[BenchmarkSla(
    MinThroughputMbps = 200, SizeParam = "DataSize",
    Standard = "SHA-256 on hardware-accelerated CPUs should sustain >= 200 MB/s; the service facade must not materially erode that throughput.")]
public class HasherSurfaceBenchmarks
{
    private readonly IHashingService _service = HashingService.Shared;
    private byte[] _data = null!;

    [Params(1024, BenchmarkData.MiB)]
    public int DataSize { get; set; }

    [GlobalSetup]
    public void Setup() => _data = BenchmarkData.DeterministicBytes(DataSize);

    [Benchmark(Baseline = true)]
    [BenchmarkDescription("SHA-256 via the static Hasher.ComputeSha256 hot path (baseline).")]
    public byte[] StaticHasher_Sha256() => Hasher.ComputeSha256(_data);

    [Benchmark]
    [BenchmarkDescription("SHA-256 via the injectable IHashingService facade (measures abstraction overhead).")]
    public byte[] Service_Sha256() => _service.Hash(ContentDigestAlgorithm.Sha256, _data);
}
