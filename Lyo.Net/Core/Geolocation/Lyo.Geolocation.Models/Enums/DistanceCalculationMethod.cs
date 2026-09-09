namespace Lyo.Geolocation.Models.Enums;

public enum DistanceCalculationMethod
{
    Haversine, // Great-circle distance (simple, fast)
    Vincenty, // Ellipsoidal distance (more accurate)
    Driving, // Actual road distance
    Walking,
    Bicycling
}