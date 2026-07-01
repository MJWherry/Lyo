using System.Text;
using BenchmarkDotNet.Attributes;
using Lyo.Benchmarking;

namespace Lyo.Xlsx.Benchmarks;

/// <summary>Benchmarks XLSX conversion paths: XLSX -> CSV (bytes/stream, default and UTF-16 encoding, sync + async) and XLSX -> HTML table.</summary>
[BenchmarkDescription(
    "Converts an XLSX workbook (built from RowCount SampleRecords) into other formats: XLSX -> CSV bytes (default and UTF-16 encoding), XLSX -> CSV via a stream, the async CSV conversion, and XLSX -> HTML table. Captures the read-then-rewrite cost and the effect of output encoding on conversion.")]
[BenchmarkParameter("RowCount", Unit = "rows", Description = "Number of SampleRecord rows in the source workbook being converted (100 to 100,000).")]
[BenchmarkDataShape(typeof(SampleRecord), Notes = "Flat 7-column record; the source workbook's first sheet is converted.")]
[BenchmarkSla(MaxMeanMs = 10000, Standard = "Converting an up-to-100k-row workbook (read XLSX + rewrite CSV/HTML) should complete within ~10s.")]
public class XlsxConvertBenchmarks
{
    private byte[] _bytes = null!;
    private XlsxService _xlsx = null!;

    [Params(100, 1_000, 10_000, 100_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _xlsx = new();
        _bytes = _xlsx.ExportToXlsxBytes(SampleRecord.Generate(RowCount));
    }

    [Benchmark(Baseline = true)]
    [BenchmarkDescription("Convert the workbook bytes to CSV bytes using the default encoding (baseline).")]
    public byte[] ConvertToCsvBytes() => _xlsx.ConvertXlsxToCsvBytes(_bytes);

    [Benchmark]
    [BenchmarkDescription("Convert the workbook bytes to CSV bytes using UTF-16 output encoding.")]
    public byte[] ConvertToCsvBytesUtf16() => _xlsx.ConvertXlsxToCsvBytes(_bytes, Encoding.Unicode);

    [Benchmark]
    [BenchmarkDescription("Convert the workbook to CSV written into a MemoryStream.")]
    public long ConvertToCsvStream()
    {
        using var output = new MemoryStream();
        _xlsx.ConvertXlsxToCsv(_bytes, output);
        return output.Length;
    }

    [Benchmark]
    [BenchmarkDescription("Convert the workbook bytes to CSV bytes via the async path.")]
    public async Task<byte[]> ConvertToCsvBytesAsync() => await _xlsx.ConvertXlsxToCsvBytesAsync(_bytes);

    [Benchmark]
    [BenchmarkDescription("Render the workbook's first sheet as an HTML table document.")]
    public int ExportToHtmlTable() => _xlsx.ExportToHtmlTable(_bytes).Length;
}