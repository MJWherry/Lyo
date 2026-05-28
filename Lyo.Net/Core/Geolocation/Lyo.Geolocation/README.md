# Lyo.Geolocation

Provider-agnostic geospatial operations and persistence contracts.

## Assemblies

| Package | Role |
|---------|------|
| [`Lyo.Geolocation.Models`](../Lyo.Geolocation.Models/README.md) | Domain DTOs (`Address`, `GeocodeResult`, `Route`, …) |
| **`Lyo.Geolocation`** (this) | `IGeolocationService`, `IGeolocationStore`, `GeolocationQueryKey`, `GeolocationMath` |
| [`Lyo.Geolocation.Postgres`](../Lyo.Geolocation.Postgres/README.md) | EF Core store (`geolocation` schema) |

This package does **not** reference HTTP clients or vendor SDKs. Wire providers and import mappers in the **host** (API, worker, tool).

## `IGeolocationService`

Contract for geocoding, routing, time zone, and distance operations (implementations live in separate integration packages).

## `IGeolocationStore`

Persistence boundary (implemented by [`PostgresGeolocationStore`](../Lyo.Geolocation.Postgres/PostgresGeolocationStore.cs)):

- Canonical **`geolocation.address`** rows
- **`geolocation.address_source`** — `EntityRef` provenance (`source_entity_type` + `source_entity_id` strings you define at import time)
- **`GetBySourceAsync`** / **`SaveAddressAsync`**

Use [`Lyo.Cache`](../Cache/Lyo.Cache/README.md) in the host if you need query-key caching.

## Consumer composition (example)

A worker or API registers **store + provider(s)** and owns mapping from vendor DTOs to `Address` + `Sources`:

```csharp
// Host project references: Lyo.Geolocation.Postgres, Lyo.Google.Geolocation.Client, …
services.AddPostgresGeolocationStoreFromConfiguration(configuration);
services.AddGoogleMapsClientFromConfiguration(configuration);
services.AddGoogleMapsGeolocationService();

// Ingest: call IGeolocationService, map to Address + EntitySourceRecord, then:
await geolocationStore.SaveAddressAsync(address, ct);
```

Vendor-specific `EntityRef` type names (e.g. `GoogleMapsPlace`) are defined in the integration package that performs the mapping, not in this assembly.

## See also

[`Lyo.People.Models`](../People/Lyo.People.Models/README.md) — internal people rows with their own `*_source` tables; link across stores via `EntityRef` at import time.
