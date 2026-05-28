# Lyo.Google.Geolocation.Client

Google Maps REST client and `IGeolocationService` implementation.

References [`Lyo.Geolocation`](../../../Core/Geolocation/Lyo.Geolocation/README.md) and [`Lyo.Geolocation.Models`](../../../Core/Geolocation/Lyo.Geolocation.Models/README.md) only — **not** `Lyo.Geolocation.Postgres`. The host wires this client together with the store.

## Overview

| Type | Role |
|------|------|
| `GoogleMapsClient` | Geocoding, directions, time zone APIs |
| `GoogleMapsGeolocationService` | `IGeolocationService` |
| `GoogleMapsMapper` | Google JSON → `Address` + `EntitySourceRecord` (`GoogleGeolocationSourceTypes.GoogleMapsPlace`) |

## Consumer composition

```csharp
services.AddPostgresGeolocationStoreFromConfiguration(configuration);
services.AddGoogleMapsClientFromConfiguration(configuration);
services.AddGoogleMapsGeolocationService();

// Worker/API: geocode, then persist
var result = await geolocationService.GeocodeAsync(query, ct);
await geolocationStore.SaveAddressAsync(result.Address, ct);
```

## Dependencies

- [`Lyo.Api.Client`](../../Api/Lyo.Api.Client/README.md)
- [`Lyo.Geolocation`](../../../Core/Geolocation/Lyo.Geolocation/README.md)
- [`Lyo.Geolocation.Models`](../../../Core/Geolocation/Lyo.Geolocation.Models/README.md)
- [`Lyo.EntityReference.Models`](../../../Core/EntityReference/Lyo.EntityReference.Models/README.md) (transitive via Geolocation)
