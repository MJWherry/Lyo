using Lyo.Api.Client;
using Lyo.Common;
using Lyo.Exceptions;
using Lyo.Geolocation.Models;
using Lyo.Geolocation.Models.Coordinates;
using Lyo.Geolocation.Models.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Google.Geolocation.Client;

/// <summary>Typed HTTP client for Google Maps REST APIs.</summary>
public sealed class GoogleMapsClient : ApiClient
{
    private static readonly DateTimeOffset UnixEpoch = new(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly GoogleMapsClientOptions _options;

    public GeocodingManager Geocoding { get; }

    public DirectionsManager Directions { get; }

    public TimeZoneManager TimeZones { get; }

    public GoogleMapsClient(GoogleMapsClientOptions options, ILoggerFactory? loggerFactory = null, HttpClient? httpClient = null)
        : base(
            loggerFactory?.CreateLogger<GoogleMapsClient>() ?? NullLoggerFactory.Instance.CreateLogger<GoogleMapsClient>(),
            httpClient,
            LyoJsonSerializerOptions.Create(),
            options)
    {
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(options.ApiKey, nameof(options.ApiKey));
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
            options.BaseUrl = "https://maps.googleapis.com/maps/api/";
        options.EnsureStatusCode = false;

        _options = options;
        Geocoding = new GeocodingManager(this);
        Directions = new DirectionsManager(this);
        TimeZones = new TimeZoneManager(this);
    }

    internal string BuildGeocodeUrl(string address)
    {
        var url = $"geocode/json?address={Uri.EscapeDataString(address)}&key={_options.ApiKey}";
        if (!string.IsNullOrEmpty(_options.DefaultLanguage))
            url += $"&language={_options.DefaultLanguage}";
        if (!string.IsNullOrEmpty(_options.DefaultRegion))
            url += $"&region={_options.DefaultRegion}";
        return url;
    }

    internal string BuildReverseGeocodeUrl(GeoCoordinate coordinate)
    {
        var url = $"geocode/json?latlng={coordinate.Latitude},{coordinate.Longitude}&key={_options.ApiKey}";
        if (!string.IsNullOrEmpty(_options.DefaultLanguage))
            url += $"&language={_options.DefaultLanguage}";
        return url;
    }

    internal string BuildTimeZoneUrl(GeoCoordinate coordinate)
    {
        var timestamp = (long)(DateTimeOffset.UtcNow - UnixEpoch).TotalSeconds;
        return $"timezone/json?location={coordinate.Latitude},{coordinate.Longitude}&timestamp={timestamp}&key={_options.ApiKey}";
    }

    internal string BuildDirectionsUrl(GeoCoordinate start, GeoCoordinate end, RouteOptions options)
    {
        var url = $"directions/json?origin={start.Latitude},{start.Longitude}&destination={end.Latitude},{end.Longitude}&key={_options.ApiKey}";
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
        if (avoids.Count > 0)
            url += $"&avoid={string.Join("|", avoids)}";

        if (options.DepartureTime.HasValue) {
            var departureTime = ((DateTimeOffset)options.DepartureTime.Value).ToUnixTimeSeconds();
            url += $"&departure_time={departureTime}";
        }
        else if (options.ArrivalTime.HasValue) {
            var arrivalTime = ((DateTimeOffset)options.ArrivalTime.Value).ToUnixTimeSeconds();
            url += $"&arrival_time={arrivalTime}";
        }

        var language = !string.IsNullOrEmpty(options.Language) ? options.Language : _options.DefaultLanguage;
        if (!string.IsNullOrEmpty(language))
            url += $"&language={language}";

        return url;
    }
}
