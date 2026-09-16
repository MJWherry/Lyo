using BenchmarkDotNet.Attributes;
using Lyo.Benchmark;
using Lyo.Benchmark.Data;
using Lyo.Toml;

namespace Lyo.Toml.Benchmarks;

/// <summary>Times TOML conversion to and from JSON.</summary>
[BenchmarkDescription("Converts a TOML document of RowCount SampleRecords to JSON and back. Captures the read-then-rewrite cost between the two document forms.")]
[BenchmarkParameter("RowCount", Unit = "rows", Description = "Number of SampleRecord rows in the source document (100 to 100,000).")]
[BenchmarkDataShape(typeof(SampleRecord), Notes = "Flat 7-column record. ConvertToJson starts from TOML text. ConvertFromJson starts from the JSON that conversion produced.")]
[BenchmarkSla(MaxMeanMs = 2000, Standard = "Converting up to 100k TOML rows to or from JSON should stay within a couple of seconds.")]
public class TomlConvertBenchmarks
{
    private readonly TomlService _toml = TomlService.Default;
    private string _json = null!;
    private string _tomlText = null!;

    [Params(100, 1_000, 10_000, 100_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _tomlText = _toml.ExportToTomlString(SampleRecord.Generate(RowCount));
        _json = _toml.ConvertToJson(_tomlText);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkDescription("Convert the TOML document to a JSON string (baseline).")]
    public int ConvertToJson() => _toml.ConvertToJson(_tomlText).Length;

    [Benchmark]
    [BenchmarkDescription("Convert the JSON document back to a TOML string.")]
    public int ConvertFromJson() => _toml.ConvertFromJson(_json).Length;
}
