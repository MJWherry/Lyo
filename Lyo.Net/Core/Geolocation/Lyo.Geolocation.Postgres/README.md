# Lyo.Geolocation.Postgres

PostgreSQL persistence for canonical geolocation data using Entity Framework Core.

**Archetype A (Lyo domain).** Vendor clients such as [`Lyo.Google.Geolocation.Client`](../../../Integration/Google/Lyo.Google.Geolocation.Client/README.md) persist via
`IGeolocationStore` in the host. See [package layout](../../../docs/package-layout.md).

## Overview

Schema **`geolocation`**:

- **address** — Canonical normalized addresses (`Lyo.Geolocation.Models.Address`)
- **address_source** — `EntityRef` provenance per import

There is **no** `geocode_cache` table.

This project references only **`Lyo.Geolocation`**, **`Lyo.Geolocation.Models`**, and **`Lyo.EntityReference.Postgres`** — not Google, Endato, or other vendors.

## Usage

```csharp
services.AddPostgresGeolocationStoreFromConfiguration(configuration);
```

Import flow (in your worker/API — not in this package):

1. Call a vendor client (e.g. Google Maps) in a separate integration assembly.
2. Map the response to `Address` and `EntitySourceRecord` rows.
3. `await store.SaveAddressAsync(address, ct)` or `GetBySourceAsync` for idempotent ingest.

## Migrations

Schema: `geolocation`. Design-time: `GEOLOCATION_CONNECTION_STRING` env var.

## See also

[`Lyo.Geolocation`](../Lyo.Geolocation/README.md)
