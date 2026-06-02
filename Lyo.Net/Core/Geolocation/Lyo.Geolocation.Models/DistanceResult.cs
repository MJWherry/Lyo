using System.Diagnostics;
using Lyo.Geolocation.Models.Coordinates;
using Lyo.Geolocation.Models.Enums;

namespace Lyo.Geolocation.Models;

[DebuggerDisplay("{ToString(),nq}")]
public class DistanceResult
{
    public GeoCoordinate From { get; set; } = null!;

    public GeoCoordinate To { get; set; } = null!;

    public double DistanceMeters { get; set; }

    public double DistanceKilometers => DistanceMeters / 1000;

    public double DistanceMiles => DistanceMeters * 0.000621371;

    public DistanceCalculationMethod Method { get; set; }

    public override string ToString()
        => $"DistanceResult: {From} -> {To}, {DistanceMeters:0.#}m ({Method})";
}
