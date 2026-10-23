using BenchmarkDotNet.Attributes;
using Lyo.Benchmark;
using Lyo.Benchmark.Data;

namespace Lyo.Csv.Benchmarks;

/// <summary>Times CSV serialization and parsing of a record with nested reference members.</summary>
[BenchmarkDescription(
    "Serializes and parses NestedRecord rows. The CSV writer flattens Address and Geo into leaf columns, so this captures the round-trip cost a flat row count hides.")]
[BenchmarkParameter("RowCount", Unit = "rows", Description = "Number of NestedRecord rows round-tripped (1,000 or 10,000).")]
[BenchmarkDataShape(typeof(NestedRecord), Notes = "Address nests a Geo coordinate (depth 2). The CSV writer flattens both into leaf columns.")]
[BenchmarkSla(MaxMeanMs = 200, Standard = "Round-tripping up to 10k nested records through CSV should complete within a couple hundred milliseconds.")]
public class CsvNestedBenchmarks
{
    private readonly CsvService _csv = new();
    private byte[] _bytes = null!;
    private List<NestedRecord> _rows = null!;

    [Params(1_000, 10_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _rows = NestedRecord.Generate(RowCount);
        _bytes = _csv.ExportToCsvBytes(_rows);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkDescription("Serialize the nested records to CSV bytes (writer flattens Address and Geo into columns).")]
    public byte[] ExportNestedBytes() => _csv.ExportToCsvBytes(_rows);

    [Benchmark]
    [BenchmarkDescription("Parse the flattened CSV bytes back into typed NestedRecords.")]
    public int ParseNestedBytes() => _csv.ParseBytes<NestedRecord>(_bytes).ToList().Count;
}