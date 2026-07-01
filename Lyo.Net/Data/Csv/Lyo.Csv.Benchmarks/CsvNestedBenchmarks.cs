using BenchmarkDotNet.Attributes;
using Lyo.Benchmarking;

namespace Lyo.Csv.Benchmarks;

/// <summary>Benchmarks CSV serialization/parsing of a record with nested reference members (the writer flattens nested objects into columns).</summary>
[BenchmarkDescription(
    "Serializes and parses a NestedRecord whose Address reference member itself nests a Geo coordinate. The CSV writer flattens these nested objects into leaf columns, so this captures the round-trip cost and the data structure that a flat row count alone hides.")]
[BenchmarkParameter("RowCount", Unit = "rows", Description = "Number of NestedRecord rows round-tripped (1,000 or 10,000).")]
[BenchmarkDataShape(
    typeof(NestedRecord), Notes = "Record with a nested Address object that itself nests a Geo coordinate (nesting depth 2); the CSV writer flattens these into leaf columns.")]
[BenchmarkSla(MaxMeanMs = 200, Standard = "Round-tripping up to 10k nested records through CSV should complete within a couple hundred milliseconds.")]
public class CsvNestedBenchmarks
{
    private readonly CsvService _csv = new();
    private byte[] _bytes = null!;
    private List<NestedRecord> _rows = null!;

    [Params(1_000, 10_000)]
    public int RowCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _rows = NestedRecord.Generate(RowCount);
        _bytes = _csv.ExportToCsvBytes(_rows);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkDescription("Serialize the nested records to CSV bytes (writer flattens Address and Geo into columns).")]
    public byte[] ExportNestedBytes() => _csv.ExportToCsvBytes(_rows);

    [Benchmark]
    [BenchmarkDescription("Parse the flattened CSV bytes back into typed NestedRecords (rebuilds the nested objects).")]
    public int ParseNestedBytes() => _csv.ParseBytes<NestedRecord>(_bytes).ToList().Count;
}

/// <summary>Record with nested reference members used to exercise CSV's flattening of nested objects.</summary>
public sealed class NestedRecord
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public NestedAddress Address { get; set; } = new();

    public static List<NestedRecord> Generate(int count)
    {
        var rows = new List<NestedRecord>(count);
        for (var i = 0; i < count; i++) {
            rows.Add(
                new() {
                    Id = i,
                    Name = $"Person {i}",
                    Address = new() {
                        City = $"City {i % 50}",
                        Country = "Lyoland",
                        PostalCode = 10000 + i % 9000,
                        Geo = new() { Latitude = 51.5 + i % 10 * 0.01, Longitude = -0.12 - i % 10 * 0.01 }
                    }
                });
        }

        return rows;
    }
}

/// <summary>Nested address on <see cref="NestedRecord" /> (flattened into CSV columns).</summary>
public sealed class NestedAddress
{
    public string City { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public int PostalCode { get; set; }

    public NestedGeo Geo { get; set; } = new();
}

/// <summary>Geo coordinate nested inside <see cref="NestedAddress" /> (depth-2 nesting).</summary>
public sealed class NestedGeo
{
    public double Latitude { get; set; }

    public double Longitude { get; set; }
}