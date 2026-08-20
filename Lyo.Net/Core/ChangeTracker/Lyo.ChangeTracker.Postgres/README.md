# Lyo.ChangeTracker.Postgres

PostgreSQL implementation of `Lyo.ChangeTracker`. Persists entity-scoped change history using `Lyo.EntityReference.Models.EntityRef` for both the target entity and the optional actor.

## Features

- `PostgresChangeTracker` implementing both `IChangeTracker` and `Lyo.Health.IHealth`
- EF Core migrations in the `change_tracker` schema (`PostgresChangeTrackerOptions.Schema`)
- Queryable history by entity, entity type, or change id (newest first)
- `DeleteForEntityAsync` to drop all history for a target entity
- Optional automatic migrations on startup via `EnableAutoMigrations`

## Examples

### Quick start

```csharp
services.AddPostgresChangeTracker(new PostgresChangeTrackerOptions {
    ConnectionString = configuration.GetConnectionString("ChangeTracker")!,
    EnableAutoMigrations = true
});

services.AddPostgresChangeTrackerFromConfiguration(configuration);
```

### Migrations

```bash
export CHANGE_TRACKER_CONNECTION_STRING="Host=localhost;Database=change_tracker;Username=postgres;Password=postgres"
dotnet ef migrations add MigrationName --project Core/ChangeTracker/Lyo.ChangeTracker.Postgres
```

## Registration

Registration helpers, from factory-only to `IChangeTracker`:

| Extension | What it adds |
| ---------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `AddChangeTrackerDbContextFactory(options)` / `(Action<…>)` | Registers `IDbContextFactory<ChangeTrackerDbContext>` and the migrations helper (`AddPostgresMigrations`). |
| `AddChangeTrackerDbContextFactoryFromConfiguration(IConfiguration, …)` | Binds `PostgresChangeTrackerOptions` from `"PostgresChangeTracker"` (override the section via the optional `configSectionName`) and registers as above. |
| `AddChangeTrackerDbContext(connectionString)` | Builds options from a raw connection string, registers the factory, and exposes a scoped `ChangeTrackerDbContext`. |
| `AddChangeTrackerDbContext(Action<DbContextOptionsBuilder>)` | Uses a caller-provided `DbContextOptionsBuilder` (useful for tests or shared connection multiplexing). |
| `AddPostgresChangeTracker(options)` / `(Action<…>)` | Calls `AddChangeTrackerDbContextFactory` and registers `IChangeTracker` → `PostgresChangeTracker` as a singleton. |
| `AddPostgresChangeTrackerFromConfiguration(IConfiguration, …)` | Same as the options overload, binding from configuration. |

## Health

`PostgresChangeTracker` implements `Lyo.Health.IHealth` with `HealthCheckName = "change-tracker-postgres"`. The probe opens a `ChangeTrackerDbContext` and runs `Database.CanConnectAsync`. The `HealthResult` data bag includes the schema name. Hosts that resolve `IEnumerable<IHealth>` include this tracker.

## Schema

- All tables live in the `change_tracker` schema (see `PostgresChangeTrackerOptions.Schema`).
- `change_tracker.changes`. Subject (`for_entity_*` / `SubjectEntityType`), optional actor (`from_entity_*` / `ActorEntityType`), nullable `tenant_id` (uuid), JSON `OldValues`, JSON `ChangedProperties`, optional `ChangeType` / `Message`, and `Timestamp`.

## Tenancy

`ChangeRecord` carries an optional `TenantId`; `null` denotes a system / untenanted row. `PostgresChangeTracker` runs every record through `TenancyResolver.Resolve` using the
policy configured in `PostgresChangeTrackerOptions.Tenancy` (inheriting from `EntityRefOptions.Mode` when unset) before persisting:

- `SystemOnly`. Caller `TenantId` is ignored. The row stores `null`.
- `SingleTenantDefault` *(default)*. Caller value, falling back to `Tenancy.DefaultTenantId` then `EntityRefOptions.DefaultTenantId`.
- `MultiTenantStrict`. Caller must supply a non-empty `TenantId` or persistence throws.

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

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.ChangeTracker` (direct, lyo)
- `Lyo.EntityReference.Models` (direct, lyo)
- `Lyo.EntityReference.Postgres` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Health` (direct, lyo)
- `Lyo.Postgres` (direct, lyo)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Lyo.Common` (transitive, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)