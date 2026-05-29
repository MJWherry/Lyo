using Lyo.Exceptions;
using Lyo.Geolocation.Models;
using Lyo.Geolocation.Models.Addresses;
using Lyo.Geolocation.Models.Coordinates;
using Lyo.Google.Geolocation.Client.Mapping;
using Lyo.Google.Geolocation.Client.Models;

namespace Lyo.Google.Geolocation.Client;

/// <summary>Google Geocoding API operations.</summary>
public sealed class GeocodingManager(GoogleMapsClient client)
{
    public async Task<GeocodeResult> GeocodeAsync(string address, CancellationToken ct = default)
    {
        var url = client.BuildGeocodeUrl(address);
        var response = await client.GetAsAsync<GoogleGeocodeResponse>(url, ct: ct).ConfigureAwait(false);
        GoogleMapsApiStatus.EnsureOk(response?.Status, response?.ErrorMessage);
        if (response?.Results == null || response.Results.Count == 0)
            throw new NotFoundException($"No geocoding results found for address: {address}");

        return GoogleMapsMapper.ToGeocodeResult(response.Results[0]);
    }

    public async Task<GeocodeResult> GeocodeAsync(Address address, CancellationToken ct = default)
    {
        var addressString = address.GetFormattedAddress(AddressFormat.SingleLine);
        return await GeocodeAsync(addressString, ct).ConfigureAwait(false);
    }

    public async Task<ReverseGeocodeResult> ReverseGeocodeAsync(GeoCoordinate coordinate, CancellationToken ct = default)
    {
        var url = client.BuildReverseGeocodeUrl(coordinate);
        var response = await client.GetAsAsync<GoogleGeocodeResponse>(url, ct: ct).ConfigureAwait(false);
        GoogleMapsApiStatus.EnsureOk(response?.Status, response?.ErrorMessage);
        if (response?.Results == null || response.Results.Count == 0)
            throw new NotFoundException($"No reverse geocoding results for: {coordinate.Latitude}, {coordinate.Longitude}");

        return GoogleMapsMapper.ToReverseGeocodeResult(response.Results[0], coordinate);
    }
}