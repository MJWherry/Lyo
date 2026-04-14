# Lyo.Geolocation.Google

Google Maps API implementation for the Lyo Geolocation library.

## Overview

This package provides a `GoogleGeolocationService` that implements `IGeolocationService` using Google Maps APIs including:

- Geocoding API (address to coordinates)
- Reverse Geocoding API (coordinates to address)
- Directions API (routing)
- Time Zone API

## Configuration

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

## Usage

```csharp
// Geocode an address
var coordinate = await service.GeocodeAsync("1600 Amphitheatre Parkway, Mountain View, CA");

// Reverse geocode coordinates
var address = await service.ReverseGeocodeAsync(37.4220, -122.0841);

// Get a route
var route = await service.GetRouteAsync(
    new GeoCoordinate(37.4220, -122.0841),
    new GeoCoordinate(37.7749, -122.4194),
    new RouteOptions { Mode = TransportMode.Driving }
);

// Get time zone
var timeZone = await service.GetTimeZoneAsync(new GeoCoordinate(37.4220, -122.0841));
```

## Notes

The implementation uses direct HTTP calls to Google Maps REST APIs, which is the standard approach. The `Geo.Google` NuGet package is included as a dependency and can be used for
additional Google Maps functionality if needed.

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Geolocation.Google.csproj`.)*

**Target framework:** `netstandard2.0;net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `Geo.Google` | `2.*` |
| `Microsoft.Extensions.Logging.Abstractions` | `[10,)` |

### Project references

- `Lyo.Exceptions`
- `Lyo.Geolocation`
- `Lyo.Geolocation.Models`

## Public API (generated)

Top-level `public` types in `*.cs` (*2*). Nested types and file-scoped namespaces may omit some entries.

- `GoogleGeolocationService`
- `GoogleOptions`

<!-- LYO_README_SYNC:END -->

