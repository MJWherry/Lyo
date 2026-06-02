# Lyo.Audit.Postgres

PostgreSQL implementation of Lyo.Audit using Entity Framework Core. Persists `AuditChange` and `AuditEvent` records to PostgreSQL with JSONB columns for dictionary data.

## Features

- **PostgresAuditRecorder** – Implements `IAuditRecorder` and `Lyo.Health.IHealth` with PostgreSQL persistence
- **Migrations** – EF Core migrations in the `audit` schema (`PostgresAuditOptions.Schema`) with `audit_changes` and `audit_events` tables
- **Auto migrations** – Optional automatic migration on startup via `EnableAutoMigrations`

## Registration

| Extension                                                      | What it adds                                                                                                                        |
|----------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------|
| `AddAuditDbContextFactory(options)` / `(Action<…>)`            | Registers `IDbContextFactory<AuditDbContext>` and the migrations helper (`AddPostgresMigrations`).                                  |
| `AddAuditDbContextFactoryFromConfiguration(IConfiguration, …)` | Binds `PostgresAuditOptions` from `"PostgresAudit"` (override section via the optional `configSectionName`) and registers as above. |
| `AddAuditDbContext(connectionString)`                          | Builds options from a raw connection string, registers the factory, and exposes a scoped `AuditDbContext`.                          |
| `AddAuditDbContext(Action<DbContextOptionsBuilder>)`           | Uses a caller-provided `DbContextOptionsBuilder` (useful for tests or shared connection multiplexing).                              |
| `AddPostgresAuditRecorder(options)` / `(Action<…>)`            | Calls `AddAuditDbContextFactory` and registers `IAuditRecorder` → `PostgresAuditRecorder` as a singleton.                           |
| `AddPostgresAuditRecorderFromConfiguration(IConfiguration, …)` | Same as the options overload, binding from configuration.                                                                           |

## Quick Start

```csharp
services.AddPostgresAuditRecorder(new PostgresAuditOptions {
    ConnectionString = configuration.GetConnectionString("Audit")!,
    EnableAutoMigrations = true
});

services.AddPostgresAuditRecorderFromConfiguration(configuration);
```

Need just the factory (e.g. to share with downstream services or run migrations explicitly)?

```csharp
services.AddAuditDbContextFactory(configuration.GetSection("PostgresAudit").Get<PostgresAuditOptions>()!);
```

## Health

`PostgresAuditRecorder` implements `Lyo.Health.IHealth` with `HealthCheckName = "audit-postgres"`. The probe opens an `AuditDbContext` and runs
`Database.CanConnectAsync`, surfacing connection failures through `HealthResult.Unhealthy` so the recorder contributes to host health endpoints that resolve
`IEnumerable<IHealth>`.

## Migrations

Design-time migrations require `AUDIT_CONNECTION_STRING` environment variable:

```bash
export AUDIT_CONNECTION_STRING="Host=localhost;Database=audit;Username=postgres;Password=postgres"
dotnet ef migrations add MigrationName --project Core/Audit/Lyo.Audit.Postgres
```

## Schema

Both tables use **`EntityRelationOptionalActorBase`** from `Lyo.EntityReference.Postgres`:

- **audit.audit_changes** – `id` (uuid), `timestamp`, `for_entity_type`, `for_entity_id` (varchar), `from_entity_type?`, `from_entity_id?` (varchar), `tenant_id?` (uuid),
  `old_values_json` (jsonb),
  `changed_properties_json` (jsonb), `created_timestamp`, `updated_timestamp?`
- **audit.audit_events** – `id` (uuid), `event_type`, `timestamp`, `for_entity_type`, `for_entity_id` (varchar), `from_entity_type?`, `from_entity_id?` (varchar), `tenant_id?` (
  uuid), `message?`,
  `metadata_json?` (jsonb), `created_timestamp`, `updated_timestamp?`

`for_entity_*` columns hold the subject of the change/event; `from_entity_*` columns hold the optional actor that caused it.

## Tenancy

`AuditChange` and `AuditEvent` records carry an optional `TenantId` (mapped to the `tenant_id` column). `PostgresAuditRecorder` runs every record through
`TenancyResolver.Resolve` using the policy configured in `PostgresAuditOptions.Tenancy` (inheriting from `EntityRefOptions.Mode` when unset) before persisting:

- `SystemOnly` — caller `TenantId` is ignored and the row stores `null` (system-level audit).
- `SingleTenantDefault` *(default)* — caller value, falling back to `Tenancy.DefaultTenantId` then `EntityRefOptions.DefaultTenantId`.
- `MultiTenantStrict` — caller must supply a non-empty `TenantId` or persistence throws.

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

*(Synchronized from `Lyo.Audit.Postgres.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package                                     | Version |
|---------------------------------------------|---------|
| `Microsoft.Extensions.Configuration.Binder` | `[10,)` |

### Project references

- [`Lyo.Audit`](../Lyo.Audit/README.md)
- [`Lyo.EntityReference.Postgres`](../../EntityReference/Lyo.EntityReference.Postgres/README.md)
- [`Lyo.Exceptions`](../../Lyo.Exceptions/README.md)
- [`Lyo.Health`](../../Health/Lyo.Health/README.md)
- [`Lyo.Postgres`](../../../Data/Postgres/Lyo.Postgres/README.md)