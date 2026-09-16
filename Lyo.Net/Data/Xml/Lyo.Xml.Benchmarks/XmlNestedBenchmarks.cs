using BenchmarkDotNet.Attributes;
using Lyo.Benchmark;
using Lyo.Benchmark.Data;
using Lyo.Xml;

namespace Lyo.Xml.Benchmarks;

/// <summary>Times XML serialization and parsing of a record with nested reference members.</summary>
[BenchmarkDescription("Serializes and parses NestedRecord rows. XML keeps Address and Geo as nested elements.")]
[BenchmarkParameter("RowCount", Unit = "rows", Description = "Number of NestedRecord rows round-tripped (1,000 or 10,000).")]
[BenchmarkDataShape(typeof(NestedRecord), Notes = "Address nests a Geo coordinate (depth 2). XML preserves the object graph.")]
[BenchmarkSla(MaxMeanMs = 2000, Standard = "Round-tripping up to 10k nested records through XML should stay within a couple of seconds.")]
public class XmlNestedBenchmarks
{
    private readonly XmlService _xml = XmlService.Default;
    private byte[] _bytes = null!;
    private List<NestedRecord> _rows = null!;

    [Params(1_000, 10_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _rows = NestedRecord.Generate(RowCount);
        _bytes = _xml.ExportToXmlBytes(_rows);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkDescription("Serialize the nested records to XML bytes.")]
    public byte[] ExportNestedBytes() => _xml.ExportToXmlBytes(_rows);

    [Benchmark]
    [BenchmarkDescription("Parse the XML bytes back into typed NestedRecords.")]
    public int ParseNestedBytes() => _xml.ParseBytes<List<NestedRecord>>(_bytes).Count;
}
