# Lyo.Geolocation

Geospatial operations and persistence contracts that do not pick a vendor.

**Archetype A (Lyo domain).** Vendor Maps clients (e.g. [`Lyo.Google.Geolocation.Client`](../../../Integration/Google/Lyo.Google.Geolocation.Client/README.md)) are Archetype C under `Integration/{Vendor}/`. See [package layout](../../../docs/package-layout.md).

## Examples

### How a consumer composes this

```csharp
// Host project references: Lyo.Geolocation.Postgres, Lyo.Google.Geolocation.Client, …
services.AddPostgresGeolocationStoreFromConfiguration(configuration);
services.AddGoogleMapsClientFromConfiguration(configuration);
services.AddGoogleMapsGeolocationService();

// Ingest: call IGeolocationService, map to Address + EntitySourceRecord, then:
await geolocationStore.SaveAddressAsync(address, ct);
```

## Assemblies

| Package | What it does |
| ------------------------------------------------------------------- | ------------------------------------------------------------------------------------ |
| [`Lyo.Geolocation.Models`](../Lyo.Geolocation.Models/README.md) | Domain DTOs (`GeocodeResult`, `Address`, `Route`, …) |
| `Lyo.Geolocation` (this) | `IGeolocationService`, `IGeolocationStore`, `GeolocationQueryKey`, `GeolocationMath` |
| [`Lyo.Geolocation.Postgres`](../Lyo.Geolocation.Postgres/README.md) | EF Core persistence (`geolocation` schema) |

HTTP clients and vendor SDKs are not referenced here. Wire providers and import mappers in the host (API, worker, tool).

## `IGeolocationService`

The contract for routing, geocoding, distance, and time zone work (implementations live in separate integration packages).

## `IGeolocationStore`

- Canonical rows in `geolocation.address`
- `geolocation.address_source`. Import provenance: `source_entity_*` + `imported_at` (owner `address_id` on parent). The importing app supplies external type strings, for example `GoogleMapsPlace`.
- `SaveAddressAsync` / `GetBySourceAsync` (matches `source_entity_*`). Parent `Address` implements `IEntitySourceDerived` (`Sources`, optional `LocallyModifiedAt`).

## How a consumer composes this

The API or worker registers provider(s) and the store, and maps vendor DTOs onto `Address` + `Sources`. Vendor-specific `EntityRef` type names (for example `GoogleMapsPlace`) belong in the integration package that does the mapping, not this assembly.

## Related reading

Internal people rows with their own `*_source` tables: [`Lyo.People.Models`](../People/Lyo.People.Models/README.md). At import time, link across stores with `EntityRef`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.EntityReference.Models` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Geolocation.Models` (direct, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)