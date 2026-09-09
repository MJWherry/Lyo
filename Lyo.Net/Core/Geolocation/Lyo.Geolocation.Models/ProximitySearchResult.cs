using System.Diagnostics;

namespace Lyo.Geolocation.Models;

[DebuggerDisplay("{ToString(),nq}")]
public class ProximitySearchResult<T>
    where T : class
{
    public T Item { get; set; } = null!;

    public double DistanceMeters { get; set; }

    public double DistanceKilometers => DistanceMeters / 1000;

    public double DistanceMiles => DistanceMeters * 0.000621371;

    public override string ToString() => $"ProximitySearchResult: {DistanceMeters:0.#}m, item={Item}";
}