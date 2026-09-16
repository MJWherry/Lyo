using BenchmarkDotNet.Attributes;
using Lyo.Benchmark;
using Lyo.Benchmark.Data;
using Lyo.Toml;

namespace Lyo.Toml.Benchmarks;

/// <summary>Times TOML serialization and parsing of a record with nested reference members.</summary>
[BenchmarkDescription("Serializes and parses NestedRecord rows. TOML keeps Address and Geo as nested tables.")]
[BenchmarkParameter("RowCount", Unit = "rows", Description = "Number of NestedRecord rows round-tripped (1,000 or 10,000).")]
[BenchmarkDataShape(typeof(NestedRecord), Notes = "Address nests a Geo coordinate (depth 2). TOML preserves the object graph.")]
[BenchmarkSla(MaxMeanMs = 2000, Standard = "Round-tripping up to 10k nested records through TOML should stay within a couple of seconds.")]
public class TomlNestedBenchmarks
{
    private readonly TomlService _toml = TomlService.Default;
    private byte[] _bytes = null!;
    private List<NestedRecord> _rows = null!;

    [Params(1_000, 10_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _rows = NestedRecord.Generate(RowCount);
        _bytes = _toml.ExportToTomlBytes(_rows);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkDescription("Serialize the nested records to TOML bytes.")]
    public byte[] ExportNestedBytes() => _toml.ExportToTomlBytes(_rows);

    [Benchmark]
    [BenchmarkDescription("Parse the TOML bytes back into typed NestedRecords.")]
    public int ParseNestedBytes() => _toml.ParseBytes<List<NestedRecord>>(_bytes).Count;
}
