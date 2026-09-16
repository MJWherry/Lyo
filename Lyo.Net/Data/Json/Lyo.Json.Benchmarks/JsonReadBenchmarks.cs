using System.Text;
using BenchmarkDotNet.Attributes;
using Lyo.Benchmark;
using Lyo.Benchmark.Data;
using Lyo.Common.Core.Identifiers;
using Lyo.Json;

namespace Lyo.Json.Benchmarks;

/// <summary>Times JSON parsing from bytes, string, span, stream, file, and the async byte path, plus NDJSON.</summary>
[BenchmarkDescription(
    "Parses a JSON array of RowCount SampleRecords from bytes, a string, a UTF-8 span, a stream, a file, and the async byte path. NDJSON is one object per line.")]
[BenchmarkParameter("RowCount", Unit = "rows", Description = "Number of SampleRecord rows in the document (100 to 100,000).")]
[BenchmarkDataShape(typeof(SampleRecord), Notes = "Flat 7-column record serialized as a JSON array, or one object per line for NDJSON.")]
[BenchmarkSla(MaxMeanMs = 500, Standard = "Parsing up to 100k JSON records should complete within a few hundred milliseconds on the byte path.")]
public class JsonReadBenchmarks
{
    private readonly JsonService _json = JsonService.Default;
    private byte[] _bytes = null!;
    private byte[] _ndjson = null!;
    private string _filePath = null!;
    private string _text = null!;

    [Params(100, 1_000, 10_000, 100_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var rows = SampleRecord.Generate(RowCount);
        _bytes = _json.ExportToJsonBytes(rows);
        _text = Encoding.UTF8.GetString(_bytes);
        _ndjson = _json.ExportNdjsonBytes(rows);
        _filePath = Path.Combine(Path.GetTempPath(), $"lyo-json-read-{LyoGuid.CreateV7():N}.json");
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
    public int ParseBytes() => _json.ParseBytes<List<SampleRecord>>(_bytes).Count;

    [Benchmark]
    [BenchmarkDescription("Parse all rows into typed SampleRecords from a JSON string.")]
    public int ParseString() => _json.ParseString<List<SampleRecord>>(_text).Count;

    [Benchmark]
    [BenchmarkDescription("Parse all rows into typed SampleRecords from a UTF-8 span.")]
    public int ParseSpan() => _json.Parse<List<SampleRecord>>(_bytes.AsSpan()).Count;

    [Benchmark]
    [BenchmarkDescription("Parse all rows into typed SampleRecords from a MemoryStream.")]
    public int ParseStream()
    {
        using var stream = new MemoryStream(_bytes, false);
        return _json.ParseStream<List<SampleRecord>>(stream).Count;
    }

    [Benchmark]
    [BenchmarkDescription("Parse rows from disk into typed SampleRecords.")]
    public int ParseFile() => _json.ParseFile<List<SampleRecord>>(_filePath).Count;

    [Benchmark]
    [BenchmarkDescription("Parse all rows into a typed list via the async byte path.")]
    public async Task<int> ParseBytesAsync() => (await _json.ParseBytesAsync<List<SampleRecord>>(_bytes)).Count;

    [Benchmark]
    [BenchmarkDescription("Parse NDJSON, one SampleRecord per line, from UTF-8 bytes.")]
    public int ParseNdjson() => _json.ParseNdjsonBytes<SampleRecord>(_ndjson).Count;
}
