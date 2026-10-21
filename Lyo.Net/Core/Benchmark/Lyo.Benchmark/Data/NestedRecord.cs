namespace Lyo.Benchmark.Data;

/// <summary>Row with a nested address, used by every document suite so nesting cost is comparable.</summary>
/// <remarks>CSV flattens <see cref="Address" /> into columns. JSON, YAML, XML, and TOML keep the object graph. XLSX writes the nested value as text.</remarks>
public sealed class NestedRecord
{
    /// <summary>Sequential row id.</summary>
    public int Id { get; set; }

    /// <summary>Display name shaped as <c>Person {Id}</c>.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Nested address. Depth 2 once <see cref="NestedAddress.Geo" /> is included.</summary>
    public NestedAddress Address { get; set; } = new();

    /// <summary>Builds <paramref name="count" /> deterministic rows.</summary>
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

/// <summary>Address nested on <see cref="NestedRecord" />.</summary>
public sealed class NestedAddress
{
    /// <summary>City, cycling every 50 rows.</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>Fixed country so the column is stable across runs.</summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>Postal code derived from the row id.</summary>
    public int PostalCode { get; set; }

    /// <summary>Coordinate nested one level deeper.</summary>
    public NestedGeo Geo { get; set; } = new();
}

/// <summary>Geo point nested inside <see cref="NestedAddress" />.</summary>
public sealed class NestedGeo
{
    /// <summary>Latitude, varying slightly with the row id.</summary>
    public double Latitude { get; set; }

    /// <summary>Longitude, varying slightly with the row id.</summary>
    public double Longitude { get; set; }
}