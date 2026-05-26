# Lyo.Geolocation.Google

Google Maps API implementation for the Lyo Geolocation library.

## Overview

This package provides a `GoogleGeolocationService` that implements `IGeolocationService` using Google Maps APIs including:

- Geocoding API (address to coordinates)
- Reverse Geocoding API (coordinates to address)
- Directions API (routing)
- Time Zone API

## Configuration

`GoogleOptions` (`SectionName = "GoogleOptions"`) carries:

| Property | Notes |
|----------|-------|
| `ApiKey` | Required Google Maps API key. |
| `BaseUrl` | Defaults to `https://maps.googleapis.com/maps/api`. |
| `DefaultLanguage` | Optional `language=` parameter applied to geocoding, reverse-geocoding, and directions calls. |
| `DefaultRegion` | Optional `region=` bias applied to geocoding calls. |
| `TimeoutSeconds` | Used to construct the default `HttpClient` (30 s by default). |

```csharp
var options = new GoogleOptions
{
    ApiKey = "your-google-maps-api-key",
    DefaultLanguage = "en",
    DefaultRegion = "us",
    TimeoutSeconds = 30
};

var service = new GoogleGeolocationService(options, logger);
```

`GoogleGeolocationService` accepts `(GoogleOptions options, ILogger<GoogleGeolocationService>? logger = null, HttpClient? httpClient = null)`. When no `HttpClient` is supplied the
service constructs one with the configured timeout; it implements `IDisposable` and disposes any owned `HttpClient`. No `AddGoogle*` DI extension ships in this project—register
the service yourself, for example:

```csharp
services.AddSingleton(options);
services.AddHttpClient<GoogleGeolocationService>();
services.AddSingleton<IGeolocationService>(sp => sp.GetRequiredService<GoogleGeolocationService>());
```

## Usage

```csharp
// Geocode (string or Address)
var coordinate = await service.GeocodeAsync("1600 Amphitheatre Parkway, Mountain View, CA");
var fromAddress = await service.GeocodeAsync(new Address { /* ... */ });
var batch = await service.GeocodeBatchAsync(new[] { "addr1", "addr2" });

// Reverse geocode coordinates
var address = await service.ReverseGeocodeAsync(37.4220, -122.0841);
var sameAddress = await service.ReverseGeocodeAsync(new GeoCoordinate(37.4220, -122.0841));

// Distances — coordinate or address overloads (address overloads geocode internally)
var coordKm = await service.GetDistanceAsync(coord1, coord2, DistanceUnit.Kilometers);
var addrKm  = await service.GetDistanceAsync("Mountain View, CA", "San Francisco, CA", DistanceUnit.Kilometers);
var nearby  = await service.IsWithinRadiusAsync(coord1, coord2, radiusKm: 50);

// Routing
var route = await service.GetRouteAsync(
    new GeoCoordinate(37.4220, -122.0841),
    new GeoCoordinate(37.7749, -122.4194),
    new RouteOptions { Mode = TransportMode.Driving });

var driveKm = await service.GetDrivingDistanceAsync(coord1, coord2);
var eta     = await service.GetEstimatedTravelTimeAsync(coord1, coord2, TransportMode.Walking);

// Time zone — coordinate or address overloads
var tzFromCoord   = await service.GetTimeZoneAsync(new GeoCoordinate(37.4220, -122.0841));
var tzFromAddress = await service.GetTimeZoneAsync("1600 Amphitheatre Parkway, Mountain View, CA");
```

## Notes

- Coordinate-based `GetDistanceAsync` runs locally via `GeoCoordinate.DistanceTo` (Haversine); the string-address overload first geocodes both endpoints, so it makes two REST
  calls before measuring.
- `GetTimeZoneAsync(string address)` is a convenience that geocodes the address then calls the coordinate overload (Google Time Zone API).
- HTTP failures surface as `ServiceUnavailableException`; non-JSON responses surface as `InvalidFormatException`; empty result sets surface as `NotFoundException`.
- The implementation uses direct HTTP calls to Google Maps REST APIs. The `Geo.Google` NuGet package is referenced for shared types and can be used for additional Google Maps
  functionality if needed.


## Dependencies

*(Synchronized from `Lyo.Geolocation.Google.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

| Package                                     | Version |
|---------------------------------------------|---------|
| `Geo.Google`                                | `2.*`   |
| `Microsoft.Extensions.Logging.Abstractions` | `[10,)` |

### Project references

- [`Lyo.Exceptions`](../../Lyo.Exceptions/README.md)
- [`Lyo.Geolocation`](../Lyo.Geolocation/README.md)
- [`Lyo.Geolocation.Models`](../Lyo.Geolocation.Models/README.md)