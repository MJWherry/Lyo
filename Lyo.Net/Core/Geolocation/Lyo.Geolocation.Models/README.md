# Lyo.Geolocation.Models

Vendor-neutral data contracts used by [`Lyo.Geolocation`](../Lyo.Geolocation/README.md) and [`Lyo.Geolocation.Postgres`](../Lyo.Geolocation.Postgres/README.md).

No HTTP stacks and no vendor client packages. Hosts that only need DTOs can reference this project alone.

## Where data came from

`IEntitySourceDerived` is implemented by [`Addresses.Address`](Addresses/Address.cs) (`ICollection<EntitySourceRecord> Sources`, optional `LocallyModifiedAt`). Persistence is [`address_source`](../Lyo.Geolocation.Postgres/README.md). The parent `address` carries owner identity (`address_id` FK). External source lives in `source_entity_*` + `imported_at`, for example `GoogleMapsPlace` plus a place id. The importing application chooses `source_entity_type` (for example `Lyo.Google.Geolocation.Client`), not this package. Map vendor DTOs with `EntitySourceRecord.From(source, importedAt)` before persist.

## Catalog of DTOs

**Geometry and coordinates**

| Type | What it does |
| --------------------------- | -------------------------------------------------------------------------------------- |
| `Coordinates.GeoCoordinate` | Longitude / latitude plus optional accuracy, altitude, timestamp. |
| `BoundingBox` | Northeast/Southwest pair with `Contains`, `Center`, `Intersects`, `Expand`, factories. |

**Addresses**

| Type | Role |
|--------------------------------|------------------------------------------------------------------------------------------|
| `Addresses.Address` | Unified US + international address. Endato-shaped optional enrichment fields. `Sources`. |
| `Addresses.ContactAddress` | Person to address junction, used by People flows. |

**Places, routing, geocoding, distance.** See `Place`, `Route`, `GeocodeResult`, and the other types in this package.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.EntityReference.Models` (direct, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)