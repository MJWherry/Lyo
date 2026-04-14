using Lyo.Geolocation.Models.Coordinates;
using Lyo.Geolocation.Models.Enums;

namespace Lyo.Geolocation.Models;

public class DistanceResult
{
    public GeoCoordinate From { get; set; }

    public GeoCoordinate To { get; set; }

    public double DistanceMeters { get; set; }

    public double DistanceKilometers => DistanceMeters / 1000;

    public double DistanceMiles => DistanceMeters * 0.000621371;

    public DistanceCalculationMethod Method { get; set; } // Haversine, Vincenty, etc.
}