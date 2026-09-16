using BenchmarkDotNet.Attributes;
using Lyo.Benchmark;
using Lyo.Benchmark.Data;
using Lyo.Yaml;

namespace Lyo.Yaml.Benchmarks;

/// <summary>Times YAML serialization and parsing of a record with nested reference members.</summary>
[BenchmarkDescription("Serializes and parses NestedRecord rows. YAML keeps Address and Geo as nested mappings.")]
[BenchmarkParameter("RowCount", Unit = "rows", Description = "Number of NestedRecord rows round-tripped (1,000 or 10,000).")]
[BenchmarkDataShape(typeof(NestedRecord), Notes = "Address nests a Geo coordinate (depth 2). YAML preserves the object graph.")]
[BenchmarkSla(MaxMeanMs = 2000, Standard = "Round-tripping up to 10k nested records through YAML should stay within a couple of seconds.")]
public class YamlNestedBenchmarks
{
    private readonly YamlService _yaml = YamlService.Default;
    private byte[] _bytes = null!;
    private List<NestedRecord> _rows = null!;

    [Params(1_000, 10_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _rows = NestedRecord.Generate(RowCount);
        _bytes = _yaml.ExportToYamlBytes(_rows);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkDescription("Serialize the nested records to YAML bytes.")]
    public byte[] ExportNestedBytes() => _yaml.ExportToYamlBytes(_rows);

    [Benchmark]
    [BenchmarkDescription("Parse the YAML bytes back into typed NestedRecords.")]
    public int ParseNestedBytes() => _yaml.ParseBytes<List<NestedRecord>>(_bytes).Count;
}
