# Lyo.Audit.Postgres

EF Core persistence for Lyo.Audit in PostgreSQL. Stores `AuditChange` and `AuditEvent` rows with JSONB columns for dictionary data.

## Features

- **PostgresAuditRecorder.** Implements `IAuditRecorder` and `Lyo.Health.IHealth` against PostgreSQL.
- **Migrations.** EF Core migrations in the `audit` schema (`PostgresAuditOptions.Schema`) with `audit_events` and `audit_changes` tables.
- **Auto migrations.** Optional startup migration when `EnableAutoMigrations` is on.

## Examples

### First steps

```csharp
services.AddPostgresAuditRecorder(new PostgresAuditOptions {
    ConnectionString = configuration.GetConnectionString("Audit")!,
    EnableAutoMigrations = true
});

services.AddPostgresAuditRecorderFromConfiguration(configuration);
```

### First steps (2)

```csharp
services.AddAuditDbContextFactory(configuration.GetSection("PostgresAudit").Get<PostgresAuditOptions>()!);
```

### Migrations

```bash
export AUDIT_CONNECTION_STRING="Host=localhost;Database=audit;Username=postgres;Password=postgres"
dotnet ef migrations add MigrationName --project Core/Audit/Lyo.Audit.Postgres
```

## Registration

| Extension | What it adds |
| -------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------- |
| `AddAuditDbContextFactory(options)` / `(Action<…>)` | Registers `IDbContextFactory<AuditDbContext>` and the migrations helper (`AddPostgresMigrations`). |
| `AddAuditDbContextFactoryFromConfiguration(IConfiguration, …)` | Binds `PostgresAuditOptions` from `"PostgresAudit"` (override section via the optional `configSectionName`) and registers as above. |
| `AddAuditDbContext(connectionString)` | Builds options from a raw connection string, registers the factory, and exposes a scoped `AuditDbContext`. |
| `AddPostgresAuditRecorder(options)` / `(Action<…>)` | Calls `AddAuditDbContextFactory` and registers `IAuditRecorder` → `PostgresAuditRecorder` as a singleton. |
| `AddPostgresAuditRecorderFromConfiguration(IConfiguration, …)` | Same as the options overload, binding from configuration. |

## First steps

Need just the factory, for example to share with downstream services or run migrations yourself?

## Health

`HealthCheckName = "audit-postgres"` comes from `PostgresAuditRecorder` implementing `Lyo.Health.IHealth`. The probe constructs an `AuditDbContext` and calls `Database.CanConnectAsync`. A failed connect is `HealthResult.Unhealthy`. Hosts resolving `IEnumerable<IHealth>` pick this recorder up.

## Database schema

- **audit.audit_changes.** `id` (uuid), `timestamp`, `for_entity_type`, `for_entity_id` (varchar), `from_entity_type?`, `from_entity_id?` (varchar), `tenant_id?` (uuid), `old_values_json` (jsonb), `changed_properties_json` (jsonb), `created_timestamp`, `updated_timestamp?`
- **audit.audit_events.** `id` (uuid), `event_type`, `timestamp`, `for_entity_type`, `for_entity_id` (varchar), `from_entity_type?`, `from_entity_id?` (varchar), `tenant_id?` (uuid), `message?`, `metadata_json?` (jsonb), `created_timestamp`, `updated_timestamp?`

## Tenancy

`AuditEvent` and `AuditChange` records carry an optional `TenantId` (mapped to the `tenant_id` column). Before persist, `PostgresAuditRecorder` runs every record through
`TenancyResolver.Resolve` using the policy configured in `PostgresAuditOptions.Tenancy` (inheriting from `EntityRefOptions.Mode` when unset):

- `SystemOnly`. Caller `TenantId` is ignored and the row stores `null` (system-level audit).
- `SingleTenantDefault` *(default)*. Caller value, falling back to `Tenancy.DefaultTenantId` then `EntityRefOptions.DefaultTenantId`.
- `MultiTenantStrict`. Caller must supply a non-empty `TenantId` or persistence throws.

Each table has an `ix_<table>_tenant` index for filtered scans. Use `WhereTenant(tenantId)` to scope queries to a single tenant (or `null` for system rows) and
`WhereTenantOrSystem(tenantId)` to include both. See [`Lyo.EntityReference.Postgres`](../../EntityReference/Lyo.EntityReference.Postgres/README.md#tenancy) for the policy matrix.

```json
{
  "PostgresAudit": {
    "ConnectionString": "Host=localhost;Database=audit;...",
    "Tenancy": { "Mode": "MultiTenantStrict" }
  }
}
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Audit` (direct, lyo)
- `Lyo.Configuration` (direct, lyo)
- `Lyo.EntityReference.Postgres` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Health` (direct, lyo)
- `Lyo.Postgres` (direct, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.EntityReference.Models` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)