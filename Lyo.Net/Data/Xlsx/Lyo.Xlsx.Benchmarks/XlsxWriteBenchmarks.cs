using System.Reflection;
using System.Text;
using BenchmarkDotNet.Attributes;
using Lyo.Benchmarking;

namespace Lyo.Xlsx.Benchmarks;

/// <summary>
/// Benchmarks XLSX serialization across destinations (bytes/stream/file), sync vs async, selected columns, multi-sheet workbooks, and dynamic sources (DataTable,
/// dictionary).
/// </summary>
[BenchmarkDescription(
    "Serializes RowCount SampleRecords to XLSX via the streaming OpenXML writer across every write surface: typed-list to bytes / stream / file, the async byte path, a selected-columns subset (3 of 7), a 3-worksheet workbook, and the dynamic DataTable and row/column-dictionary sources. Shows the relative cost of file I/O, multi-sheet workbooks, and dynamic vs typed column construction.")]
[BenchmarkParameter("RowCount", Unit = "rows", Description = "Number of SampleRecord rows written to the worksheet (100 to 100,000).")]
[BenchmarkDataShape(typeof(SampleRecord), Notes = "Flat 7-column record; each property becomes one worksheet column.")]
[BenchmarkSla(MaxMeanMs = 10000, Standard = "XLSX is markedly heavier than CSV; a bulk export of up to 100k rows via the streaming OpenXML writer should complete within ~10s.")]
public class XlsxWriteBenchmarks
{
    private DataTable.Models.DataTable _dataTable = null!;
    private IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> _dictionary = null!;
    private string _filePath = null!;
    private List<SampleRecord> _rows = null!;
    private IReadOnlyList<PropertyInfo> _selected = null!;
    private IReadOnlyDictionary<string, IEnumerable<SampleRecord>> _sheets = null!;
    private XlsxService _xlsx = null!;

    [Params(100, 1_000, 10_000, 100_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _xlsx = new();
        _rows = SampleRecord.Generate(RowCount);
        var bytes = _xlsx.ExportToXlsxBytes(_rows);
        _dataTable = _xlsx.ParseXlsxBytesAsDataTable(bytes).ValueOrThrow();
        _dictionary = _xlsx.ParseXlsxBytesAsDictionary(bytes);
        _selected = [
            typeof(SampleRecord).GetProperty(nameof(SampleRecord.Id))!, typeof(SampleRecord).GetProperty(nameof(SampleRecord.Name))!,
            typeof(SampleRecord).GetProperty(nameof(SampleRecord.Email))!
        ];

        var third = Math.Max(1, RowCount / 3);
        _sheets = new Dictionary<string, IEnumerable<SampleRecord>> {
            ["Sheet1"] = _rows.Take(third).ToList(), ["Sheet2"] = _rows.Skip(third).Take(third).ToList(), ["Sheet3"] = _rows.Skip(third * 2).ToList()
        };

        _filePath = Path.Combine(Path.GetTempPath(), $"lyo-xlsx-write-{Guid.NewGuid():N}.xlsx");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(_filePath))
            File.Delete(_filePath);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkDescription("Serialize the typed record list to XLSX bytes, single worksheet (baseline).")]
    public byte[] ExportToXlsxBytes() => _xlsx.ExportToXlsxBytes(_rows);

    [Benchmark]
    [BenchmarkDescription("Serialize the typed record list into a MemoryStream workbook.")]
    public long ExportToXlsxStream()
    {
        using var stream = new MemoryStream();
        _xlsx.ExportToXlsx(_rows, stream);
        return stream.Length;
    }

    [Benchmark]
    [BenchmarkDescription("Serialize the typed record list to an XLSX file on disk (writer + filesystem I/O).")]
    public void ExportToXlsxFile() => _xlsx.ExportToXlsx(_rows, _filePath);

    [Benchmark]
    [BenchmarkDescription("Serialize the typed record list to XLSX bytes via the async path.")]
    public async Task<byte[]> ExportToXlsxBytesAsync() => await _xlsx.ExportToXlsxBytesAsync(_rows);

    [Benchmark]
    [BenchmarkDescription("Serialize only 3 of the 7 columns (Id, Name, Email) to XLSX bytes (selected-property path).")]
    public byte[] ExportToXlsxBytesSelected() => _xlsx.ExportToXlsxBytes(_rows, _selected);

    [Benchmark]
    [BenchmarkDescription("Serialize the rows split across a 3-worksheet workbook to XLSX bytes (multi-sheet construction).")]
    public byte[] ExportToXlsxBytesMultiSheet() => _xlsx.ExportToXlsxBytes(_sheets);

    [Benchmark]
    [BenchmarkDescription("Serialize a pre-parsed DataTable to XLSX bytes (dynamic column path).")]
    public byte[] ExportToXlsxBytesFromDataTable() => _xlsx.ExportToXlsxBytesFromDataTable(_dataTable);

    [Benchmark]
    [BenchmarkDescription("Serialize a pre-built row/column dictionary map to XLSX bytes (no typed model).")]
    public byte[] ExportToXlsxBytesFromDictionary() => _xlsx.ExportToXlsxBytesFromDictionary(_dictionary);
}