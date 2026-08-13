# Lyo.Google.Geolocation.Client

Google Maps REST client and `IGeolocationService` implementation.

**Archetype C** — `Integration/Google/` (vendor folder). Geolocation is not Communication; this package mirrors `Lyo.Endato.Client` + Core store, not `Lyo.Translation.Google`. Path confirmed in [package layout](../../../docs/package-layout.md) (Phase 1).

References [`Lyo.Geolocation`](../../../Core/Geolocation/Lyo.Geolocation/README.md) and [`Lyo.Geolocation.Models`](../../../Core/Geolocation/Lyo.Geolocation.Models/README.md) only — **not** `Lyo.Geolocation.Postgres`. The host wires this client together with the store.

## Examples

### Consumer composition

```csharp
services.AddPostgresGeolocationStoreFromConfiguration(configuration);
services.AddGoogleMapsClientFromConfiguration(configuration);
services.AddGoogleMapsGeolocationService();

// Worker/API: geocode, then persist
var result = await geolocationService.GeocodeAsync(query, ct);
await geolocationStore.SaveAddressAsync(result.Address, ct);
```

## Overview

| Type | Role |
| ------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------- |
| `GoogleMapsClient` | Geocoding, directions, time zone APIs |
| `GoogleMapsGeolocationService` | `IGeolocationService` |
| `GoogleMapsMapper` | Google JSON → `Address` + `EntitySourceRecord.From(...)` (`GoogleGeolocationSourceTypes.GoogleMapsPlace` on **`source_entity_*`**) |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api.Client` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Geolocation` — (direct, lyo)
- `Lyo.Geolocation.Models` — (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (direct, microsoft)
- `Lyo.Api.Models` — (transitive, lyo)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.DateAndTime` — (transitive, lyo)
- `Lyo.Diagnostic` — (transitive, lyo)
- `Lyo.EntityReference.Models` — (transitive, lyo)
- `Lyo.Hashing` — (transitive, lyo)
- `Lyo.PackageMetadata` — (transitive, lyo)
- `Lyo.Query.Models` — (transitive, lyo)
- `Microsoft.Extensions.Http` `10.0.5` — (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` — (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` — (transitive, microsoft)