# Lyo.Geolocation

Provider-agnostic geospatial operations and persistence contracts.

**Archetype A (Lyo domain).** Vendor Maps clients (e.g. [`Lyo.Google.Geolocation.Client`](../../../Integration/Google/Lyo.Google.Geolocation.Client/README.md)) are Archetype C under `Integration/{Vendor}/`. See [package layout](../../../docs/package-layout.md).

## Examples

### Consumer composition (example)

```csharp
// Host project references: Lyo.Geolocation.Postgres, Lyo.Google.Geolocation.Client, …
services.AddPostgresGeolocationStoreFromConfiguration(configuration);
services.AddGoogleMapsClientFromConfiguration(configuration);
services.AddGoogleMapsGeolocationService();

// Ingest: call IGeolocationService, map to Address + EntitySourceRecord, then:
await geolocationStore.SaveAddressAsync(address, ct);
```

## Assemblies

| Package | Role |
| ------------------------------------------------------------------- | ------------------------------------------------------------------------------------ |
| [`Lyo.Geolocation.Models`](../Lyo.Geolocation.Models/README.md) | Domain DTOs (`Address`, `GeocodeResult`, `Route`, …) |
| **`Lyo.Geolocation`** (this) | `IGeolocationService`, `IGeolocationStore`, `GeolocationQueryKey`, `GeolocationMath` |
| [`Lyo.Geolocation.Postgres`](../Lyo.Geolocation.Postgres/README.md) | EF Core store (`geolocation` schema) |

This package does **not** reference HTTP clients or vendor SDKs. Wire providers and import mappers in the **host** (API, worker, tool).

## `IGeolocationService`

Contract for geocoding, routing, time zone, and distance operations (implementations live in separate integration packages).

## `IGeolocationStore`

- Canonical **`geolocation.address`** rows
- **`geolocation.address_source`** — import provenance: **`source_entity_*`** + **`imported_at`** (owner `address_id` on parent); external type strings come from the importing app (e.g. `GoogleMapsPlace`)
- **`GetBySourceAsync`** (matches **`source_entity_*`**) / **`SaveAddressAsync`** — parent **`Address`** implements **`IEntitySourceDerived`** (`Sources`, optional * *`LocallyModifiedAt`**)

## Consumer composition (example)

A worker or API registers **store + provider(s)** and owns mapping from vendor DTOs to `Address` + `Sources`: Vendor-specific `EntityRef` type names (e.g. `GoogleMapsPlace`) are defined in the integration package that performs the mapping, not in this assembly.

## See also

[`Lyo.People.Models`](../People/Lyo.People.Models/README.md) — internal people rows with their own `*_source` tables; link across stores via `EntityRef` at import time.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.EntityReference.Models` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Geolocation.Models` — (direct, lyo)
- `Lyo.Common` — (transitive, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)