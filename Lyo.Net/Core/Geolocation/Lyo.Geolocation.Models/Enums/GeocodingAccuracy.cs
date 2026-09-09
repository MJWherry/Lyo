namespace Lyo.Geolocation.Models.Enums;

public enum GeocodingAccuracy
{
    Rooftop, // Exact location
    RangeInterpolated, // Approximate location on a street
    GeometricCenter, // Center of a result like polyline or polygon
    Approximate // Approximate location
}