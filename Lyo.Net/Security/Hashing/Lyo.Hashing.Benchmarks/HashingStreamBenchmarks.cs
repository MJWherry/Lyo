using BenchmarkDotNet.Attributes;
using Lyo.Benchmark;
using Lyo.Benchmark.Data;

namespace Lyo.Hashing.Benchmarks;

/// <summary>Benchmarks incremental stream hashing vs one-shot buffer hashing for the same data.</summary>
[BenchmarkDescription("Compares one-shot buffer hashing against incremental stream hashing (81920-byte reads) of the same seeded deterministic payload.")]
[BenchmarkParameter("DataSize", Unit = "bytes", Description = "Size of the seeded input buffer being hashed (1 MB, 10 MB).")]
[BenchmarkSla(
    MinThroughputMbps = 150, SizeParam = "DataSize",
    Standard = "Both one-shot and incremental SHA-256 should sustain >= 150 MB/s; streaming must not collapse throughput versus the one-shot path.")]
public class HashingStreamBenchmarks
{
    private readonly IHashingService _hashing = HashingService.Shared;
    private byte[] _data = null!;

    [Params(BenchmarkData.MiB, 10 * BenchmarkData.MiB)]
    public int DataSize { get; set; }

    [GlobalSetup]
    public void Setup() => _data = BenchmarkData.DeterministicBytes(DataSize);

    [Benchmark(Baseline = true)]
    [BenchmarkDescription("One-shot SHA-256 over the full in-memory buffer (baseline).")]
    public byte[] OneShot_Sha256() => _hashing.Hash(ContentDigestAlgorithm.Sha256, _data);

    [Benchmark]
    [BenchmarkDescription("Incremental SHA-256 reading the payload through a hashing stream in 80 KB chunks.")]
    public byte[] Stream_Sha256()
    {
        using var source = new MemoryStream(_data, false);
        using var hashing = _hashing.CreateHashingStream(source, ContentDigestAlgorithm.Sha256);
        var buffer = new byte[81920];
        while (hashing.Read(buffer, 0, buffer.Length) > 0) { }

        return hashing.GetHash();
    }
}