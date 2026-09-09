namespace Lyo.Benchmark.Data;

/// <summary>Seven-column flat POCO (int/string/decimal/bool/DateTime) used as the row type by tabular suites such as CSV and XLSX.</summary>
/// <remarks>
/// The layout stays flat on purpose: nested objects or collections would add per-row flattening cost that a row count alone does not capture. Mark a suite with
/// <c>[BenchmarkDataShape(typeof(SampleRecord))]</c> so the exported report records the column set.
/// </remarks>
public sealed class SampleRecord
{
    /// <summary>Sequential row id, offset by the <c>startId</c> argument to <see cref="Generate" />.</summary>
    public int Id { get; set; }

    /// <summary>Display name shaped as <c>Person {Id}</c>.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Email shaped as <c>person{Id}@example.com</c>.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Age in the range 18–77, cycling with <see cref="Id" />.</summary>
    public int Age { get; set; }

    /// <summary>Decimal column used to exercise fixed-point format and parse.</summary>
    public decimal Balance { get; set; }

    /// <summary>Boolean column that flips with <see cref="Id" />.</summary>
    public bool IsActive { get; set; }

    /// <summary>UTC timestamp column, one minute per row from 2020-01-01.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Builds <paramref name="count" /> deterministic rows from <paramref name="startId" /> so runs stay comparable.</summary>
    public static List<SampleRecord> Generate(int count, int startId = 0)
    {
        var rows = new List<SampleRecord>(count);
        var baseDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < count; i++) {
            var id = startId + i;
            rows.Add(
                new() {
                    Id = id,
                    Name = $"Person {id}",
                    Email = $"person{id}@example.com",
                    Age = 18 + id % 60,
                    Balance = 100.50m + id,
                    IsActive = id % 2 == 0,
                    CreatedAt = baseDate.AddMinutes(id)
                });
        }

        return rows;
    }
}
