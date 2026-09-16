using BenchmarkDotNet.Attributes;
using Lyo.Benchmark;
using Lyo.Benchmark.Data;
using Lyo.Xml;

namespace Lyo.Xml.Benchmarks;

/// <summary>Times XML conversion to and from JSON.</summary>
[BenchmarkDescription("Converts an XML document of RowCount SampleRecords to JSON and back. Captures the read-then-rewrite cost between the two document forms.")]
[BenchmarkParameter("RowCount", Unit = "rows", Description = "Number of SampleRecord rows in the source document (100 to 100,000).")]
[BenchmarkDataShape(typeof(SampleRecord), Notes = "Flat 7-column record. ConvertToJson starts from XML text. ConvertFromJson starts from the JSON that conversion produced.")]
[BenchmarkSla(MaxMeanMs = 2000, Standard = "Converting up to 100k XML rows to or from JSON should stay within a couple of seconds.")]
public class XmlConvertBenchmarks
{
    private readonly XmlService _xml = XmlService.Default;
    private string _json = null!;
    private string _xmlText = null!;

    [Params(100, 1_000, 10_000, 100_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _xmlText = _xml.ExportToXmlString(SampleRecord.Generate(RowCount));
        _json = _xml.ConvertToJson(_xmlText);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkDescription("Convert the XML document to a JSON string (baseline).")]
    public int ConvertToJson() => _xml.ConvertToJson(_xmlText).Length;

    [Benchmark]
    [BenchmarkDescription("Convert the JSON document back to an XML string.")]
    public int ConvertFromJson() => _xml.ConvertFromJson(_json).Length;
}
