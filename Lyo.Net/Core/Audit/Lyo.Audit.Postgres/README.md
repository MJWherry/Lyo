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

- **audit.audit_changes** – `id` (uuid), `timestamp`, `type_assembly_full_name`, `old_values_json` (jsonb), `changed_properties_json` (jsonb)
- **audit.audit_events** – `id` (uuid), `event_type`, `timestamp`, `message`, `actor`, `metadata_json` (jsonb)

## Dependencies

*(Synchronized from `Lyo.Audit.Postgres.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package                                           | Version |
|---------------------------------------------------|---------|
| `Microsoft.Extensions.Configuration.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder`       | `[10,)` |

### Project references

- [`Lyo.Audit`](../Lyo.Audit/README.md)
- [`Lyo.Exceptions`](../../Lyo.Exceptions/README.md)
- [`Lyo.Health`](../../Health/Lyo.Health/README.md)
- [`Lyo.Postgres`](../../../Data/Postgres/Lyo.Postgres/README.md)