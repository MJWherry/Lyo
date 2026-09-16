using System.Text;
using BenchmarkDotNet.Attributes;
using Lyo.Benchmark;
using Lyo.Benchmark.Data;
using Lyo.Common.Core.Identifiers;

namespace Lyo.Xlsx.Benchmarks;

/// <summary>Times XLSX parsing across sources (bytes/stream/file) and targets (dictionary and DataTable), sync and async.</summary>
[BenchmarkDescription(
    "Parses an XLSX workbook (built from RowCount SampleRecords) via ExcelDataReader into a row/column dictionary and into a DataTable, from bytes, a stream, and a file, plus the async byte paths.")]
[BenchmarkParameter("RowCount", Unit = "rows", Description = "Number of SampleRecord rows in the workbook being parsed (100 to 100,000).")]
[BenchmarkDataShape(typeof(SampleRecord), Notes = "Flat 7-column record; columns mapped by header.")]
[BenchmarkSla(MaxMeanMs = 10000, Standard = "XLSX parsing is heavier than CSV; reading up to 100k rows should complete within ~10s.")]
public class XlsxReadBenchmarks
{
    private byte[] _bytes = null!;
    private string _filePath = null!;
    private XlsxService _xlsx = null!;

    [Params(100, 1_000, 10_000, 100_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _xlsx = new();
        _bytes = _xlsx.ExportToXlsxBytes(SampleRecord.Generate(RowCount));
        _filePath = Path.Combine(Path.GetTempPath(), $"lyo-xlsx-read-{LyoGuid.CreateV7():N}.xlsx");
        File.WriteAllBytes(_filePath, _bytes);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(_filePath))
            File.Delete(_filePath);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkDescription("Parse the workbook bytes into a row/column dictionary (baseline).")]
    public int ParseBytesAsDictionary() => _xlsx.ParseXlsxBytesAsDictionary(_bytes).Count;

    [Benchmark]
    [BenchmarkDescription("Parse the workbook bytes into a typed DataTable and count rows.")]
    public int ParseBytesAsDataTable() => _xlsx.ParseXlsxBytesAsDataTable(_bytes).ValueOrThrow().Rows.Count;

    [Benchmark]
    [BenchmarkDescription("Parse a workbook stream into a row/column dictionary.")]
    public int ParseStreamAsDictionary()
    {
        using var stream = new MemoryStream(_bytes, false);
        return _xlsx.ParseXlsxStreamAsDictionary(stream).Count;
    }

    [Benchmark]
    [BenchmarkDescription("Parse a workbook stream into a DataTable and count rows.")]
    public int ParseStreamAsDataTable()
    {
        using var stream = new MemoryStream(_bytes, false);
        return _xlsx.ParseXlsxStreamAsDataTable(stream).ValueOrThrow().Rows.Count;
    }

    [Benchmark]
    [BenchmarkDescription("Parse a workbook file from disk into a row/column dictionary (filesystem read).")]
    public int ParseFileAsDictionary() => _xlsx.ParseXlsxFileAsDictionary(_filePath).Count;

    [Benchmark]
    [BenchmarkDescription("Parse a workbook file from disk into a DataTable and count rows (filesystem read).")]
    public int ParseFileAsDataTable() => _xlsx.ParseXlsxFileAsDataTable(_filePath).ValueOrThrow().Rows.Count;

    [Benchmark]
    [BenchmarkDescription("Parse the workbook bytes into a DataTable via the async path.")]
    public async Task<int> ParseBytesAsDataTableAsync() => (await _xlsx.ParseXlsxBytesAsDataTableAsync(_bytes)).ValueOrThrow().Rows.Count;

    [Benchmark]
    [BenchmarkDescription("Parse the workbook bytes into a row/column dictionary via the async path.")]
    public async Task<int> ParseBytesAsDictionaryAsync() => (await _xlsx.ParseXlsxBytesAsDictionaryAsync(_bytes)).Count;
}