# Lyo.People.Postgres

PostgreSQL persistence for Lyo.People.Models using Entity Framework Core.

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

## Usage

### Configuration

```csharp
// From connection string
services.AddPeopleDbContext("Host=localhost;Database=lyo;Username=postgres;Password=...");

// From configuration (PostgresPeople section)
services.AddPeopleDbContextFactory(configuration);

// With options
services.AddPeopleDbContextFactory(opts => {
    opts.ConnectionString = "...";
    opts.EnableAutoMigrations = true;
});
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

For design-time operations (e.g. adding migrations), set the `PEOPLE_CONNECTION_STRING` environment variable:

```bash
export PEOPLE_CONNECTION_STRING="Host=localhost;Database=lyo_people;Username=postgres;Password=postgres"
dotnet ef migrations add MigrationName --project Core/People/Lyo.People.Postgres --context PeopleDbContext
```

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.People.Postgres.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `Microsoft.EntityFrameworkCore.Design` | `[10,)` |
| `Microsoft.Extensions.Configuration.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder` | `[10,)` |

### Project references

- `Lyo.Exceptions`
- `Lyo.People.Models`
- `Lyo.Postgres`

## Public API (generated)

Top-level `public` types in `*.cs` (*27*). Nested types and file-scoped namespaces may omit some entries.

- `AddressEntity`
- `AddressEntityConfiguration`
- `ContactAddressEntity`
- `ContactAddressEntityConfiguration`
- `ContactEmailAddressEntity`
- `ContactEmailAddressEntityConfiguration`
- `ContactPhoneNumberEntity`
- `ContactPhoneNumberEntityConfiguration`
- `EmailAddressEntity`
- `EmailAddressEntityConfiguration`
- `EmploymentEntity`
- `EmploymentEntityConfiguration`
- `Extensions`
- `IdentificationEntity`
- `IdentificationEntityConfiguration`
- `InitialCreate`
- `PeopleDbContext`
- `PeopleDbContextFactory`
- `PersonEntity`
- `PersonEntityConfiguration`
- `PersonRelationshipEntity`
- `PersonRelationshipEntityConfiguration`
- `PhoneNumberEntity`
- `PhoneNumberEntityConfiguration`
- `PostgresPeopleOptions`
- `SocialMediaProfileEntity`
- `SocialMediaProfileEntityConfiguration`

<!-- LYO_README_SYNC:END -->

