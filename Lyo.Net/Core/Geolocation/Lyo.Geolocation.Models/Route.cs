using System.Diagnostics;
using Lyo.Geolocation.Models.Coordinates;
using Lyo.Geolocation.Models.Enums;

namespace Lyo.Geolocation.Models;

[DebuggerDisplay("{ToString(),nq}")]
public class Route
{
    public Guid Id { get; set; }

    public GeoCoordinate StartPoint { get; set; } = null!;

    public GeoCoordinate EndPoint { get; set; } = null!;

    public IEnumerable<GeoCoordinate>? Waypoints { get; set; }

    public IEnumerable<RouteStep>? Steps { get; set; }

    public double TotalDistanceMeters { get; set; }

    public TimeSpan EstimatedDuration { get; set; }

    public TimeSpan? DurationInTraffic { get; set; }

    public TransportMode TransportMode { get; set; }

    public BoundingBox Bounds { get; set; } = null!;

    public string Summary { get; set; } = string.Empty;

    public IEnumerable<string>? Warnings { get; set; }

    public IDictionary<string, string>? Metadata { get; set; }

    public override string ToString() => $"Route: {StartPoint} -> {EndPoint}, {TotalDistanceMeters:0.#}m, {EstimatedDuration}, mode={TransportMode}";
}