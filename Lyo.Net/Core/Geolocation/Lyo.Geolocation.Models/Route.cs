using Lyo.Geolocation.Models.Coordinates;
using Lyo.Geolocation.Models.Enums;

namespace Lyo.Geolocation.Models;

public class Route
{
    public Guid Id { get; set; }

    public GeoCoordinate StartPoint { get; set; }

    public GeoCoordinate EndPoint { get; set; }

    public IEnumerable<GeoCoordinate> Waypoints { get; set; }

    public IEnumerable<RouteStep> Steps { get; set; }

    public double TotalDistanceMeters { get; set; }

    public TimeSpan EstimatedDuration { get; set; }

    public TimeSpan? DurationInTraffic { get; set; }

    public TransportMode TransportMode { get; set; }

    public BoundingBox Bounds { get; set; }

    public string Summary { get; set; }

    public IEnumerable<string> Warnings { get; set; }

    public IDictionary<string, string> Metadata { get; set; }
}