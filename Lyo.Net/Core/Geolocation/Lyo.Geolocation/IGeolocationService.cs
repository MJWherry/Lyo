using Lyo.Geolocation.Models;
using Lyo.Geolocation.Models.Addresses;
using Lyo.Geolocation.Models.Coordinates;
using Lyo.Geolocation.Models.Enums;

namespace Lyo.Geolocation;

/// <summary>Service for geolocation operations including geocoding, reverse geocoding, distance calculations, and time zone lookup.</summary>
public interface IGeolocationService
{
    /// <summary>Converts an address string to a geocode result.</summary>
    Task<GeocodeResult> GeocodeAsync(string address, CancellationToken ct = default);

    /// <summary>Converts an address object to a geocode result.</summary>
    Task<GeocodeResult> GeocodeAsync(Address address, CancellationToken ct = default);

    /// <summary>Converts multiple address strings to geocode results in batch (preserves order and per-item errors).</summary>
    Task<BatchGeocodeResult> GeocodeBatchAsync(IEnumerable<string> addresses, CancellationToken ct = default);

    /// <summary>Converts geographic coordinates to an address (reverse geocoding).</summary>
    Task<ReverseGeocodeResult> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken ct = default);

    /// <summary>Converts geographic coordinates to an address (reverse geocoding).</summary>
    Task<ReverseGeocodeResult> ReverseGeocodeAsync(GeoCoordinate coordinate, CancellationToken ct = default);

    /// <summary>Calculates the distance between two geographic coordinates.</summary>
    Task<double> GetDistanceAsync(GeoCoordinate from, GeoCoordinate to, DistanceUnit unit = DistanceUnit.Kilometers, CancellationToken ct = default);

    /// <summary>Calculates the distance between two addresses.</summary>
    Task<double> GetDistanceAsync(string fromAddress, string toAddress, DistanceUnit unit = DistanceUnit.Kilometers, CancellationToken ct = default);

    /// <summary>Checks whether two points are within the specified radius of each other.</summary>
    Task<bool> IsWithinRadiusAsync(GeoCoordinate point1, GeoCoordinate point2, double radiusKm, CancellationToken ct = default);

    /// <summary>Gets the time zone for a geographic coordinate.</summary>
    Task<string> GetTimeZoneAsync(GeoCoordinate coordinate, CancellationToken ct = default);

    /// <summary>Gets the time zone for an address.</summary>
    Task<string> GetTimeZoneAsync(string address, CancellationToken ct = default);

    /// <summary>Gets route information between two coordinates.</summary>
    Task<Route> GetRouteAsync(GeoCoordinate start, GeoCoordinate end, RouteOptions? options = null, CancellationToken ct = default);

    /// <summary>Gets the driving distance between two coordinates.</summary>
    Task<double> GetDrivingDistanceAsync(GeoCoordinate from, GeoCoordinate to, CancellationToken ct = default);

    /// <summary>Gets the estimated travel time between two coordinates.</summary>
    Task<TimeSpan> GetEstimatedTravelTimeAsync(GeoCoordinate from, GeoCoordinate to, TransportMode mode, CancellationToken ct = default);
}
