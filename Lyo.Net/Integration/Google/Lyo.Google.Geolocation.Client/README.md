# Lyo.Google.Geolocation.Client

REST client for Google Maps plus an `IGeolocationService` implementation.

**Archetype C.** Lives in `Integration/Google/` (vendor folder). Geolocation does not belong under Communication. The shape follows `Lyo.Endato.Client` plus a Core store, not `Lyo.Translation.Google`. Path is confirmed in [package layout](../../../docs/package-layout.md) (Phase 1).

Depends on [`Lyo.Geolocation`](../../../Core/Geolocation/Lyo.Geolocation/README.md) and [`Lyo.Geolocation.Models`](../../../Core/Geolocation/Lyo.Geolocation.Models/README.md) only — not `Lyo.Geolocation.Postgres`. The host composes this client with the store.

## Examples

### Host composition

```csharp
services.AddPostgresGeolocationStoreFromConfiguration(configuration);
services.AddGoogleMapsClientFromConfiguration(configuration);
services.AddGoogleMapsGeolocationService();

// Worker/API: geocode, then persist
var result = await geolocationService.GeocodeAsync(query, ct);
await geolocationStore.SaveAddressAsync(result.Address, ct);
```

## At a glance

| Type | Role |
| ------------------------------ | ------------------------------------------------------------------------------------------------------------------------------ |
| `GoogleMapsClient` | Geocoding, directions, and time zone APIs |
| `GoogleMapsGeolocationService` | `IGeolocationService` |
| `GoogleMapsMapper` | Google JSON → `Address` + `EntitySourceRecord.From(...)` (`GoogleGeolocationSourceTypes.GoogleMapsPlace` on `source_entity_*`) |

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Json` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Geolocation` (direct, lyo)
- `Lyo.Geolocation.Models` (direct, lyo)
- `Lyo.Http.Client` (direct, lyo)
- `Lyo.Metrics` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Http` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Configuration` (transitive, lyo)
- `Lyo.EntityReference.Models` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `AngleSharp` `1.5.0` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.RateLimiting` `10.0.5` (transitive, microsoft)