using System.Diagnostics;
using Lyo.Common.Core;
using Lyo.Exceptions;
using Lyo.Geolocation.Models.Enums;

namespace Lyo.Geolocation.Models.Coordinates;

/// <summary>Geographic coordinate (latitude, longitude) with optional altitude and accuracy.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class GeoCoordinate : IEquatable<GeoCoordinate>
{
    /// <summary>Latitude in decimal degrees (-90 to 90).</summary>
    public double Latitude {
        get;
        set {
            ArgumentHelpers.ThrowIfNotInRange(value, -90.0, 90.0, nameof(Latitude), "Latitude must be between -90 and 90 degrees");
            field = value;
        }
    }

    /// <summary>Longitude in decimal degrees (-180 to 180).</summary>
    public double Longitude {
        get;
        set {
            ArgumentHelpers.ThrowIfNotInRange(value, -180.0, 180.0, nameof(Longitude), "Longitude must be between -180 and 180 degrees");
            field = value;
        }
    }

    /// <summary>Altitude in meters above sea level.</summary>
    public double? Altitude { get; set; }

    /// <summary>Accuracy in meters.</summary>
    public double? Accuracy { get; set; }

    /// <summary>When the coordinate was recorded.</summary>
    public DateTime? Timestamp { get; set; }

    /// <summary>Parameterless constructor.</summary>
    public GeoCoordinate() { }

    /// <summary>Mints a coordinate from latitude and longitude.</summary>
    public GeoCoordinate(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    /// <summary>Mints a coordinate from latitude, longitude, and altitude.</summary>
    public GeoCoordinate(double latitude, double longitude, double altitude)
        : this(latitude, longitude)
        => Altitude = altitude;

    public bool Equals(GeoCoordinate? other)
    {
        if (other == null)
            return false;

        return Math.Abs(Latitude - other.Latitude) < 0.000001 && Math.Abs(Longitude - other.Longitude) < 0.000001;
    }

    /// <summary>True when latitude and longitude sit inside the valid ranges.</summary>
    public bool IsValid() => Latitude is >= -90 and <= 90 && Longitude is >= -180 and <= 180;

    /// <summary>Distance to another coordinate via the Haversine formula.</summary>
    /// <param name="other">Other coordinate</param>
    /// <param name="unit">Unit of the returned distance</param>
    /// <returns>Distance in <paramref name="unit" /></returns>
    public double DistanceTo(GeoCoordinate other, DistanceUnit unit = DistanceUnit.Meters)
    {
        ArgumentHelpers.ThrowIfNull(other);
        const double earthRadiusMeters = 6371000; // Mean Earth radius in meters
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

    /// <summary>True when this coordinate lies within <paramref name="radiusMeters" /> of <paramref name="center" />.</summary>
    public bool IsWithinRadius(GeoCoordinate center, double radiusMeters)
    {
        ArgumentHelpers.ThrowIfNull(center);
        return DistanceTo(center) <= radiusMeters;
    }

    /// <summary>Mints a coordinate offset by the given distances.</summary>
    /// <param name="metersNorth">Meters north (negative is south)</param>
    /// <param name="metersEast">Meters east (negative is west)</param>
    public GeoCoordinate Offset(double metersNorth, double metersEast)
    {
        const double earthRadiusMeters = 6371000;
        var latOffset = metersNorth / earthRadiusMeters * (180 / Math.PI);
        var lonOffset = metersEast / (earthRadiusMeters * Math.Cos(Latitude * Math.PI / 180)) * (180 / Math.PI);
        return new(Latitude + latOffset, Longitude + lonOffset, Altitude ?? 0);
    }

    /// <summary>Formats the coordinate as Degrees Minutes Seconds.</summary>
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

    /// <summary>Parses a coordinate from Degrees Minutes Seconds (for example 40°26'46"N 79°58'56"W).</summary>
    public static GeoCoordinate FromDms(string dms)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(dms);
        var parts = dms.Split([' '], StringSplitOptions.RemoveEmptyEntries);
        FormatHelpers.ThrowIf(parts.Length < 2, "DMS string must contain latitude and longitude segments.");
        return new(ParseDmsSegment(parts[0], true), ParseDmsSegment(parts[1], false));
    }

    private static double ParseDmsSegment(string segment, bool isLatitude)
    {
        var dir = segment[^1];
        var numeric = segment[..^1].Trim();
        var pieces = numeric.Split(['°', '\'', '"'], StringSplitOptions.RemoveEmptyEntries);
        if (pieces.Length < 2)
            throw new FormatException($"Invalid DMS segment: {segment}");

        var degrees = double.Parse(pieces[0]);
        var minutes = double.Parse(pieces[1]);
        var seconds = pieces.Length > 2 ? double.Parse(pieces[2]) : 0;
        var decimalDegrees = degrees + minutes / 60.0 + seconds / 3600.0;
        var negative = dir is 'S' or 's' or 'W' or 'w';
        if (isLatitude && dir is not ('N' or 'n' or 'S' or 's'))
            throw new FormatException($"Invalid latitude direction in: {segment}");

        if (!isLatitude && dir is not ('E' or 'e' or 'W' or 'w'))
            throw new FormatException($"Invalid longitude direction in: {segment}");

        return negative ? -decimalDegrees : decimalDegrees;
    }

    public override bool Equals(object? obj) => obj is GeoCoordinate other && Equals(other);

    public override int GetHashCode() => HashCodeHelpers.Combine(Latitude, Longitude);

    public override string ToString() => $"{Latitude}, {Longitude}";

    public static bool operator ==(GeoCoordinate? left, GeoCoordinate? right) => Equals(left, right);

    public static bool operator !=(GeoCoordinate? left, GeoCoordinate? right) => !Equals(left, right);
}