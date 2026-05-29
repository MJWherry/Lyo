using Lyo.Exceptions;
using Lyo.Geolocation.Models.Coordinates;
using Lyo.Geolocation.Models.Enums;

namespace Lyo.Geolocation;

/// <summary>Local geolocation calculations that do not require an external provider.</summary>
public static class GeolocationMath
{
    /// <summary>Calculates the distance between two coordinates.</summary>
    public static double GetDistance(GeoCoordinate from, GeoCoordinate to, DistanceUnit unit = DistanceUnit.Kilometers)
    {
        ArgumentHelpers.ThrowIfNull(from);
        ArgumentHelpers.ThrowIfNull(to);
        var distanceMeters = from.DistanceTo(to);
        return unit switch {
            DistanceUnit.Kilometers => distanceMeters / 1000.0,
            DistanceUnit.Miles => distanceMeters * 0.000621371,
            DistanceUnit.Feet => distanceMeters * 3.28084,
            DistanceUnit.NauticalMiles => distanceMeters * 0.000539957,
            var _ => distanceMeters
        };
    }

    /// <summary>Checks whether two points are within the specified radius.</summary>
    public static bool IsWithinRadius(GeoCoordinate point1, GeoCoordinate point2, double radiusKm)
    {
        ArgumentHelpers.ThrowIfNull(point1);
        ArgumentHelpers.ThrowIfNull(point2);
        return point1.DistanceTo(point2, DistanceUnit.Kilometers) <= radiusKm;
    }
}