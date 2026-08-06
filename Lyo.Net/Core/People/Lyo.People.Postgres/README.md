# Lyo.People.Postgres

PostgreSQL persistence for Lyo.People.Models using Entity Framework Core.

**Archetype A (Lyo domain).** Vendor clients such as [`Lyo.Endato.Client`](../../../Integration/Endato/Lyo.Endato.Client/README.md) map into this schema in the host. See [package layout](../../../docs/package-layout.md).

## Features

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
- **person_source**, **address_source**, **phone_number_source**, **email_address_source** — import provenance via **`source_entity_*`** + **`imported_at`** (see [ `PeopleSourceTypes`](../Lyo.People.Models/PeopleSourceTypes.cs))
- **contact_address_source**, **contact_phone_number_source**, **contact_email_address_source** — optional junction-level provenance (same shape)

## Examples

### Register services

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

### Configuration section

```json
{
  "PostgresPeople": {
    "ConnectionString": "Host=localhost;Database=lyo;Username=postgres;Password=...",
    "EnableAutoMigrations": false
  }
}
```

### Migrations

```bash
export PEOPLE_CONNECTION_STRING="Host=localhost;Database=lyo_people;Username=postgres;Password=postgres"
dotnet ef migrations add MigrationName --project Core/People/Lyo.People.Postgres --context PeopleDbContext
```

## Overview

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
- **person_source**, **address_source**, **phone_number_source**, **email_address_source** — import provenance via **`source_entity_*`** + **`imported_at`** (see [ `PeopleSourceTypes`](../Lyo.People.Models/PeopleSourceTypes.cs))
- **contact_address_source**, **contact_phone_number_source**, **contact_email_address_source** — optional junction-level provenance (same shape)

## `IPeopleStore`

[`PostgresPeopleStore`](PostgresPeopleStore.cs) implements save/load with source rows:

```csharp
services.AddPeopleDbContextFactoryFromConfiguration(configuration);
services.AddPostgresPeopleStore();
```

## Registration

Everything ships as extension methods on `IServiceCollection`; no people-domain service is registered — consumers resolve `PeopleDbContext` (scoped) or `IDbContextFactory<PeopleDbContext>` (singleton) and write their own repositories. The factory overloads also call `services.AddPostgresMigrations<PeopleDbContext, PostgresPeopleOptions>()` from `Lyo.Postgres`, which honours `PostgresPeopleOptions.EnableAutoMigrations` and stamps the `__EFMigrationsHistory` table inside the `people` schema.

## Configuration section

The section name and schema are exposed as constants: `PostgresPeopleOptions.SectionName` (`"PostgresPeople"`) and `PostgresPeopleOptions.Schema` (`"people"`).

## Migrations

For design-time operations (e.g. adding migrations), set the `PEOPLE_CONNECTION_STRING` environment variable:

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.EntityReference.Postgres` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.People.Models` — (direct, lyo)
- `Lyo.Postgres` — (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` — (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (direct, microsoft)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.DateAndTime` — (transitive, lyo)
- `Lyo.EntityReference.Models` — (transitive, lyo)
- `Lyo.Geolocation.Models` — (transitive, lyo)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` — (transitive, third-party)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)