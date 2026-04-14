using Lyo.Geolocation.Models.Enums;

namespace Lyo.Geolocation.Models.Coordinates;

/// <summary>Represents a geographic coordinate (latitude, longitude) with optional altitude and accuracy</summary>
public class GeoCoordinate : IEquatable<GeoCoordinate>
{
    private double _latitude;
    private double _longitude;

    /// <summary>Latitude in decimal degrees (-90 to 90)</summary>
    public double Latitude {
        get => _latitude;
        set {
            if (value < -90 || value > 90)
                throw new ArgumentOutOfRangeException(nameof(Latitude), "Latitude must be between -90 and 90 degrees");

            _latitude = value;
        }
    }

    /// <summary>Longitude in decimal degrees (-180 to 180)</summary>
    public double Longitude {
        get => _longitude;
        set {
            if (value < -180 || value > 180)
                throw new ArgumentOutOfRangeException(nameof(Longitude), "Longitude must be between -180 and 180 degrees");

            _longitude = value;
        }
    }

    /// <summary>Altitude in meters above sea level</summary>
    public double? Altitude { get; set; }

    /// <summary>Accuracy in meters</summary>
    public double? Accuracy { get; set; }

    /// <summary>Timestamp when the coordinate was recorded</summary>
    public DateTime? Timestamp { get; set; }

    /// <summary>Default constructor</summary>
    public GeoCoordinate() { }

    /// <summary>Constructor with latitude and longitude</summary>
    public GeoCoordinate(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    /// <summary>Constructor with latitude, longitude, and altitude</summary>
    public GeoCoordinate(double latitude, double longitude, double altitude)
        : this(latitude, longitude)
        => Altitude = altitude;

    public bool Equals(GeoCoordinate? other)
    {
        if (other == null)
            return false;

        return Math.Abs(Latitude - other.Latitude) < 0.000001 && Math.Abs(Longitude - other.Longitude) < 0.000001;
    }

    /// <summary>Whether the coordinate is valid (within valid ranges)</summary>
    public bool IsValid() => Latitude >= -90 && Latitude <= 90 && Longitude >= -180 && Longitude <= 180;

    /// <summary>Calculates the distance to another coordinate using the Haversine formula</summary>
    /// <param name="other">The other coordinate</param>
    /// <param name="unit">The unit for the distance result</param>
    /// <returns>Distance in the specified unit</returns>
    public double DistanceTo(GeoCoordinate other, DistanceUnit unit = DistanceUnit.Meters)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        const double earthRadiusMeters = 6371000; // Earth's radius in meters
        var lat1Rad = Latitude * Math.PI / 180;
        var lat2Rad = other.Latitude * Math.PI / 180;
        var deltaLatRad = (other.Latitude - Latitude) * Math.PI / 180;
        var deltaLonRad = (other.Longitude - Longitude) * Math.PI / 180;
        var a = Math.Sin(deltaLatRad / 2) * Math.Sin(deltaLatRad / 2) + Math.Cos(lat1Rad) * Math.Cos(lat2Rad) * Math.Sin(deltaLonRad / 2) * Math.Sin(deltaLonRad / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        var distanceMeters = earthRadiusMeters * c;
        switch (unit) {
            case DistanceUnit.Kilometers:
                return distanceMeters / 1000;
            case DistanceUnit.Miles:
                return distanceMeters * 0.000621371;
            case DistanceUnit.Feet:
                return distanceMeters * 3.28084;
            case DistanceUnit.NauticalMiles:
                return distanceMeters * 0.000539957;
            case DistanceUnit.Meters:
            default:
                return distanceMeters;
        }
    }

    /// <summary>Checks if this coordinate is within a specified radius of another coordinate</summary>
    public bool IsWithinRadius(GeoCoordinate center, double radiusMeters)
    {
        if (center == null)
            throw new ArgumentNullException(nameof(center));

        return DistanceTo(center) <= radiusMeters;
    }

    /// <summary>Creates a new coordinate offset by the specified distance</summary>
    /// <param name="metersNorth">Meters to offset north (negative for south)</param>
    /// <param name="metersEast">Meters to offset east (negative for west)</param>
    public GeoCoordinate Offset(double metersNorth, double metersEast)
    {
        const double earthRadiusMeters = 6371000;
        var latOffset = metersNorth / earthRadiusMeters * (180 / Math.PI);
        var lonOffset = metersEast / (earthRadiusMeters * Math.Cos(Latitude * Math.PI / 180)) * (180 / Math.PI);
        return new(Latitude + latOffset, Longitude + lonOffset, Altitude ?? 0);
    }

    /// <summary>Converts to Degrees Minutes Seconds format</summary>
    public string ToDms()
    {
        var latDegrees = (int)Math.Abs(Latitude);
        var latMinutes = (int)((Math.Abs(Latitude) - latDegrees) * 60);
        var latSeconds = ((Math.Abs(Latitude) - latDegrees) * 60 - latMinutes) * 60;
        var latDir = Latitude >= 0 ? "N" : "S";
        var lonDegrees = (int)Math.Abs(Longitude);
        var lonMinutes = (int)((Math.Abs(Longitude) - lonDegrees) * 60);
        var lonSeconds = ((Math.Abs(Longitude) - lonDegrees) * 60 - lonMinutes) * 60;
        var lonDir = Longitude >= 0 ? "E" : "W";
        return $"{latDegrees}°{latMinutes}'{latSeconds:F2}\"{latDir} {lonDegrees}°{lonMinutes}'{lonSeconds:F2}\"{lonDir}";
    }

    /// <summary>Parses a coordinate from Degrees Minutes Seconds format</summary>
    public static GeoCoordinate FromDms(string dms)
        => throw
            // Simplified parser - would need more robust implementation
            new NotImplementedException("DMS parsing not yet implemented");

    public override bool Equals(object? obj) => obj is GeoCoordinate other && Equals(other);

    public override int GetHashCode()
    {
        unchecked {
            var hashCode = Latitude.GetHashCode();
            hashCode = (hashCode * 397) ^ Longitude.GetHashCode();
            return hashCode;
        }
    }

    public override string ToString() => $"{Latitude}, {Longitude}";

    public static bool operator ==(GeoCoordinate? left, GeoCoordinate? right) => Equals(left, right);

    public static bool operator !=(GeoCoordinate? left, GeoCoordinate? right) => !Equals(left, right);
}