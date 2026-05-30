# Lyo.Geolocation.Postgres

PostgreSQL persistence for canonical geolocation data using Entity Framework Core.

**Archetype A (Lyo domain).** Vendor clients such as [`Lyo.Google.Geolocation.Client`](../../../Integration/Google/Lyo.Google.Geolocation.Client/README.md) persist via
`IGeolocationStore` in the host. See [package layout](../../../docs/package-layout.md).

## Overview

Schema **`geolocation`**:

- **address** — Canonical normalized addresses (`Lyo.Geolocation.Models.Address`)
- **address_source** — provenance per import (`source_entity_*` + `imported_at`; owner `address_id`; lookup index on `source_entity_*`)

There is **no** `geocode_cache` table.

This project references only **`Lyo.Geolocation`**, **`Lyo.Geolocation.Models`**, and **`Lyo.EntityReference.Postgres`** — not Google, Endato, or other vendors.

## Usage

```csharp
services.AddPostgresGeolocationStoreFromConfiguration(configuration);
```

Import flow (in your worker/API — not in this package):

1. Call a vendor client (e.g. Google Maps) in a separate integration assembly.
2. Map to `Address` + `EntitySourceRecord.From(externalRef, importedAt)` on **`Sources`** (owner id set on save).
3. `await store.SaveAddressAsync(address, ct)` or `GetBySourceAsync` (queries **`source_entity_*`**) for idempotent ingest.

## Migrations

Schema: `geolocation`. Design-time: `GEOLOCATION_CONNECTION_STRING` env var.

## See also

[`Lyo.Geolocation`](../Lyo.Geolocation/README.md)
