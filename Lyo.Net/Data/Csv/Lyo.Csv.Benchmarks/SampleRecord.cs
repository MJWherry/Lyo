namespace Lyo.Csv.Benchmarks;

/// <summary>Flat POCO used as the row type for CSV read/write benchmarks.</summary>
public sealed class SampleRecord
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public int Age { get; set; }

    public decimal Balance { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

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