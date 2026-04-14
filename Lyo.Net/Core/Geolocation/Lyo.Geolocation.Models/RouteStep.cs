using Lyo.Geolocation.Models.Coordinates;
using Lyo.Geolocation.Models.Enums;

namespace Lyo.Geolocation.Models;

public class RouteStep
{
    public int StepNumber { get; set; }

    public GeoCoordinate StartLocation { get; set; }

    public GeoCoordinate EndLocation { get; set; }

    public double DistanceMeters { get; set; }

    public TimeSpan Duration { get; set; }

    public string Instructions { get; set; }

    public string RoadName { get; set; }

    public ManeuverType Maneuver { get; set; }

    public IEnumerable<GeoCoordinate> PathPoints { get; set; } // Detailed path
}