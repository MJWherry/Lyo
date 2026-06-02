using System.Diagnostics;
using Lyo.Geolocation.Models.Coordinates;
using Lyo.Geolocation.Models.Enums;

namespace Lyo.Geolocation.Models;

[DebuggerDisplay("{ToString(),nq}")]
public class RouteStep
{
    public int StepNumber { get; set; }

    public GeoCoordinate StartLocation { get; set; } = null!;

    public GeoCoordinate EndLocation { get; set; } = null!;

    public double DistanceMeters { get; set; }

    public TimeSpan Duration { get; set; }

    public string Instructions { get; set; } = string.Empty;

    public string? RoadName { get; set; }

    public ManeuverType Maneuver { get; set; }

    public IEnumerable<GeoCoordinate>? PathPoints { get; set; }

    public override string ToString()
        => $"RouteStep #{StepNumber}: {StartLocation} -> {EndLocation}, {DistanceMeters:0.#}m, {Maneuver}";
}
