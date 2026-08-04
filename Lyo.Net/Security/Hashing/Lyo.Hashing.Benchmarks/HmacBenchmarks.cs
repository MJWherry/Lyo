using BenchmarkDotNet.Attributes;
using Lyo.Benchmarking;
using Lyo.Benchmarking.Data;

namespace Lyo.Hashing.Benchmarks;

/// <summary>Benchmarks the keyed-hash (HMAC) helpers across payload sizes.</summary>
[BenchmarkDescription("Keyed-hash (HMAC) of a seeded deterministic payload using a fixed 32-byte seeded key, comparing SHA-256 vs SHA-512.")]
[BenchmarkParameter("DataSize", Unit = "bytes", Description = "Size of the seeded input buffer being HMAC'd (1 KB, 1 MB).")]
[BenchmarkSla(MinThroughputMbps = 150, SizeParam = "DataSize", Standard = "Keyed hashing (HMAC over SHA-2) should sustain >= 150 MB/s on production hardware.")]
public class HmacBenchmarks
{
    private readonly IHashingService _hashing = HashingService.Shared;
    private byte[] _data = null!;
    private byte[] _key = null!;

    [Params(1024, BenchmarkData.MiB)]
    public int DataSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _key = BenchmarkData.DeterministicBytes(32);
        _data = BenchmarkData.DeterministicBytes(DataSize);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkDescription("HMAC-SHA-256 over the payload (baseline).")]
    public byte[] HmacSha256() => _hashing.HmacSha256(_key, _data);

    [Benchmark]
    [BenchmarkDescription("HMAC-SHA-512 over the payload.")]
    public byte[] HmacSha512() => _hashing.HmacSha512(_key, _data);
}
