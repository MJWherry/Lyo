using System.Text;
using BenchmarkDotNet.Attributes;
using Lyo.Benchmark;
using Lyo.Xlsx.Models;

namespace Lyo.Xlsx.Benchmarks;

/// <summary>Benchmarks XLSX split and merge operations (by sheet, by rows, and merge modes).</summary>
[BenchmarkDescription(
    "Splits and merges XLSX workbooks built from RowCount SampleRecords. Single-sheet payloads exercise SplitXlsxBytesByRows and row-based file splits; a three-sheet workbook exercises SplitXlsxBytesBySheet. Merge benchmarks cover PreserveSheets and ConcatenateRows via bytes and files.")]
[BenchmarkParameter("RowCount", Unit = "rows", Description = "Rows per worksheet in the source workbook(s) (1,000 to 100,000).")]
[BenchmarkParameter("RowsPerFile", Unit = "rows", Description = "Maximum data rows per output part when splitting by rows (500 or 5,000).")]
[BenchmarkDataShape(typeof(SampleRecord), Notes = "Flat 7-column record; multi-sheet workbooks use three equally sized sheets.")]
[BenchmarkSla(MaxMeanMs = 30000, Standard = "XLSX split/merge is heavier than CSV; up to 100k rows per sheet should complete within ~30s.")]
public class XlsxSplitMergeBenchmarks
{
    private byte[] _mergeInputA = null!;
    private byte[] _mergeInputB = null!;
    private string _mergeOutputPath = null!;
    private byte[] _multiSheetBytes = null!;
    private string _outputDirectory = null!;
    private byte[] _singleSheetBytes = null!;
    private string _singleSheetFilePath = null!;
    private XlsxService _xlsx = null!;

    [Params(1_000, 10_000, 100_000)]
    public int RowCount { get; set; }

    [Params(500, 5_000)]
    public int RowsPerFile { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _xlsx = new();
        var rows = SampleRecord.Generate(RowCount);
        _singleSheetBytes = _xlsx.ExportToXlsxBytes(rows);
        _mergeInputA = _singleSheetBytes;
        _mergeInputB = _xlsx.ExportToXlsxBytes(SampleRecord.Generate(RowCount, RowCount));
        var perSheetRows = Math.Max(1, RowCount / 3);
        _multiSheetBytes = _xlsx.ExportToXlsxBytes(
            new Dictionary<string, IEnumerable<SampleRecord>> {
                { "SheetA", SampleRecord.Generate(perSheetRows) },
                { "SheetB", SampleRecord.Generate(perSheetRows, perSheetRows) },
                { "SheetC", SampleRecord.Generate(perSheetRows, perSheetRows * 2) }
            });

        _singleSheetFilePath = Path.Combine(Path.GetTempPath(), $"lyo-xlsx-splitmerge-{Guid.NewGuid():N}.xlsx");
        File.WriteAllBytes(_singleSheetFilePath, _singleSheetBytes);
        _outputDirectory = Path.Combine(Path.GetTempPath(), $"lyo-xlsx-split-out-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_outputDirectory);
        _mergeOutputPath = Path.Combine(Path.GetTempPath(), $"lyo-xlsx-merged-{Guid.NewGuid():N}.xlsx");
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(_singleSheetFilePath))
            File.Delete(_singleSheetFilePath);

        if (File.Exists(_mergeOutputPath))
            File.Delete(_mergeOutputPath);

        if (Directory.Exists(_outputDirectory))
            Directory.Delete(_outputDirectory, true);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkDescription("Split a single-sheet workbook into row-based parts (bytes).")]
    public int SplitBytesByRows() => _xlsx.SplitXlsxBytesByRows(_singleSheetBytes, RowsPerFile).Count;

    [Benchmark]
    [BenchmarkDescription("Split a multi-sheet workbook into one workbook per worksheet (bytes).")]
    public int SplitBytesBySheet() => _xlsx.SplitXlsxBytesBySheet(_multiSheetBytes).Count;

    [Benchmark]
    [BenchmarkDescription("Split a single-sheet workbook file into row-based part files.")]
    public int SplitFileByRows() => _xlsx.SplitXlsxByRows(_singleSheetFilePath, RowsPerFile, _outputDirectory).Count;

    [Benchmark]
    [BenchmarkDescription("Merge two single-sheet workbooks preserving all worksheets.")]
    public long MergeBytesPreserveSheets()
    {
        var merged = _xlsx.MergeXlsxBytes([_mergeInputA, _mergeInputB]);
        return merged.LongLength;
    }

    [Benchmark]
    [BenchmarkDescription("Merge two single-sheet workbooks by concatenating all data rows into one sheet.")]
    public long MergeBytesConcatenateRows()
    {
        var merged = _xlsx.MergeXlsxBytes([_mergeInputA, _mergeInputB], XlsxMergeMode.ConcatenateRows);
        return merged.LongLength;
    }

    [Benchmark]
    [BenchmarkDescription("Merge two workbook files on disk (preserve worksheets).")]
    public void MergeFilesPreserveSheets()
    {
        var inputA = Path.Combine(_outputDirectory, "merge-a.xlsx");
        var inputB = Path.Combine(_outputDirectory, "merge-b.xlsx");
        File.WriteAllBytes(inputA, _mergeInputA);
        File.WriteAllBytes(inputB, _mergeInputB);
        _xlsx.MergeXlsxFiles([inputA, inputB], _mergeOutputPath);
    }

    [Benchmark]
    [BenchmarkDescription("Split a workbook by rows then merge parts back together (preserve sheets).")]
    public long SplitThenMergeBytes()
    {
        var parts = _xlsx.SplitXlsxBytesByRows(_singleSheetBytes, RowsPerFile);
        return _xlsx.MergeXlsxBytes(parts).LongLength;
    }
}