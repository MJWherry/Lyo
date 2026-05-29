using System.Diagnostics;
using Lyo.Exceptions;
using Lyo.Geolocation;
using Lyo.Geolocation.Models;
using Lyo.Geolocation.Models.Addresses;
using Lyo.Geolocation.Models.Coordinates;
using Lyo.Geolocation.Models.Enums;

namespace Lyo.Google.Geolocation.Client;

/// <summary>Google Maps implementation of <see cref="IGeolocationService" /> (no persistence cache).</summary>
public sealed class GoogleMapsGeolocationService : IGeolocationService
{
    private readonly GoogleMapsClient _client;

    public GoogleMapsGeolocationService(GoogleMapsClient client)
    {
        ArgumentHelpers.ThrowIfNull(client);
        _client = client;
    }

    /// <inheritdoc />
    public Task<GeocodeResult> GeocodeAsync(string address, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(address);
        return _client.Geocoding.GeocodeAsync(address, ct);
    }

    /// <inheritdoc />
    public Task<GeocodeResult> GeocodeAsync(Address address, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(address);
        return _client.Geocoding.GeocodeAsync(address, ct);
    }

    /// <inheritdoc />
    public async Task<BatchGeocodeResult> GeocodeBatchAsync(IEnumerable<string> addresses, CancellationToken ct = default)
    {
        var addressList = addresses.ToList();
        var sw = Stopwatch.StartNew();
        var items = new List<GeocodeResultItem>();
        for (var i = 0; i < addressList.Count; i++) {
            ct.ThrowIfCancellationRequested();
            var query = addressList[i];
            try {
                var result = await GeocodeAsync(query, ct).ConfigureAwait(false);
                items.Add(
                    new() {
                        Index = i,
                        OriginalQuery = query,
                        IsSuccess = true,
                        Result = result,
                        ErrorMessage = string.Empty
                    });
            }
            catch (Exception ex) {
                items.Add(
                    new() {
                        Index = i,
                        OriginalQuery = query,
                        IsSuccess = false,
                        ErrorMessage = ex.Message
                    });
            }
        }

        sw.Stop();
        return new() {
            TotalRequests = addressList.Count,
            SuccessfulResults = items.Count(x => x.IsSuccess),
            FailedResults = items.Count(x => !x.IsSuccess),
            Results = items,
            ProcessingTime = sw.Elapsed
        };
    }

    /// <inheritdoc />
    public Task<ReverseGeocodeResult> ReverseGeocodeAsync(double latitude, double longitude, CancellationToken ct = default) => ReverseGeocodeAsync(new(latitude, longitude), ct);

    /// <inheritdoc />
    public Task<ReverseGeocodeResult> ReverseGeocodeAsync(GeoCoordinate coordinate, CancellationToken ct = default) => _client.Geocoding.ReverseGeocodeAsync(coordinate, ct);

    /// <inheritdoc />
    public Task<double> GetDistanceAsync(GeoCoordinate from, GeoCoordinate to, DistanceUnit unit = DistanceUnit.Kilometers, CancellationToken ct = default)
        => Task.FromResult(GeolocationMath.GetDistance(from, to, unit));

    /// <inheritdoc />
    public async Task<double> GetDistanceAsync(string fromAddress, string toAddress, DistanceUnit unit = DistanceUnit.Kilometers, CancellationToken ct = default)
    {
        var from = await GeocodeAsync(fromAddress, ct).ConfigureAwait(false);
        var to = await GeocodeAsync(toAddress, ct).ConfigureAwait(false);
        return GeolocationMath.GetDistance(from.Coordinate, to.Coordinate, unit);
    }

    /// <inheritdoc />
    public Task<bool> IsWithinRadiusAsync(GeoCoordinate point1, GeoCoordinate point2, double radiusKm, CancellationToken ct = default)
        => Task.FromResult(GeolocationMath.IsWithinRadius(point1, point2, radiusKm));

    /// <inheritdoc />
    public Task<string> GetTimeZoneAsync(GeoCoordinate coordinate, CancellationToken ct = default) => _client.TimeZones.GetTimeZoneAsync(coordinate, ct);

    /// <inheritdoc />
    public async Task<string> GetTimeZoneAsync(string address, CancellationToken ct = default)
    {
        var result = await GeocodeAsync(address, ct).ConfigureAwait(false);
        return await GetTimeZoneAsync(result.Coordinate, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<Route> GetRouteAsync(GeoCoordinate start, GeoCoordinate end, RouteOptions? options = null, CancellationToken ct = default)
        => _client.Directions.GetRouteAsync(start, end, options, ct);

    /// <inheritdoc />
    public async Task<double> GetDrivingDistanceAsync(GeoCoordinate from, GeoCoordinate to, CancellationToken ct = default)
    {
        var route = await GetRouteAsync(from, to, new() { Mode = TransportMode.Driving }, ct).ConfigureAwait(false);
        return route.TotalDistanceMeters / 1000.0;
    }

    /// <inheritdoc />
    public async Task<TimeSpan> GetEstimatedTravelTimeAsync(GeoCoordinate from, GeoCoordinate to, TransportMode mode, CancellationToken ct = default)
    {
        var route = await GetRouteAsync(from, to, new() { Mode = mode }, ct).ConfigureAwait(false);
        return route.EstimatedDuration;
    }
}