# Lyo.Geolocation.Models

Neutral data contracts for [`Lyo.Geolocation`](../Lyo.Geolocation/README.md) and [`Lyo.Geolocation.Postgres`](../Lyo.Geolocation.Postgres/README.md).

No HTTP stacks and **no vendor client packages** — hosts that only need DTOs can reference this project alone.

## Provenance

[`Addresses.Address`](Addresses/Address.cs) implements **`IEntitySourceDerived`** (`ICollection<EntitySourceRecord> Sources`, optional **`LocallyModifiedAt`**). Persisted via [
`address_source`](../Lyo.Geolocation.Postgres/README.md): - Owner identity on parent **`address`** (`address_id` FK) - **`source_entity_*`** + **`imported_at`** — external source
(e.g. `GoogleMapsPlace` + place id) The **`source_entity_type`** string is chosen by the **importing application** (e.g. `Lyo.Google.Geolocation.Client`), not this package. Use * *
`EntitySourceRecord.From(source, importedAt)`** when mapping vendor DTOs before persist.

## DTO catalog

**Coordinates & geometry**

| Type                            | Role                                                                                   |
|---------------------------------|----------------------------------------------------------------------------------------|
| **`Coordinates.GeoCoordinate`** | Latitude / longitude (+ optional altitude, accuracy, timestamp).                       |
| **`BoundingBox`**               | Southwest/Northeast pair with `Center`, `Contains`, `Intersects`, `Expand`, factories. |

**Addresses**

| Type                           | Role                                                                                     |
|--------------------------------|------------------------------------------------------------------------------------------|
| **`Addresses.Address`**        | Unified US + international address; Endato-shaped optional enrichment fields; `Sources`. |
| **`Addresses.ContactAddress`** | Person ↔ address junction (used by People flows).                                        |

**Geocoding, routing, places, distance** — see existing sections in prior docs (`GeocodeResult`, `Route`, `Place`, …).

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` — (direct, lyo)
- `Lyo.EntityReference.Models` — (direct, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)