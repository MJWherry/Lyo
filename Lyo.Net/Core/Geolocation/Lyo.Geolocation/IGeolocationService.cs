using Lyo.Geolocation.Models;
using Lyo.Geolocation.Models.Addresses;
using Lyo.Geolocation.Models.Coordinates;
using Lyo.Geolocation.Models.Enums;

namespace Lyo.Geolocation;

/// <summary>Service for geolocation operations including geocoding, reverse geocoding, distance calculations, and time zone lookup.</summary>
public interface IGeolocationService
{
    /// <summary>Converts an address string to geographic coordinates.</summary>
    /// <param name="address">The address string to geocode.</param>
    /// <returns>The geographic coordinates for the address.</returns>
    Task<GeoCoordinate> GeocodeAsync(string address);

    /// <summary>Converts an address object to geographic coordinates.</summary>
    /// <param name="address">The address to geocode.</param>
    /// <returns>The geographic coordinates for the address.</returns>
    Task<GeoCoordinate> GeocodeAsync(Address address);

    /// <summary>Converts multiple address strings to geographic coordinates in batch.</summary>
    /// <param name="addresses">The address strings to geocode.</param>
    /// <returns>An enumeration of geographic coordinates in the same order as the input addresses.</returns>
    Task<IEnumerable<GeoCoordinate>> GeocodeBatchAsync(IEnumerable<string> addresses);

    /// <summary>Converts geographic coordinates to an address (reverse geocoding).</summary>
    /// <param name="latitude">The latitude.</param>
    /// <param name="longitude">The longitude.</param>
    /// <returns>The address at the specified coordinates.</returns>
    Task<Address> ReverseGeocodeAsync(double latitude, double longitude);

    /// <summary>Converts geographic coordinates to an address (reverse geocoding).</summary>
    /// <param name="coordinate">The geographic coordinate.</param>
    /// <returns>The address at the specified coordinates.</returns>
    Task<Address> ReverseGeocodeAsync(GeoCoordinate coordinate);

    /// <summary>Calculates the distance between two geographic coordinates.</summary>
    /// <param name="from">The starting coordinate.</param>
    /// <param name="to">The ending coordinate.</param>
    /// <param name="unit">The distance unit. Defaults to kilometers.</param>
    /// <returns>The distance in the specified unit.</returns>
    Task<double> GetDistanceAsync(GeoCoordinate from, GeoCoordinate to, DistanceUnit unit = DistanceUnit.Kilometers);

    /// <summary>Calculates the distance between two addresses.</summary>
    /// <param name="fromAddress">The starting address.</param>
    /// <param name="toAddress">The ending address.</param>
    /// <param name="unit">The distance unit. Defaults to kilometers.</param>
    /// <returns>The distance in the specified unit.</returns>
    Task<double> GetDistanceAsync(string fromAddress, string toAddress, DistanceUnit unit = DistanceUnit.Kilometers);

    /// <summary>Checks whether two points are within the specified radius of each other.</summary>
    /// <param name="point1">The first coordinate.</param>
    /// <param name="point2">The second coordinate.</param>
    /// <param name="radiusKm">The radius in kilometers.</param>
    /// <returns>True if the points are within the radius.</returns>
    Task<bool> IsWithinRadiusAsync(GeoCoordinate point1, GeoCoordinate point2, double radiusKm);

    /// <summary>Gets the time zone for a geographic coordinate.</summary>
    /// <param name="coordinate">The geographic coordinate.</param>
    /// <returns>The IANA time zone identifier (e.g. "America/New_York").</returns>
    Task<string> GetTimeZoneAsync(GeoCoordinate coordinate);

    /// <summary>Gets the time zone for an address.</summary>
    /// <param name="address">The address string.</param>
    /// <returns>The IANA time zone identifier (e.g. "America/New_York").</returns>
    Task<string> GetTimeZoneAsync(string address);

    /// <summary>Gets route information between two coordinates.</summary>
    /// <param name="start">The starting coordinate.</param>
    /// <param name="end">The ending coordinate.</param>
    /// <param name="options">Optional route options. May be null for defaults.</param>
    /// <returns>The route details including distance and duration.</returns>
    Task<Route> GetRouteAsync(GeoCoordinate start, GeoCoordinate end, RouteOptions? options = null);

    /// <summary>Gets the driving distance between two coordinates.</summary>
    /// <param name="from">The starting coordinate.</param>
    /// <param name="to">The ending coordinate.</param>
    /// <returns>The driving distance in kilometers.</returns>
    Task<double> GetDrivingDistanceAsync(GeoCoordinate from, GeoCoordinate to);

    /// <summary>Gets the estimated travel time between two coordinates.</summary>
    /// <param name="from">The starting coordinate.</param>
    /// <param name="to">The ending coordinate.</param>
    /// <param name="mode">The transport mode (driving, walking, etc.).</param>
    /// <returns>The estimated travel time.</returns>
    Task<TimeSpan> GetEstimatedTravelTimeAsync(GeoCoordinate from, GeoCoordinate to, TransportMode mode);
}