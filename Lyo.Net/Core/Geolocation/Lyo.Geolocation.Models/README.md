# Lyo.Geolocation.Models

Neutral data contracts for [`Lyo.Geolocation`](../Lyo.Geolocation/README.md) and [`Lyo.Geolocation.Postgres`](../Lyo.Geolocation.Postgres/README.md).

No HTTP stacks and **no vendor client packages** — hosts that only need DTOs can reference this project alone.

## Provenance

[`Addresses.Address`](Addresses/Address.cs) exposes `ICollection<EntitySourceRecord> Sources`. Persisted via [`address_source`](../Lyo.Geolocation.Postgres/README.md). The *
*`source_entity_type`** string is chosen by the **importing application** (e.g. a constant in `Lyo.Google.Geolocation.Client`), not hard-coded in this package.

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

- [`Lyo.Common`](../../Common/Lyo.Common/README.md)
- [`Lyo.EntityReference.Models`](../../EntityReference/Lyo.EntityReference.Models/README.md)
