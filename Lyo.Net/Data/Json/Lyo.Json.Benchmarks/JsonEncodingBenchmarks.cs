using System.Text;
using BenchmarkDotNet.Attributes;
using Lyo.Benchmark;
using Lyo.Benchmark.Data;

namespace Lyo.Json.Benchmarks;

/// <summary>Comparison suite contrasting UTF-8 / UTF-16 / UTF-32 JSON round-trips.</summary>
[ComparisonSuite]
[BenchmarkDescription(
    "Round-trips SampleRecords through JSON in UTF-8, UTF-16, and UTF-32. Export writes typed rows to bytes. Parse reads those bytes back into typed rows. UTF-8 is the baseline.")]
[BenchmarkParameter("RowCount", Unit = "rows", Description = "Number of SampleRecord rows round-tripped (10,000 or 100,000).")]
[BenchmarkDataShape(typeof(SampleRecord), Notes = "Flat 7-column record; only the byte encoding differs between algorithms.")]
[BenchmarkSla(MaxMeanMs = 500, Standard = "Round-trips of up to 100k JSON rows should stay within a few hundred milliseconds regardless of encoding.")]
public class JsonEncodingBenchmarks
{
    private readonly JsonService _utf16 = new(o => o.Encoding = Encoding.Unicode);
    private readonly JsonService _utf32 = new(o => o.Encoding = Encoding.UTF32);
    private readonly JsonService _utf8 = JsonService.Default;
    private List<SampleRecord> _rows = null!;
    private byte[] _utf16Bytes = null!;
    private byte[] _utf32Bytes = null!;
    private byte[] _utf8Bytes = null!;

    [Params(10_000, 100_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _rows = SampleRecord.Generate(RowCount);
        _utf8Bytes = _utf8.ExportToJsonBytes(_rows);
        _utf16Bytes = _utf16.ExportToJsonBytes(_rows);
        _utf32Bytes = _utf32.ExportToJsonBytes(_rows);
    }

    [Benchmark(Baseline = true)]
    [ComparisonAxis("Export")]
    [BenchmarkDescription("Serialize the rows to UTF-8 JSON bytes (baseline encoding).")]
    public byte[] Utf8_Export() => _utf8.ExportToJsonBytes(_rows);

    [Benchmark]
    [ComparisonAxis("Export")]
    [BenchmarkDescription("Serialize the rows to UTF-16 JSON bytes.")]
    public byte[] Utf16_Export() => _utf16.ExportToJsonBytes(_rows);

    [Benchmark]
    [ComparisonAxis("Export")]
    [BenchmarkDescription("Serialize the rows to UTF-32 JSON bytes.")]
    public byte[] Utf32_Export() => _utf32.ExportToJsonBytes(_rows);

    [Benchmark]
    [ComparisonAxis("Parse")]
    [BenchmarkDescription("Parse UTF-8 JSON bytes back into typed rows.")]
    public int Utf8_Parse() => _utf8.ParseBytes<List<SampleRecord>>(_utf8Bytes).Count;

    [Benchmark]
    [ComparisonAxis("Parse")]
    [BenchmarkDescription("Parse UTF-16 JSON bytes back into typed rows.")]
    public int Utf16_Parse() => _utf16.ParseBytes<List<SampleRecord>>(_utf16Bytes).Count;

    [Benchmark]
    [ComparisonAxis("Parse")]
    [BenchmarkDescription("Parse UTF-32 JSON bytes back into typed rows.")]
    public int Utf32_Parse() => _utf32.ParseBytes<List<SampleRecord>>(_utf32Bytes).Count;
}