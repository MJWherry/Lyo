using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using Lyo.Benchmarking;

namespace Lyo.Hashing.Benchmarks;

/// <summary>Benchmarks the keyed-hash (HMAC) helpers across payload sizes.</summary>
[BenchmarkDescription("Keyed-hash (HMAC) of a random payload using a fixed 32-byte random key, comparing SHA-256 vs SHA-512.")]
[BenchmarkParameter("DataSize", Unit = "bytes", Description = "Size of the random input buffer being HMAC'd (1 KB, 1 MB).")]
[BenchmarkSla(MinThroughputMbps = 150, SizeParam = "DataSize", Standard = "Keyed hashing (HMAC over SHA-2) should sustain >= 150 MB/s on production hardware.")]
public class HmacBenchmarks
{
    private readonly IHashingService _hashing = HashingService.Shared;
    private byte[] _data = null!;
    private byte[] _key = null!;

    [Params(1024, 1024 * 1024)] // 1 KB, 1 MB
    public int DataSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _key = new byte[32];
        RandomNumberGenerator.Fill(_key);
        _data = new byte[DataSize];
        RandomNumberGenerator.Fill(_data);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkDescription("HMAC-SHA-256 over the payload (baseline).")]
    public byte[] HmacSha256() => _hashing.HmacSha256(_key, _data);

    [Benchmark]
    [BenchmarkDescription("HMAC-SHA-512 over the payload.")]
    public byte[] HmacSha512() => _hashing.HmacSha512(_key, _data);
}