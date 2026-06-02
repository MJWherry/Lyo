using System.Diagnostics;

namespace Lyo.Geolocation.Models;

[DebuggerDisplay("{ToString(),nq}")]
public class GeocodeOptions
{
    public string? Language { get; set; }

    public string? Region { get; set; }

    public BoundingBox? Bounds { get; set; }

    public int? MaxResults { get; set; }

    public IEnumerable<string>? ComponentRestrictions { get; set; }

    public override string ToString()
        => $"GeocodeOptions: lang={Language}, region={Region}, max={MaxResults?.ToString() ?? "default"}";
}
