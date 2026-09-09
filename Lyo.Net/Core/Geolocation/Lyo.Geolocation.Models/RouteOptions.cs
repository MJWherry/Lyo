using System.Diagnostics;
using Lyo.Geolocation.Models.Coordinates;
using Lyo.Geolocation.Models.Enums;

namespace Lyo.Geolocation.Models;

[DebuggerDisplay("{ToString(),nq}")]
public class RouteOptions
{
    public TransportMode Mode { get; set; }

    public bool AvoidTolls { get; set; }

    public bool AvoidHighways { get; set; }

    public bool AvoidFerries { get; set; }

    public DateTime? DepartureTime { get; set; }

    public DateTime? ArrivalTime { get; set; }

    public IEnumerable<GeoCoordinate>? Waypoints { get; set; }

    public bool OptimizeWaypoints { get; set; }

    public string? Language { get; set; }

    public DistanceUnit Unit { get; set; }

    public override string ToString() => $"RouteOptions: mode={Mode}, unit={Unit}, avoidTolls={AvoidTolls}, avoidHighways={AvoidHighways}";
}