# Lyo.People.Postgres

PostgreSQL persistence for Lyo.People.Models using Entity Framework Core.

**Archetype A (Lyo domain).** Vendor clients such as [`Lyo.Endato.Client`](../../../Integration/Endato/Lyo.Endato.Client/README.md) map into this schema in the host.
See [package layout](../../../docs/package-layout.md).

## Overview

This package provides Entity Framework Core entities and DbContext for storing person data in PostgreSQL. It uses the `people` schema and includes tables for:

- **person** — Core person data with flattened name, demographics, and JSON columns for preferences/citizenship/custom fields
- **phone_number** — Base phone numbers (E.164)
- **email_address** — Base email addresses
- **contact_phone_number** — Person–phone junction with type (mobile, home, work)
- **contact_email_address** — Person–email junction with type (personal, work)
- **social_media_profile** — Social platform profiles
- **address** — Addresses (simplified for people schema)
- **contact_address** — Person–address junction with type (home, work, billing)
- **identification** — ID documents (passport, driver's license, SSN, etc.)
- **person_relationship** — Relationships between people
- **employment** — Employment history
- **person_source**, **address_source**, **phone_number_source**, **email_address_source** — import provenance via **`source_entity_*`** + **`imported_at`** (see [`PeopleSourceTypes`](../Lyo.People.Models/PeopleSourceTypes.cs))
- **contact_address_source**, **contact_phone_number_source**, **contact_email_address_source** — optional junction-level provenance (same shape)

The legacy **`person.source`** varchar column is removed; use **`person_source`** instead. Domain: **`EntitySourceRecord.From(source, importedAt)`** on **`IEntitySourceDerived`** aggregates; lookup by external key uses **`source_entity_*`** (e.g. `GetPersonBySourceAsync`).

## `IPeopleStore`

[`PostgresPeopleStore`](PostgresPeopleStore.cs) implements save/load with source rows:

```csharp
services.AddPeopleDbContextFactoryFromConfiguration(configuration);
services.AddPostgresPeopleStore();
```

## Usage

### Registration

Everything ships as extension methods on `IServiceCollection`; no people-domain service is registered — consumers resolve `PeopleDbContext` (scoped) or
`IDbContextFactory<PeopleDbContext>` (singleton) and write their own repositories.

```csharp
// Connection string overload: registers IDbContextFactory<PeopleDbContext>
// AND a scoped PeopleDbContext via factory.CreateDbContext()
services.AddPeopleDbContext("Host=localhost;Database=lyo;Username=postgres;Password=...");

// DbContextOptionsBuilder overload: classic AddDbContext registration
services.AddPeopleDbContext(opts => opts.UseNpgsql(connectionString));

// Options-action overload (registers IDbContextFactory<PeopleDbContext> only)
services.AddPeopleDbContextFactory(opts => {
    opts.ConnectionString = "...";
    opts.EnableAutoMigrations = true;
});

// Pre-built options overload
services.AddPeopleDbContextFactory(new PostgresPeopleOptions { ConnectionString = "..." });

// IConfiguration binding (defaults to the "PostgresPeople" section)
services.AddPeopleDbContextFactoryFromConfiguration(configuration);
services.AddPeopleDbContextFactoryFromConfiguration(configuration, configSectionName: "MyPeopleSection");
```

The factory overloads also call `services.AddPostgresMigrations<PeopleDbContext, PostgresPeopleOptions>()` from `Lyo.Postgres`, which honours
`PostgresPeopleOptions.EnableAutoMigrations` and stamps the `__EFMigrationsHistory` table inside the `people` schema.

### Configuration section

```json
{
  "PostgresPeople": {
    "ConnectionString": "Host=localhost;Database=lyo;Username=postgres;Password=...",
    "EnableAutoMigrations": false
  }
}
```

The section name and schema are exposed as constants: `PostgresPeopleOptions.SectionName` (`"PostgresPeople"`) and `PostgresPeopleOptions.Schema` (`"people"`).

### Migrations

For design-time operations (e.g. adding migrations), set the `PEOPLE_CONNECTION_STRING` environment variable:

```bash
export PEOPLE_CONNECTION_STRING="Host=localhost;Database=lyo_people;Username=postgres;Password=postgres"
dotnet ef migrations add MigrationName --project Core/People/Lyo.People.Postgres --context PeopleDbContext
```

## Dependencies

*(Synchronized from `Lyo.People.Postgres.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package                                     | Version |
|---------------------------------------------|---------|
| `Microsoft.EntityFrameworkCore.Design`      | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder` | `[10,)` |

### Project references

- [`Lyo.Exceptions`](../../Lyo.Exceptions/README.md)
- [`Lyo.People.Models`](../Lyo.People.Models/README.md)
- [`Lyo.Postgres`](../../../Data/Postgres/Lyo.Postgres/README.md)