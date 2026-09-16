using BenchmarkDotNet.Attributes;
using Lyo.Benchmark;
using Lyo.Benchmark.Data;
using Lyo.Yaml;

namespace Lyo.Yaml.Benchmarks;

/// <summary>Times YAML conversion to and from JSON.</summary>
[BenchmarkDescription("Converts a YAML document of RowCount SampleRecords to JSON and back. Captures the read-then-rewrite cost between the two document forms.")]
[BenchmarkParameter("RowCount", Unit = "rows", Description = "Number of SampleRecord rows in the source document (100 to 100,000).")]
[BenchmarkDataShape(typeof(SampleRecord), Notes = "Flat 7-column record. ConvertToJson starts from YAML text. ConvertFromJson starts from the JSON that conversion produced.")]
[BenchmarkSla(MaxMeanMs = 2000, Standard = "Converting up to 100k YAML rows to or from JSON should stay within a couple of seconds.")]
public class YamlConvertBenchmarks
{
    private readonly YamlService _yaml = YamlService.Default;
    private string _json = null!;
    private string _yamlText = null!;

    [Params(100, 1_000, 10_000, 100_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _yamlText = _yaml.ExportToYamlString(SampleRecord.Generate(RowCount));
        _json = _yaml.ConvertToJson(_yamlText);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkDescription("Convert the YAML document to a JSON string (baseline).")]
    public int ConvertToJson() => _yaml.ConvertToJson(_yamlText).Length;

    [Benchmark]
    [BenchmarkDescription("Convert the JSON document back to a YAML string.")]
    public int ConvertFromJson() => _yaml.ConvertFromJson(_json).Length;
}
