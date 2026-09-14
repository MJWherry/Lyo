# Lyo.People.Postgres

EF Core persistence of Lyo.People.Models in PostgreSQL.

**Archetype A (Lyo domain).** Vendor clients such as [`Lyo.Endato.Client`](../../../Integration/Endato/Lyo.Endato.Client/README.md) map into this schema in the host. See [package layout](../../../docs/package-layout.md).

## Features

- **person.** Flattened name, core person data, demographics, and JSON columns for preferences/citizenship/custom fields.
- **phone_number.** Base phone numbers (E.164).
- **email_address.** Base email addresses.
- **contact_phone_number.** Person-phone junction with type (home, mobile, work).
- **contact_email_address.** Person-email junction with type (work, personal).
- **social_media_profile.** Profiles on social platforms.
- **address.** Addresses (people-schema subset).
- **contact_address.** Person-address junction with type (work, home, billing).
- **identification.** ID documents (driver's license, passport, SSN, and similar).
- **person_relationship.** Links between people.
- **employment.** Employment history.
- **person_source, address_source, phone_number_source, email_address_source.** Import provenance via `source_entity_*` plus `imported_at` (see [`PeopleSourceTypes`](../Lyo.People.Models/PeopleSourceTypes.cs)).
- **contact_address_source, contact_phone_number_source, contact_email_address_source.** Optional junction-level provenance (same shape).

## Examples

### Register in DI

```csharp
// Connection string overload: registers IDbContextFactory<PeopleDbContext>
// AND a scoped PeopleDbContext via factory.CreateDbContext()
services.AddPeopleDbContext("Host=localhost;Database=lyo;Username=postgres;Password=...");

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

### Config section

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

- **person.** Flattened name, core person data, demographics, and JSON columns for preferences/citizenship/custom fields.
- **phone_number.** Base phone numbers (E.164).
- **email_address.** Base email addresses.
- **contact_phone_number.** Person-phone junction with type (home, mobile, work).
- **contact_email_address.** Person-email junction with type (work, personal).
- **social_media_profile.** Profiles on social platforms.
- **address.** Addresses (people-schema subset).
- **contact_address.** Person-address junction with type (work, home, billing).
- **identification.** ID documents (driver's license, passport, SSN, and similar).
- **person_relationship.** Links between people.
- **employment.** Employment history.
- **person_source, address_source, phone_number_source, email_address_source.** Import provenance via `source_entity_*` plus `imported_at` (see [`PeopleSourceTypes`](../Lyo.People.Models/PeopleSourceTypes.cs)).
- **contact_address_source, contact_phone_number_source, contact_email_address_source.** Optional junction-level provenance (same shape).

## `IPeopleStore`

Save/load with source rows is implemented by [`PostgresPeopleStore`](PostgresPeopleStore.cs):

```csharp
services.AddPeopleDbContextFactoryFromConfiguration(configuration);
services.AddPostgresPeopleStore();
```

## Registration

Everything is an `IServiceCollection` extension. No people-domain service is registered. Consumers resolve `PeopleDbContext` (scoped) or `IDbContextFactory<PeopleDbContext>` (singleton) and write their own repositories. The factory overloads also call `services.AddPostgresMigrations<PeopleDbContext, PostgresPeopleOptions>()` from `Lyo.Postgres`, which honours `PostgresPeopleOptions.EnableAutoMigrations` and stamps the `__EFMigrationsHistory` table inside the `people` schema.

## Config section

Constants expose the section name and schema: `PostgresPeopleOptions.SectionName` (`"PostgresPeople"`) and `PostgresPeopleOptions.Schema` (`"people"`).

## Migrations

For design-time work (e.g. adding migrations), set `PEOPLE_CONNECTION_STRING`:

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Metadata` (direct, lyo)
- `Lyo.EntityReference.Postgres` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.People.Models` (direct, lyo)
- `Lyo.Postgres` (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.EntityReference.Models` (transitive, lyo)
- `Lyo.Geolocation.Models` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)