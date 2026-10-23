using BenchmarkDotNet.Attributes;
using Lyo.Benchmark;
using Lyo.Benchmark.Data;

namespace Lyo.Json.Benchmarks;

/// <summary>Times JSON serialization and parsing of a record with nested reference members.</summary>
[BenchmarkDescription("Serializes and parses NestedRecord rows. JSON keeps Address and Geo as nested objects.")]
[BenchmarkParameter("RowCount", Unit = "rows", Description = "Number of NestedRecord rows round-tripped (1,000 or 10,000).")]
[BenchmarkDataShape(typeof(NestedRecord), Notes = "Address nests a Geo coordinate (depth 2). JSON preserves the object graph.")]
[BenchmarkSla(MaxMeanMs = 200, Standard = "Round-tripping up to 10k nested records through JSON should complete within a couple hundred milliseconds.")]
public class JsonNestedBenchmarks
{
    private readonly JsonService _json = JsonService.Default;
    private byte[] _bytes = null!;
    private List<NestedRecord> _rows = null!;

    [Params(1_000, 10_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _rows = NestedRecord.Generate(RowCount);
        _bytes = _json.ExportToJsonBytes(_rows);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkDescription("Serialize the nested records to JSON bytes.")]
    public byte[] ExportNestedBytes() => _json.ExportToJsonBytes(_rows);

    [Benchmark]
    [BenchmarkDescription("Parse the JSON bytes back into typed NestedRecords.")]
    public int ParseNestedBytes() => _json.ParseBytes<List<NestedRecord>>(_bytes).Count;
}