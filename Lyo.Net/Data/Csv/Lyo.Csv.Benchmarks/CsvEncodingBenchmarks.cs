using System.Text;
using BenchmarkDotNet.Attributes;
using Lyo.Benchmarking;

namespace Lyo.Csv.Benchmarks;

/// <summary>Comparison suite contrasting UTF-8 / UTF-16 / UTF-32 CSV round-trips (export + parse).</summary>
[ComparisonSuite]
[BenchmarkDescription(
    "Round-trips SampleRecords through CSV in UTF-8, UTF-16, and UTF-32 to show the encoding's effect on (de)serialization cost. Export = serialize the typed rows to bytes in the encoding; Parse = read those bytes back into typed rows. UTF-8 is the baseline.")]
[BenchmarkParameter("RowCount", Unit = "rows", Description = "Number of SampleRecord rows round-tripped (10,000 or 100,000).")]
[BenchmarkDataShape(typeof(SampleRecord), Notes = "Flat 7-column record; only the byte encoding differs between algorithms.")]
[BenchmarkSla(
    MaxMeanMs = 500,
    Standard =
        "Encoding choice should not blow the CSV (de)serialization budget; round-trips of up to 100k rows should stay within a few hundred milliseconds regardless of encoding.")]
public class CsvEncodingBenchmarks
{
    private readonly CsvService _utf16 = new();
    private readonly CsvService _utf32 = new();
    private readonly CsvService _utf8 = new();
    private List<SampleRecord> _rows = null!;
    private byte[] _utf16Bytes = null!;
    private byte[] _utf32Bytes = null!;
    private byte[] _utf8Bytes = null!;

    [Params(10_000, 100_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _utf8.SetEncoding(Encoding.UTF8);
        _utf16.SetEncoding(Encoding.Unicode);
        _utf32.SetEncoding(Encoding.UTF32);
        _rows = SampleRecord.Generate(RowCount);
        _utf8Bytes = _utf8.ExportToCsvBytes(_rows);
        _utf16Bytes = _utf16.ExportToCsvBytes(_rows);
        _utf32Bytes = _utf32.ExportToCsvBytes(_rows);
    }

    [Benchmark(Baseline = true)]
    [ComparisonAxis("Export")]
    [BenchmarkDescription("Serialize the rows to UTF-8 CSV bytes (baseline encoding).")]
    public byte[] Utf8_Export() => _utf8.ExportToCsvBytes(_rows);

    [Benchmark]
    [ComparisonAxis("Export")]
    [BenchmarkDescription("Serialize the rows to UTF-16 (little-endian) CSV bytes.")]
    public byte[] Utf16_Export() => _utf16.ExportToCsvBytes(_rows);

    [Benchmark]
    [ComparisonAxis("Export")]
    [BenchmarkDescription("Serialize the rows to UTF-32 CSV bytes (widest encoding).")]
    public byte[] Utf32_Export() => _utf32.ExportToCsvBytes(_rows);

    [Benchmark]
    [ComparisonAxis("Parse")]
    [BenchmarkDescription("Parse UTF-8 CSV bytes back into typed rows.")]
    public int Utf8_Parse() => _utf8.ParseBytes<SampleRecord>(_utf8Bytes).ToList().Count;

    [Benchmark]
    [ComparisonAxis("Parse")]
    [BenchmarkDescription("Parse UTF-16 CSV bytes back into typed rows.")]
    public int Utf16_Parse() => _utf16.ParseBytes<SampleRecord>(_utf16Bytes).ToList().Count;

    [Benchmark]
    [ComparisonAxis("Parse")]
    [BenchmarkDescription("Parse UTF-32 CSV bytes back into typed rows.")]
    public int Utf32_Parse() => _utf32.ParseBytes<SampleRecord>(_utf32Bytes).ToList().Count;
}