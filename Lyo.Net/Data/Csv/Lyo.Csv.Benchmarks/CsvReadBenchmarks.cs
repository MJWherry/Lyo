using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Lyo.Benchmarking;

namespace Lyo.Csv.Benchmarks;

/// <summary>Benchmarks CSV parsing across sources (bytes/stream/file), buffered vs streaming vs async, and dynamic targets (DataTable, dictionary).</summary>
[BenchmarkDescription("Parses CSV (produced from RowCount SampleRecords) back through every read surface: buffered typed objects from bytes / stream / file, the IAsyncEnumerable streaming path, the async list path, the options-driven parse, and the dynamic DataTable and row/column-dictionary targets. Contrasts the peak-memory of materializing all rows against bounded streaming, and typed mapping against dynamic parsing.")]
[BenchmarkParameter("RowCount", Unit = "rows", Description = "Number of SampleRecord rows encoded in the CSV being parsed (100 to 100,000).")]
[BenchmarkDataShape(typeof(SampleRecord), Notes = "Flat 7-column record; the parser maps columns by header to typed properties.")]
[BenchmarkSla(MaxMeanMs = 500, Standard = "Parsing up to 100k rows of CSV should complete within a few hundred milliseconds (target >= 100k rows/sec on the typed buffered path).")]
public class CsvReadBenchmarks
{
    private readonly CsvService _csv = new();
    private byte[] _bytes = null!;
    private string _filePath = null!;

    [Params(100, 1_000, 10_000, 100_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _bytes = _csv.ExportToCsvBytes(SampleRecord.Generate(RowCount));
        _filePath = Path.Combine(Path.GetTempPath(), $"lyo-csv-read-{Guid.NewGuid():N}.csv");
        File.WriteAllBytes(_filePath, _bytes);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        if (File.Exists(_filePath))
            File.Delete(_filePath);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkDescription("Parse all rows into typed SampleRecords from an in-memory byte buffer (baseline).")]
    public int ParseBytes() => _csv.ParseBytes<SampleRecord>(_bytes).ToList().Count;

    [Benchmark]
    [BenchmarkDescription("Parse all rows into typed SampleRecords from a MemoryStream.")]
    public int ParseStream()
    {
        using var stream = new MemoryStream(_bytes, writable: false);
        return _csv.ParseStream<SampleRecord>(stream).ToList().Count;
    }

    [Benchmark]
    [BenchmarkDescription("Parse rows from disk into typed SampleRecords (filesystem read + mapping).")]
    public int ParseFile() => _csv.ParseFile<SampleRecord>(_filePath).ToList().Count;

    [Benchmark]
    [BenchmarkDescription("Parse rows lazily via the async streaming API (bounded memory, no buffering of all rows).")]
    public async Task<int> ParseStreamStreaming()
    {
        using var stream = new MemoryStream(_bytes, writable: false);
        var count = 0;
        await foreach (var _ in _csv.ParseStreamStreamingAsync<SampleRecord>(stream))
            count++;
        return count;
    }

    [Benchmark]
    [BenchmarkDescription("Parse all rows into a typed List via the async byte path.")]
    public async Task<int> ParseBytesAsync() => (await _csv.ParseBytesAsync<SampleRecord>(_bytes)).Count;

    [Benchmark]
    [BenchmarkDescription("Parse a stream into a typed List with fine-grained parse options (per-row error handling path).")]
    public async Task<int> ParseStreamWithOptions()
    {
        using var stream = new MemoryStream(_bytes, writable: false);
        return (await _csv.ParseStreamWithOptionsAsync<SampleRecord>(stream, null)).Count;
    }

    [Benchmark]
    [BenchmarkDescription("Parse the CSV into a dynamic DataTable (no typed mapping) and count rows.")]
    public int ParseBytesAsDataTable() => _csv.ParseBytesAsDataTable(_bytes).ValueOrThrow().Rows.Count;

    [Benchmark]
    [BenchmarkDescription("Parse the CSV into a nested row/column dictionary (no typed model) and count rows.")]
    public int ParseBytesAsDictionary() => _csv.ParseBytesAsDictionary(_bytes).Count;
}
