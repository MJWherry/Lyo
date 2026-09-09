using System.Diagnostics;
using Lyo.Common.Core;
using Lyo.Exceptions;
using Lyo.Geolocation.Models.Coordinates;

namespace Lyo.Geolocation.Models;

/// <summary>Geographic bounding box defined by southwest and northeast corners.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public class BoundingBox : IEquatable<BoundingBox>
{
    /// <summary>Southwest corner.</summary>
    public GeoCoordinate Southwest { get; set; } = null!;

    /// <summary>Northeast corner.</summary>
    public GeoCoordinate Northeast { get; set; } = null!;

    /// <summary>Center point.</summary>
    public GeoCoordinate Center => new((Southwest.Latitude + Northeast.Latitude) / 2, (Southwest.Longitude + Northeast.Longitude) / 2);

    /// <summary>Northwest corner.</summary>
    public GeoCoordinate Northwest => new(Northeast.Latitude, Southwest.Longitude);

    /// <summary>Southeast corner.</summary>
    public GeoCoordinate Southeast => new(Southwest.Latitude, Northeast.Longitude);

    public bool Equals(BoundingBox? other)
    {
        if (other == null)
            return false;

        return Southwest.Equals(other.Southwest) && Northeast.Equals(other.Northeast);
    }

    /// <summary>True when the coordinate sits inside this box.</summary>
    public bool Contains(GeoCoordinate? point)
    {
        if (point == null)
            return false;

        return point.Latitude >= Southwest.Latitude && point.Latitude <= Northeast.Latitude && point.Longitude >= Southwest.Longitude && point.Longitude <= Northeast.Longitude;
    }

    /// <summary>True when this box overlaps another.</summary>
    public bool Intersects(BoundingBox? other)
    {
        if (other == null)
            return false;

        return !(Northeast.Latitude < other.Southwest.Latitude || Southwest.Latitude > other.Northeast.Latitude || Northeast.Longitude < other.Southwest.Longitude ||
            Southwest.Longitude > other.Northeast.Longitude);
    }

    /// <summary>Expands the box by the given distance in meters.</summary>
    public BoundingBox Expand(double meters)
    {
        var center = Center;
        var latOffset = meters / 111320.0; // ~meters per degree of latitude
        var lonOffset = meters / (111320.0 * Math.Cos(center.Latitude * Math.PI / 180));
        return new() {
            Southwest = new(Math.Max(-90, Southwest.Latitude - latOffset), Math.Max(-180, Southwest.Longitude - lonOffset)),
            Northeast = new(Math.Min(90, Northeast.Latitude + latOffset), Math.Min(180, Northeast.Longitude + lonOffset))
        };
    }

    /// <summary>Width of the box in meters.</summary>
    public double GetWidth()
    {
        var sw = new GeoCoordinate(Southwest.Latitude, Southwest.Longitude);
        var se = new GeoCoordinate(Southwest.Latitude, Northeast.Longitude);
        return sw.DistanceTo(se);
    }

    /// <summary>Height of the box in meters.</summary>
    public double GetHeight()
    {
        var sw = new GeoCoordinate(Southwest.Latitude, Southwest.Longitude);
        var nw = new GeoCoordinate(Northeast.Latitude, Southwest.Longitude);
        return sw.DistanceTo(nw);
    }

    /// <summary>Area of the box in square meters.</summary>
    public double GetArea() => GetWidth() * GetHeight();

    /// <summary>Mints a box from a center point and a radius in meters.</summary>
    public static BoundingBox FromCenterAndRadius(GeoCoordinate center, double radiusMeters)
    {
        ArgumentHelpers.ThrowIfNull(center);
        const double metersPerDegreeLat = 111320.0;
        var metersPerDegreeLon = 111320.0 * Math.Cos(center.Latitude * Math.PI / 180);
        var latOffset = radiusMeters / metersPerDegreeLat;
        var lonOffset = radiusMeters / metersPerDegreeLon;
        return new() {
            Southwest = new(Math.Max(-90, center.Latitude - latOffset), Math.Max(-180, center.Longitude - lonOffset)),
            Northeast = new(Math.Min(90, center.Latitude + latOffset), Math.Min(180, center.Longitude + lonOffset))
        };
    }

    public override bool Equals(object? obj) => obj is BoundingBox other && Equals(other);

    public override int GetHashCode() => HashCodeHelpers.Combine(Southwest, Northeast);

    public override string ToString() => $"BoundingBox: SW={Southwest}, NE={Northeast}";

    public static bool operator ==(BoundingBox? left, BoundingBox? right) => Equals(left, right);

    public static bool operator !=(BoundingBox? left, BoundingBox? right) => !Equals(left, right);
}