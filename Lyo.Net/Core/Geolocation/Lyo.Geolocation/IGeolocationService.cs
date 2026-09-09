using Lyo.Geolocation.Models;
using Lyo.Geolocation.Models.Addresses;
using Lyo.Geolocation.Models.Coordinates;
using Lyo.Geolocation.Models.Enums;

namespace Lyo.Geolocation;

/// <summary>Geocoding, reverse geocoding, distance, routing, and time-zone lookup.</summary>
public interface IGeolocationService
{
    /// <summary>Geocodes a free-form address string.</summary>
    Task<GeocodeResult> GeocodeAsync(string address, CancellationToken ct = default);

    /// <summary>Geocodes a structured address.</summary>
    Task<GeocodeResult> GeocodeAsync(Address address, CancellationToken ct = default);

    /// <summary>Geocodes many address strings in one batch (order and per-item errors are preserved).</summary>
    Task<BatchGeocodeResult> GeocodeBatchAsync(IEnumerable<string> addresses, CancellationToken ct = default);

    /// <summary>Reverse-geocodes latitude and longitude into an address.</summary>
    Task<ReverseGeocodeResult> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken ct = default);

    /// <summary>Reverse-geocodes a coordinate into an address.</summary>
    Task<ReverseGeocodeResult> ReverseGeocodeAsync(GeoCoordinate coordinate, CancellationToken ct = default);

    /// <summary>Distance between two coordinates.</summary>
    Task<double> GetDistanceAsync(GeoCoordinate from, GeoCoordinate to, DistanceUnit unit = DistanceUnit.Kilometers, CancellationToken ct = default);

    /// <summary>Distance between two address strings.</summary>
    Task<double> GetDistanceAsync(string fromAddress, string toAddress, DistanceUnit unit = DistanceUnit.Kilometers, CancellationToken ct = default);

    /// <summary>True when the two points lie within the given radius of each other.</summary>
    Task<bool> IsWithinRadiusAsync(GeoCoordinate point1, GeoCoordinate point2, double radiusKm, CancellationToken ct = default);

    /// <summary>Time zone for a coordinate.</summary>
    Task<string> GetTimeZoneAsync(GeoCoordinate coordinate, CancellationToken ct = default);

    /// <summary>Time zone for an address string.</summary>
    Task<string> GetTimeZoneAsync(string address, CancellationToken ct = default);

    /// <summary>Route between two coordinates.</summary>
    Task<Route> GetRouteAsync(GeoCoordinate start, GeoCoordinate end, RouteOptions? options = null, CancellationToken ct = default);

    /// <summary>Driving distance between two coordinates.</summary>
    Task<double> GetDrivingDistanceAsync(GeoCoordinate from, GeoCoordinate to, CancellationToken ct = default);

    /// <summary>Estimated travel time between two coordinates.</summary>
    Task<TimeSpan> GetEstimatedTravelTimeAsync(GeoCoordinate from, GeoCoordinate to, TransportMode mode, CancellationToken ct = default);
}