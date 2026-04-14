# Lyo.ChangeTracker.Postgres

PostgreSQL implementation of `Lyo.ChangeTracker`. Persists entity-scoped change history using `Lyo.Common.EntityRef` for both the target entity and the optional actor.

## Features

- `PostgresChangeTracker` implementing `IChangeTracker`
- EF Core migrations in the `change_tracker` schema
- Queryable history by entity or entity type
- Optional automatic migrations on startup

## Quick Start

```csharp
services.AddPostgresChangeTracker(new PostgresChangeTrackerOptions {
    ConnectionString = configuration.GetConnectionString("ChangeTracker")!,
    EnableAutoMigrations = true
});
```

## Migrations

Design-time migrations require `CHANGE_TRACKER_CONNECTION_STRING`:

```bash
export CHANGE_TRACKER_CONNECTION_STRING="Host=localhost;Database=change_tracker;Username=postgres;Password=postgres"
dotnet ef migrations add MigrationName --project Core/ChangeTracker/Lyo.ChangeTracker.Postgres
```

## Schema

- `change_tracker.changes` stores target `EntityRef`, optional actor `EntityRef`, JSON old values, JSON changed values, and timestamps

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.ChangeTracker.Postgres.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `Microsoft.Extensions.Configuration.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder` | `[10,)` |

### Project references

- `Lyo.ChangeTracker`
- `Lyo.Exceptions`
- `Lyo.Health`
- `Lyo.Postgres`

## Public API (generated)

Top-level `public` types in `*.cs` (*8*). Nested types and file-scoped namespaces may omit some entries.

- `ChangeEntryEntity`
- `ChangeEntryEntityConfiguration`
- `ChangeTrackerDbContext`
- `ChangeTrackerDbContextFactory`
- `Extensions`
- `InitialCreate`
- `PostgresChangeTracker`
- `PostgresChangeTrackerOptions`

<!-- LYO_README_SYNC:END -->

