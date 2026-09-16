using System.Text;
using BenchmarkDotNet.Attributes;
using Lyo.Benchmark;
using Lyo.Benchmark.Data;

namespace Lyo.Xlsx.Benchmarks;

/// <summary>Times XLSX serialization of a record with nested reference members.</summary>
[BenchmarkDescription(
    "Serializes NestedRecord rows to XLSX and reads the workbook back as a row/column dictionary. Nested Address and Geo values are written as text, not as child sheets.")]
[BenchmarkParameter("RowCount", Unit = "rows", Description = "Number of NestedRecord rows round-tripped (1,000 or 10,000).")]
[BenchmarkDataShape(typeof(NestedRecord), Notes = "Address nests a Geo coordinate (depth 2). XLSX stores each nested value as a single text cell.")]
[BenchmarkSla(MaxMeanMs = 10000, Standard = "Round-tripping up to 10k nested records through XLSX should complete within ~10s.")]
public class XlsxNestedBenchmarks
{
    private byte[] _bytes = null!;
    private List<NestedRecord> _rows = null!;
    private XlsxService _xlsx = null!;

    [Params(1_000, 10_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _xlsx = new();
        _rows = NestedRecord.Generate(RowCount);
        _bytes = _xlsx.ExportToXlsxBytes(_rows);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkDescription("Serialize the nested records to XLSX bytes. Nested members become text cells.")]
    public byte[] ExportNestedBytes() => _xlsx.ExportToXlsxBytes(_rows);

    [Benchmark]
    [BenchmarkDescription("Parse the workbook bytes into a row/column dictionary and count rows.")]
    public int ParseNestedBytes() => _xlsx.ParseXlsxBytesAsDictionary(_bytes).Count;
}
