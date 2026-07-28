using BenchmarkDotNet.Attributes;
using Lyo.Benchmarking;

namespace Lyo.Csv.Benchmarks;

/// <summary>Benchmarks CSV split and merge operations (bytes, streams, and files).</summary>
[BenchmarkDescription(
    "Splits and merges CSV payloads built from RowCount SampleRecords. Covers SplitCsvBytesAsync, SplitCsvFileAsync, CombineCsvBytesAsync, CombineCsvFilesAsync, and a split-then-combine round trip. RowsPerFile controls chunk size for row-based splits.")]
[BenchmarkParameter("RowCount", Unit = "rows", Description = "Number of SampleRecord rows in the source CSV (1,000 to 100,000).")]
[BenchmarkParameter("RowsPerFile", Unit = "rows", Description = "Maximum data rows per output part when splitting (500 or 5,000).")]
[BenchmarkDataShape(typeof(SampleRecord), Notes = "Flat 7-column record; split repeats the header row in each part.")]
[BenchmarkSla(MaxMeanMs = 2000, Standard = "Splitting or merging up to 100k CSV rows should complete within a couple of seconds.")]
public class CsvSplitMergeBenchmarks
{
    private readonly CsvService _csv = new();
    private byte[] _bytes = null!;
    private string _combineOutputPath = null!;
    private string _filePath = null!;
    private string _outputDirectory = null!;
    private IReadOnlyList<string> _splitFilePaths = null!;
    private IReadOnlyList<byte[]> _splitParts = null!;

    [Params(1_000, 10_000, 100_000)]
    public int RowCount { get; set; }

    [Params(500, 5_000)]
    public int RowsPerFile { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _bytes = _csv.ExportToCsvBytes(SampleRecord.Generate(RowCount));
        _filePath = Path.Combine(Path.GetTempPath(), $"lyo-csv-splitmerge-{Guid.NewGuid():N}.csv");
        File.WriteAllBytes(_filePath, _bytes);
        _outputDirectory = Path.Combine(Path.GetTempPath(), $"lyo-csv-split-out-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_outputDirectory);
        _combineOutputPath = Path.Combine(Path.GetTempPath(), $"lyo-csv-combined-{Guid.NewGuid():N}.csv");
        _splitParts = _csv.SplitCsvBytesAsync(_bytes, RowsPerFile).GetAwaiter().GetResult();
        _splitFilePaths = _csv.SplitCsvFileAsync(_filePath, RowsPerFile, _outputDirectory).GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(_filePath))
            File.Delete(_filePath);

        if (File.Exists(_combineOutputPath))
            File.Delete(_combineOutputPath);

        if (Directory.Exists(_outputDirectory))
            Directory.Delete(_outputDirectory, true);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkDescription("Split CSV bytes into multiple parts (in-memory).")]
    public int SplitCsvBytes() => _csv.SplitCsvBytesAsync(_bytes, RowsPerFile).GetAwaiter().GetResult().Count;

    [Benchmark]
    [BenchmarkDescription("Split a CSV file on disk into multiple part files.")]
    public int SplitCsvFile() => _csv.SplitCsvFileAsync(_filePath, RowsPerFile, _outputDirectory).GetAwaiter().GetResult().Count;

    [Benchmark]
    [BenchmarkDescription("Combine split CSV byte parts back into one payload.")]
    public long CombineCsvBytes()
    {
        var combined = _csv.CombineCsvBytesAsync(_splitParts).GetAwaiter().GetResult();
        return combined.LongLength;
    }

    [Benchmark]
    [BenchmarkDescription("Combine split CSV part files on disk into one output file.")]
    public async Task CombineCsvFiles() => await _csv.CombineCsvFilesAsync(_splitFilePaths, _combineOutputPath).ConfigureAwait(false);

    [Benchmark]
    [BenchmarkDescription("Split CSV bytes then combine parts and count parsed rows (round trip).")]
    public int SplitThenCombineBytes()
    {
        var parts = _csv.SplitCsvBytesAsync(_bytes, RowsPerFile).GetAwaiter().GetResult();
        var combined = _csv.CombineCsvBytesAsync(parts).GetAwaiter().GetResult();
        return _csv.ParseBytes<SampleRecord>(combined).Count();
    }
}