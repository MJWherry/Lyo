using System.Diagnostics;
using System.Text.Json;
using Lyo.Common.Enums;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Geolocation.Models;
using Lyo.Geolocation.Models.Addresses;
using Lyo.Geolocation.Models.Coordinates;
using Lyo.Geolocation.Models.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Geolocation.Google;

/// <summary>Google Maps API implementation of IGeolocationService.</summary>
public sealed class GoogleGeolocationService : IGeolocationService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GoogleGeolocationService> _logger;
    private readonly GoogleOptions _options;
    private bool _disposed;

    public GoogleGeolocationService(GoogleOptions options, ILogger<GoogleGeolocationService>? logger = null, HttpClient? httpClient = null)
    {
        _options = options;
        _logger = logger ?? NullLoggerFactory.Instance.CreateLogger<GoogleGeolocationService>();
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds) };
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed) {
            _httpClient?.Dispose();
            _disposed = true;
        }
    }

    /// <inheritdoc />
    public async Task<GeoCoordinate> GeocodeAsync(string address)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(address, nameof(address));
        _logger.LogDebug("Geocoding address: {Address}", address);
        var url = $"{_options.BaseUrl}/geocode/json?address={Uri.EscapeDataString(address)}&key={_options.ApiKey}";
        if (!string.IsNullOrEmpty(_options.DefaultLanguage))
            url += $"&language={_options.DefaultLanguage}";

        if (!string.IsNullOrEmpty(_options.DefaultRegion))
            url += $"&region={_options.DefaultRegion}";

        var response = await CallApiAsync<GoogleGeocodeResponse>(url).ConfigureAwait(false);
        if (response?.Results == null || response.Results.Count == 0)
            throw new NotFoundException($"No geocoding results found for address: {address}");

        var result = response.Results[0];
        var location = result.Geometry?.Location;
        if (location == null)
            throw new InvalidFormatException("Invalid geocoding response: missing location data");

        var coordinate = new GeoCoordinate(location.Lat, location.Lng);
        _logger.LogDebug("Geocoded address '{Address}' to coordinates: {Latitude}, {Longitude}", address, coordinate.Latitude, coordinate.Longitude);
        return coordinate;
    }

    /// <inheritdoc />
    public async Task<GeoCoordinate> GeocodeAsync(Address address)
    {
        var addressString = address.GetFormattedAddress(AddressFormat.SingleLine);
        return await GeocodeAsync(addressString).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<GeoCoordinate>> GeocodeBatchAsync(IEnumerable<string> addresses)
    {
        var addressList = addresses.ToList();
        if (addressList.Count == 0)
            return [];

        _logger.LogDebug("Geocoding {Count} addresses in batch", addressList.Count);
        var tasks = addressList.Select(async (address, index) => {
            try {
                return await GeocodeAsync(address).ConfigureAwait(false);
            }
            catch (Exception ex) {
                _logger.LogWarning(ex, "Failed to geocode address at index {Index}: {Address}", index, address);
                return null;
            }
        });

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return results.Where(r => r != null).Cast<GeoCoordinate>();
    }

    /// <inheritdoc />
    public async Task<Address> ReverseGeocodeAsync(double latitude, double longitude) => await ReverseGeocodeAsync(new(latitude, longitude)).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<Address> ReverseGeocodeAsync(GeoCoordinate coordinate)
    {
        _logger.LogDebug("Reverse geocoding coordinates: {Latitude}, {Longitude}", coordinate.Latitude, coordinate.Longitude);
        var url = $"{_options.BaseUrl}/geocode/json?latlng={coordinate.Latitude},{coordinate.Longitude}&key={_options.ApiKey}";
        if (!string.IsNullOrEmpty(_options.DefaultLanguage))
            url += $"&language={_options.DefaultLanguage}";

        var response = await CallApiAsync<GoogleGeocodeResponse>(url).ConfigureAwait(false);
        if (response?.Results == null || response.Results.Count == 0)
            throw new NotFoundException($"No reverse geocoding results found for coordinates: {coordinate.Latitude}, {coordinate.Longitude}");

        var result = response.Results[0];
        var address = MapGoogleAddressToAddress(result);
        _logger.LogDebug("Reverse geocoded coordinates to address: {Address}", address.GetFormattedAddress());
        return address;
    }

    /// <inheritdoc />
    public async Task<double> GetDistanceAsync(GeoCoordinate from, GeoCoordinate to, DistanceUnit unit = DistanceUnit.Kilometers)
    {
        var distanceMeters = from.DistanceTo(to);
        return unit switch {
            DistanceUnit.Kilometers => distanceMeters / 1000.0,
            DistanceUnit.Miles => distanceMeters * 0.000621371,
            DistanceUnit.Feet => distanceMeters * 3.28084,
            DistanceUnit.NauticalMiles => distanceMeters * 0.000539957,
            var _ => distanceMeters
        };
    }

    /// <inheritdoc />
    public async Task<double> GetDistanceAsync(string fromAddress, string toAddress, DistanceUnit unit = DistanceUnit.Kilometers)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(fromAddress, nameof(fromAddress));
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(toAddress, nameof(toAddress));
        var fromCoord = await GeocodeAsync(fromAddress).ConfigureAwait(false);
        var toCoord = await GeocodeAsync(toAddress).ConfigureAwait(false);
        return await GetDistanceAsync(fromCoord, toCoord, unit).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<bool> IsWithinRadiusAsync(GeoCoordinate point1, GeoCoordinate point2, double radiusKm)
    {
        var distanceKm = point1.DistanceTo(point2, DistanceUnit.Kilometers);
        return Task.FromResult(distanceKm <= radiusKm);
    }

    /// <inheritdoc />
    public async Task<string> GetTimeZoneAsync(GeoCoordinate coordinate)
    {
        _logger.LogDebug("Getting time zone for coordinates: {Latitude}, {Longitude}", coordinate.Latitude, coordinate.Longitude);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var url = $"{_options.BaseUrl}/timezone/json?location={coordinate.Latitude},{coordinate.Longitude}&timestamp={timestamp}&key={_options.ApiKey}";
        var response = await CallApiAsync<GoogleTimeZoneResponse>(url).ConfigureAwait(false);
        if (response?.TimeZoneId == null)
            throw new NotFoundException($"No time zone found for coordinates: {coordinate.Latitude}, {coordinate.Longitude}");

        _logger.LogDebug("Found time zone: {TimeZoneId}", response.TimeZoneId);
        return response.TimeZoneId;
    }

    /// <inheritdoc />
    public async Task<string> GetTimeZoneAsync(string address)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(address, nameof(address));
        var coordinate = await GeocodeAsync(address).ConfigureAwait(false);
        return await GetTimeZoneAsync(coordinate).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Route> GetRouteAsync(GeoCoordinate start, GeoCoordinate end, RouteOptions? options = null)
    {
        _logger.LogDebug("Getting route from {StartLat},{StartLng} to {EndLat},{EndLng}", start.Latitude, start.Longitude, end.Latitude, end.Longitude);
        options ??= new() { Mode = TransportMode.Driving };
        var url = $"{_options.BaseUrl}/directions/json?origin={start.Latitude},{start.Longitude}&destination={end.Latitude},{end.Longitude}&key={_options.ApiKey}";
        var mode = options.Mode switch {
            TransportMode.Driving => "driving",
            TransportMode.Walking => "walking",
            TransportMode.Bicycling => "bicycling",
            TransportMode.Transit => "transit",
            var _ => "driving"
        };

        url += $"&mode={mode}";
        if (options.Waypoints != null && options.Waypoints.Any()) {
            var waypointStr = string.Join("|", options.Waypoints.Select(w => $"{w.Latitude},{w.Longitude}"));
            url += $"&waypoints={Uri.EscapeDataString(waypointStr)}";
            if (options.OptimizeWaypoints)
                url += "&optimize:true";
        }

        var avoids = new List<string>();
        if (options.AvoidTolls)
            avoids.Add("tolls");

        if (options.AvoidHighways)
            avoids.Add("highways");

        if (options.AvoidFerries)
            avoids.Add("ferries");

        if (avoids.Any())
            url += $"&avoid={string.Join("|", avoids)}";

        if (options.DepartureTime.HasValue) {
            var departureTime = ((DateTimeOffset)options.DepartureTime.Value).ToUnixTimeSeconds();
            url += $"&departure_time={departureTime}";
        }
        else if (options.ArrivalTime.HasValue) {
            var arrivalTime = ((DateTimeOffset)options.ArrivalTime.Value).ToUnixTimeSeconds();
            url += $"&arrival_time={arrivalTime}";
        }

        if (!string.IsNullOrEmpty(options.Language))
            url += $"&language={options.Language}";
        else if (!string.IsNullOrEmpty(_options.DefaultLanguage))
            url += $"&language={_options.DefaultLanguage}";

        var response = await CallApiAsync<GoogleDirectionsResponse>(url).ConfigureAwait(false);
        if (response?.Routes == null || response.Routes.Count == 0)
            throw new NotFoundException($"No route found from {start.Latitude},{start.Longitude} to {end.Latitude},{end.Longitude}");

        var route = MapGoogleRouteToRoute(response.Routes[0], start, end, options);
        _logger.LogDebug("Route found: {Distance}km, {Duration}", route.TotalDistanceMeters / 1000.0, route.EstimatedDuration);
        return route;
    }

    /// <inheritdoc />
    public async Task<double> GetDrivingDistanceAsync(GeoCoordinate from, GeoCoordinate to)
    {
        var route = await GetRouteAsync(from, to, new() { Mode = TransportMode.Driving }).ConfigureAwait(false);
        return route.TotalDistanceMeters / 1000.0;
    }

    /// <inheritdoc />
    public async Task<TimeSpan> GetEstimatedTravelTimeAsync(GeoCoordinate from, GeoCoordinate to, TransportMode mode)
    {
        var route = await GetRouteAsync(from, to, new() { Mode = mode }).ConfigureAwait(false);
        return route.EstimatedDuration;
    }

    private async Task<T> CallApiAsync<T>(string url)
        where T : class
    {
        try {
            var sw = Stopwatch.StartNew();
            var response = await _httpClient.GetAsync(url).ConfigureAwait(false);
            sw.Stop();
            _logger.LogDebug("API call completed in {Elapsed}ms: {Url}", sw.ElapsedMilliseconds, url);
            if (!response.IsSuccessStatusCode) {
                var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                _logger.LogError("API call failed with status {StatusCode}: {Error}", response.StatusCode, errorContent);
                throw new ServiceUnavailableException($"Google Maps API returned status {response.StatusCode}: {errorContent}");
            }

            var jsonContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var result = JsonSerializer.Deserialize<T>(jsonContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (result == null)
                throw new InvalidFormatException("Failed to deserialize API response");

            return result;
        }
        catch (HttpRequestException ex) {
            _logger.LogError(ex, "HTTP error calling Google Maps API");
            throw new ServiceUnavailableException("Failed to connect to Google Maps API", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException) {
            _logger.LogError(ex, "Timeout calling Google Maps API");
            throw new ServiceUnavailableException("Google Maps API request timed out", ex);
        }
        catch (JsonException ex) {
            _logger.LogError(ex, "Failed to parse Google Maps API response");
            throw new InvalidFormatException("Invalid response format from Google Maps API", ex);
        }
    }

    private Address MapGoogleAddressToAddress(GoogleGeocodeResult googleResult)
    {
        var addressComponents = googleResult.AddressComponents ?? new List<GoogleAddressComponent>();
        var address = new Address {
            Coordinate = googleResult.Geometry?.Location != null ? new GeoCoordinate(googleResult.Geometry.Location.Lat, googleResult.Geometry.Location.Lng) : null
            //PlaceId = googleResult.PlaceId
        };

        foreach (var component in addressComponents) {
            var types = component.Types ?? new List<string>();
            if (types.Contains("street_number"))
                address.StreetNumber = component.LongName;
            else if (types.Contains("route"))
                address.StreetName = component.LongName;
            else if (types.Contains("locality"))
                address.City = component.LongName;
            else if (types.Contains("administrative_area_level_1"))
                address.State = component.ShortName;
            else if (types.Contains("postal_code")) {
                address.Zipcode = component.LongName;
                address.PostalCode = component.LongName;
            }
            else if (types.Contains("country")) {
                var countryCode = component.ShortName;
                if (Enum.TryParse<CountryCode>(countryCode, out var code))
                    address.CountryCode = code;
            }
            else if (types.Contains("sublocality") || types.Contains("neighborhood"))
                address.SubLocality = component.LongName;
            else if (types.Contains("administrative_area_level_2"))
                address.County = component.LongName;
        }

        // If we don't have a formatted street address, use the formatted address from Google
        if (string.IsNullOrEmpty(address.StreetAddress) && !string.IsNullOrEmpty(googleResult.FormattedAddress)) {
            // Try to extract street address from formatted address
            var parts = googleResult.FormattedAddress.Split(',');
            if (parts.Length > 0)
                address.StreetAddress = parts[0].Trim();
        }

        // Set formatted address if StreetAddress is still empty
        if (string.IsNullOrEmpty(address.StreetAddress))
            address.StreetAddress = googleResult.FormattedAddress;

        return address;
    }

    private Route MapGoogleRouteToRoute(GoogleRoute googleRoute, GeoCoordinate start, GeoCoordinate end, RouteOptions options)
    {
        var route = new Route {
            Id = Guid.NewGuid(),
            StartPoint = start,
            EndPoint = end,
            TransportMode = options.Mode,
            Summary = googleRoute.Summary,
            Warnings = googleRoute.Warnings ?? new List<string>()
        };

        var leg = googleRoute.Legs?.FirstOrDefault();
        if (leg != null) {
            route.TotalDistanceMeters = leg.Distance?.Value ?? 0;
            route.EstimatedDuration = TimeSpan.FromSeconds(leg.Duration?.Value ?? 0);
            if (leg.DurationInTraffic != null)
                route.DurationInTraffic = TimeSpan.FromSeconds(leg.DurationInTraffic.Value);

            // Map steps
            if (leg.Steps != null) {
                route.Steps = leg.Steps.Select((step, index) => new RouteStep {
                        StepNumber = index + 1,
                        StartLocation = step.StartLocation != null ? new(step.StartLocation.Lat, step.StartLocation.Lng) : start,
                        EndLocation = step.EndLocation != null ? new(step.EndLocation.Lat, step.EndLocation.Lng) : end,
                        DistanceMeters = step.Distance?.Value ?? 0,
                        Duration = TimeSpan.FromSeconds(step.Duration?.Value ?? 0),
                        Instructions = step.HtmlInstructions ?? step.Instructions ?? string.Empty,
                        RoadName = step.RoadName ?? string.Empty,
                        Maneuver = MapGoogleManeuver(step.Maneuver)
                    })
                    .ToList();
            }

            // Calculate bounds
            if (leg.StartLocation != null && leg.EndLocation != null) {
                var swLat = Math.Min(leg.StartLocation.Lat, leg.EndLocation.Lat);
                var swLng = Math.Min(leg.StartLocation.Lng, leg.EndLocation.Lng);
                var neLat = Math.Max(leg.StartLocation.Lat, leg.EndLocation.Lat);
                var neLng = Math.Max(leg.StartLocation.Lng, leg.EndLocation.Lng);
                route.Bounds = new() { Southwest = new(swLat, swLng), Northeast = new(neLat, neLng) };
            }
        }

        return route;
    }

    private ManeuverType MapGoogleManeuver(string? maneuver)
    {
        if (string.IsNullOrEmpty(maneuver))
            return ManeuverType.Straight;

        return maneuver.ToLowerInvariant() switch {
            "turn-left" => ManeuverType.TurnLeft,
            "turn-right" => ManeuverType.TurnRight,
            "turn-slight-left" => ManeuverType.TurnSlightLeft,
            "turn-slight-right" => ManeuverType.TurnSlightRight,
            "turn-sharp-left" => ManeuverType.TurnSharpLeft,
            "turn-sharp-right" => ManeuverType.TurnSharpRight,
            "uturn-left" or "uturn-right" => ManeuverType.UTurn,
            "straight" => ManeuverType.Straight,
            "ramp-left" => ManeuverType.RampLeft,
            "ramp-right" => ManeuverType.RampRight,
            "merge" => ManeuverType.Merge,
            "fork-left" or "fork-right" => ManeuverType.Fork,
            "keep-left" => ManeuverType.KeepLeft,
            "keep-right" => ManeuverType.KeepRight,
            "roundabout-left" or "roundabout-right" => ManeuverType.Roundabout,
            "ferry" => ManeuverType.Ferry,
            var _ => ManeuverType.Straight
        };
    }
}