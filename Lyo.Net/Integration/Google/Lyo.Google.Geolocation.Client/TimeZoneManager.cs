using Lyo.Common.Extensions;
using Lyo.Exceptions;
using Lyo.Geolocation.Models.Coordinates;
using Lyo.Google.Geolocation.Client.Models;

namespace Lyo.Google.Geolocation.Client;

/// <summary>Google Time Zone API operations.</summary>
public sealed class TimeZoneManager(GoogleMapsClient client)
{
    public async Task<string> GetTimeZoneAsync(GeoCoordinate coordinate, CancellationToken ct = default)
    {
        var url = client.BuildTimeZoneUrl(coordinate);
        var response = await client.GetAsAsync<GoogleTimeZoneResponse>(url, ct: ct).ConfigureAwait(false);
        GoogleMapsApiStatus.EnsureOk(response?.Status, response?.ErrorMessage);
        if (response is null || response.TimeZoneId.IsNullOrEmpty())
            throw new NotFoundException($"No time zone found for coordinates: {coordinate.Latitude}, {coordinate.Longitude}");

        return response.TimeZoneId;
    }
}