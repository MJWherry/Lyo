using System.Text;
using BenchmarkDotNet.Attributes;
using Lyo.Benchmark;
using Lyo.Benchmark.Data;
using Lyo.Common.Core.Identifiers;
using Lyo.Toml;

namespace Lyo.Toml.Benchmarks;

/// <summary>Times TOML parsing from bytes, string, span, stream, file, and the async byte path.</summary>
[BenchmarkDescription("Parses a TOML array of tables of RowCount SampleRecords from bytes, a string, a UTF-8 span, a stream, a file, and the async byte path.")]
[BenchmarkParameter("RowCount", Unit = "rows", Description = "Number of SampleRecord rows in the document (100 to 100,000).")]
[BenchmarkDataShape(typeof(SampleRecord), Notes = "Flat 7-column record serialized as [[items]] tables.")]
[BenchmarkSla(MaxMeanMs = 2000, Standard = "Parsing up to 100k TOML records should stay within a couple of seconds on the byte path.")]
public class TomlReadBenchmarks
{
    private readonly TomlService _toml = TomlService.Default;
    private byte[] _bytes = null!;
    private string _filePath = null!;
    private string _text = null!;

    [Params(100, 1_000, 10_000, 100_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var rows = SampleRecord.Generate(RowCount);
        _bytes = _toml.ExportToTomlBytes(rows);
        _text = Encoding.UTF8.GetString(_bytes);
        _filePath = Path.Combine(Path.GetTempPath(), $"lyo-toml-read-{LyoGuid.CreateV7():N}.toml");
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
    public int ParseBytes() => _toml.ParseBytes<List<SampleRecord>>(_bytes).Count;

    [Benchmark]
    [BenchmarkDescription("Parse all rows into typed SampleRecords from a TOML string.")]
    public int ParseString() => _toml.ParseString<List<SampleRecord>>(_text).Count;

    [Benchmark]
    [BenchmarkDescription("Parse all rows into typed SampleRecords from a UTF-8 span.")]
    public int ParseSpan() => _toml.Parse<List<SampleRecord>>(_bytes.AsSpan()).Count;

    [Benchmark]
    [BenchmarkDescription("Parse all rows into typed SampleRecords from a MemoryStream.")]
    public int ParseStream()
    {
        using var stream = new MemoryStream(_bytes, false);
        return _toml.ParseStream<List<SampleRecord>>(stream).Count;
    }

    [Benchmark]
    [BenchmarkDescription("Parse rows from disk into typed SampleRecords.")]
    public int ParseFile() => _toml.ParseFile<List<SampleRecord>>(_filePath).Count;

    [Benchmark]
    [BenchmarkDescription("Parse all rows into a typed list via the async byte path.")]
    public async Task<int> ParseBytesAsync() => (await _toml.ParseBytesAsync<List<SampleRecord>>(_bytes)).Count;
}
