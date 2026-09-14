# Lyo.Geolocation.Postgres

Entity Framework Core persistence of canonical geolocation data in PostgreSQL.

**Archetype A (Lyo domain).** Vendor clients such as [`Lyo.Google.Geolocation.Client`](../../../Integration/Google/Lyo.Google.Geolocation.Client/README.md) persist via `IGeolocationStore` in the host. See [package layout](../../../docs/package-layout.md).

## Examples

### How to use it

```csharp
services.AddPostgresGeolocationStoreFromConfiguration(configuration);
```

## Overview

`address` (canonical normalized addresses from `Lyo.Geolocation.Models.Address`) and `address_source` (provenance per import: `source_entity_*` + `imported_at`, owner `address_id`, lookup index on `source_entity_*`) live in schema `geolocation`. There is no `geocode_cache` table. References stop at `Lyo.Geolocation`, `Lyo.Geolocation.Models`, and `Lyo.EntityReference.Postgres`. Not Google, Endato, or other vendors.

## How to use it

- Call a vendor client (e.g. Google Maps) from a separate integration assembly.
- Project onto `Address` plus `EntitySourceRecord.From(externalRef, importedAt)` on `Sources`. Save assigns the owner id.
- Idempotent ingest uses `await store.SaveAddressAsync(address, ct)` or `GetBySourceAsync` (queries `source_entity_*`).

## Migrations

Schema: `geolocation`. Design-time connection: `GEOLOCATION_CONNECTION_STRING`.

## Related reading

[`Lyo.Geolocation`](../Lyo.Geolocation/README.md)

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.EntityReference.Postgres` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Geolocation` (direct, lyo)
- `Lyo.Geolocation.Models` (direct, lyo)
- `Lyo.Health` (direct, lyo)
- `Lyo.Postgres` (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (direct, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (direct, third-party)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.EntityReference.Models` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)