using BenchmarkDotNet.Attributes;
using Lyo.Benchmarking;
using Lyo.Benchmarking.Data;

namespace Lyo.Hashing.Benchmarks;

/// <summary>Benchmarks comparing the non-cryptographic checksum algorithms exposed by <see cref="Checksummer" />.</summary>
[ComparisonSuite(Baseline = "Crc32")]
[BenchmarkDescription("Checksums the same seeded deterministic byte buffer with CRC-32, CRC-32C, CRC-64/ECMA-182 and Adler-32 to compare non-cryptographic throughput at each payload size.")]
[BenchmarkParameter("DataSize", Unit = "bytes", Description = "Size of the seeded input buffer being checksummed (1 KB, 1 MB, 10 MB).")]
[BenchmarkSla(
    MinThroughputMbps = 300, SizeParam = "DataSize",
    Standard =
        "Non-cryptographic checksums (CRC/Adler) are far cheaper than SHA-2; hardware-accelerated CRC-32 reaches multiple GB/s while table-driven variants stay well above this conservative 300 MB/s production floor.")]
public class ChecksumComparisonBenchmarks
{
    private byte[] _data = null!;

    [Params(1024, BenchmarkData.MiB, 10 * BenchmarkData.MiB)]
    public int DataSize { get; set; }

    [GlobalSetup]
    public void Setup() => _data = BenchmarkData.DeterministicBytes(DataSize);

    [Benchmark(Baseline = true)]
    [ComparisonAxis("Checksum")]
    [BenchmarkDescription("CRC-32 (IEEE) value of the payload (baseline; hardware-accelerated via System.IO.Hashing).")]
    public ulong Crc32_Value() => Checksummer.ComputeValue(ChecksumAlgorithm.Crc32, _data);

    [Benchmark]
    [ComparisonAxis("Checksum")]
    [BenchmarkDescription("CRC-32C (Castagnoli) value of the payload (table-driven).")]
    public ulong Crc32C_Value() => Checksummer.ComputeValue(ChecksumAlgorithm.Crc32C, _data);

    [Benchmark]
    [ComparisonAxis("Checksum")]
    [BenchmarkDescription("CRC-64/ECMA-182 value of the payload (via System.IO.Hashing, vectorized where available).")]
    public ulong Crc64_Value() => Checksummer.ComputeValue(ChecksumAlgorithm.Crc64, _data);

    [Benchmark]
    [ComparisonAxis("Checksum")]
    [BenchmarkDescription("Adler-32 value of the payload (table-free running sums).")]
    public ulong Adler32_Value() => Checksummer.ComputeValue(ChecksumAlgorithm.Adler32, _data);
}
