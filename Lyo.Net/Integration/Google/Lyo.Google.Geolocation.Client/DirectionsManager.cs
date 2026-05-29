using Lyo.Exceptions;
using Lyo.Geolocation.Models;
using Lyo.Geolocation.Models.Coordinates;
using Lyo.Geolocation.Models.Enums;
using Lyo.Google.Geolocation.Client.Mapping;
using Lyo.Google.Geolocation.Client.Models;

namespace Lyo.Google.Geolocation.Client;

/// <summary>Google Directions API operations.</summary>
public sealed class DirectionsManager(GoogleMapsClient client)
{
    public async Task<Route> GetRouteAsync(GeoCoordinate start, GeoCoordinate end, RouteOptions? options = null, CancellationToken ct = default)
    {
        options ??= new() { Mode = TransportMode.Driving };
        var url = client.BuildDirectionsUrl(start, end, options);
        var response = await client.GetAsAsync<GoogleDirectionsResponse>(url, ct: ct).ConfigureAwait(false);
        GoogleMapsApiStatus.EnsureOk(response?.Status, response?.ErrorMessage);
        if (response?.Routes == null || response.Routes.Count == 0)
            throw new NotFoundException($"No route found from {start.Latitude},{start.Longitude} to {end.Latitude},{end.Longitude}");

        return GoogleMapsMapper.ToRoute(response.Routes[0], start, end, options);
    }
}