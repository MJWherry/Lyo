# Lyo.Geolocation.Postgres

PostgreSQL persistence for canonical geolocation data using Entity Framework Core.

**Archetype A (Lyo domain).** Vendor clients such as [`Lyo.Google.Geolocation.Client`](../../../Integration/Google/Lyo.Google.Geolocation.Client/README.md) persist via
`IGeolocationStore` in the host. See [package layout](../../../docs/package-layout.md).

## Examples

### Usage

```csharp
services.AddPostgresGeolocationStoreFromConfiguration(configuration);
```

## Overview

Schema **`geolocation`**: - **address** — Canonical normalized addresses (`Lyo.Geolocation.Models.Address`) - **address_source** — provenance per import (`source_entity_*` +
`imported_at`; owner `address_id`; lookup index on `source_entity_*`) There is **no** `geocode_cache` table. This project references only **`Lyo.Geolocation`**, **
`Lyo.Geolocation.Models`**, and **`Lyo.EntityReference.Postgres`** — not Google, Endato, or other vendors.

## Usage

- Call a vendor client (e.g. Google Maps) in a separate integration assembly.
- Map to `Address` + `EntitySourceRecord.From(externalRef, importedAt)` on **`Sources`** (owner id set on save).
- `await store.SaveAddressAsync(address, ct)` or `GetBySourceAsync` (queries **`source_entity_*`**) for idempotent ingest.

## Migrations

Schema: `geolocation`. Design-time: `GEOLOCATION_CONNECTION_STRING` env var.

## See also

[`Lyo.Geolocation`](../Lyo.Geolocation/README.md)

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.EntityReference.Postgres` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Geolocation` — (direct, lyo)
- `Lyo.Geolocation.Models` — (direct, lyo)
- `Lyo.Health` — (direct, lyo)
- `Lyo.Postgres` — (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` — (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (direct, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` — (direct, third-party)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.EntityReference.Models` — (transitive, lyo)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)