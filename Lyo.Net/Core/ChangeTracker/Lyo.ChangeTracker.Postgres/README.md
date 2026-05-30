# Lyo.ChangeTracker.Postgres

PostgreSQL implementation of `Lyo.ChangeTracker`. Persists entity-scoped change history using `Lyo.EntityReference.Models.EntityRef` for both the target entity and the optional
actor.

## Features

- `PostgresChangeTracker` implementing both `IChangeTracker` and `Lyo.Health.IHealth`
- EF Core migrations in the `change_tracker` schema (`PostgresChangeTrackerOptions.Schema`)
- Queryable history by entity, entity type, or change id (newest first)
- `DeleteForEntityAsync` to drop all history for a target entity
- Optional automatic migrations on startup via `EnableAutoMigrations`

## Registration

The package layers registration helpers so hosts can pick the level of integration they need:

| Extension                                                              | What it adds                                                                                                                                            |
|------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------|
| `AddChangeTrackerDbContextFactory(options)` / `(Action<…>)`            | Registers `IDbContextFactory<ChangeTrackerDbContext>` and the migrations helper (`AddPostgresMigrations`).                                              |
| `AddChangeTrackerDbContextFactoryFromConfiguration(IConfiguration, …)` | Binds `PostgresChangeTrackerOptions` from `"PostgresChangeTracker"` (override the section via the optional `configSectionName`) and registers as above. |
| `AddChangeTrackerDbContext(connectionString)`                          | Builds options from a raw connection string, registers the factory, and exposes a scoped `ChangeTrackerDbContext`.                                      |
| `AddChangeTrackerDbContext(Action<DbContextOptionsBuilder>)`           | Uses a caller-provided `DbContextOptionsBuilder` (useful for tests or shared connection multiplexing).                                                  |
| `AddPostgresChangeTracker(options)` / `(Action<…>)`                    | Calls `AddChangeTrackerDbContextFactory` and registers `IChangeTracker` → `PostgresChangeTracker` as a singleton.                                       |
| `AddPostgresChangeTrackerFromConfiguration(IConfiguration, …)`         | Same as the options overload, binding from configuration.                                                                                               |

## Quick Start

```csharp
services.AddPostgresChangeTracker(new PostgresChangeTrackerOptions {
    ConnectionString = configuration.GetConnectionString("ChangeTracker")!,
    EnableAutoMigrations = true
});

services.AddPostgresChangeTrackerFromConfiguration(configuration);
```

## Health

`PostgresChangeTracker` implements `Lyo.Health.IHealth` with `HealthCheckName = "change-tracker-postgres"`. The probe opens a `ChangeTrackerDbContext` and runs
`Database.CanConnectAsync`, returning a `HealthResult` with the schema name in its data bag — so the tracker contributes to host health endpoints that resolve
`IEnumerable<IHealth>`.

## Migrations

Design-time migrations require `CHANGE_TRACKER_CONNECTION_STRING`:

```bash
export CHANGE_TRACKER_CONNECTION_STRING="Host=localhost;Database=change_tracker;Username=postgres;Password=postgres"
dotnet ef migrations add MigrationName --project Core/ChangeTracker/Lyo.ChangeTracker.Postgres
```

## Schema

- All tables live in the `change_tracker` schema (see `PostgresChangeTrackerOptions.Schema`).
- `change_tracker.changes` — subject (`for_entity_*` / `SubjectEntityType`), optional actor (`from_entity_*` / `ActorEntityType`), nullable `tenant_id`
  (uuid), JSON `OldValues`, JSON `ChangedProperties`, optional `ChangeType` / `Message`, and `Timestamp`.

## Tenancy

`ChangeRecord` carries an optional `TenantId`; `null` denotes a system / untenanted row. `PostgresChangeTracker` runs every record through `TenancyResolver.Resolve` using the
policy configured in `PostgresChangeTrackerOptions.Tenancy` (inheriting from `EntityRefOptions.Mode` when unset) before persisting:

- `SystemOnly` — caller `TenantId` is ignored; the row stores `null`.
- `SingleTenantDefault` *(default)* — caller value, falling back to `Tenancy.DefaultTenantId` then `EntityRefOptions.DefaultTenantId`.
- `MultiTenantStrict` — caller must supply a non-empty `TenantId` or persistence throws.

The `ix_changes_tenant` index supports per-tenant lookups. Use the
`WhereTenant` / `WhereTenantOrSystem` helpers from [`Lyo.EntityReference.Postgres`](../../EntityReference/Lyo.EntityReference.Postgres/README.md#tenancy) to query the table.

```json
{
  "PostgresChangeTracker": {
    "ConnectionString": "Host=localhost;Database=change_tracker;...",
    "Tenancy": { "Mode": "SystemOnly" }
  }
}
```

## Dependencies

*(Synchronized from `Lyo.ChangeTracker.Postgres.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package                                     | Version |
|---------------------------------------------|---------|
| `Microsoft.Extensions.Configuration.Binder` | `[10,)` |

### Project references

- [`Lyo.ChangeTracker`](../Lyo.ChangeTracker/README.md)
- [`Lyo.EntityReference.Models`](../../EntityReference/Lyo.EntityReference.Models/README.md)
- [`Lyo.EntityReference.Postgres`](../../EntityReference/Lyo.EntityReference.Postgres/README.md)
- [`Lyo.Exceptions`](../../Lyo.Exceptions/README.md)
- [`Lyo.Health`](../../Health/Lyo.Health/README.md)
- [`Lyo.Postgres`](../../../Data/Postgres/Lyo.Postgres/README.md)