# Lyo.Audit.Postgres

PostgreSQL implementation of Lyo.Audit using Entity Framework Core. Persists `AuditChange` and `AuditEvent` records to PostgreSQL with JSONB columns for dictionary data.

## Features

- **PostgresAuditRecorder** – Implements `IAuditRecorder` with PostgreSQL persistence
- **Migrations** – EF Core migrations with `audit` schema, `audit_changes` and `audit_events` tables
- **Auto migrations** – Optional automatic migration on startup via `EnableAutoMigrations`

## Quick Start

```csharp
services.AddPostgresAuditRecorder(new PostgresAuditOptions {
    ConnectionString = configuration.GetConnectionString("Audit")!,
    EnableAutoMigrations = true
});
```

Or without auto-migrations (apply migrations separately):

```csharp
services.AddAuditDbContext(configuration.GetConnectionString("Audit")!);
services.AddDbContextFactory<AuditDbContext>(/* ... */);
services.AddSingleton<IAuditRecorder, PostgresAuditRecorder>();
```

## Migrations

Design-time migrations require `AUDIT_CONNECTION_STRING` environment variable:

```bash
export AUDIT_CONNECTION_STRING="Host=localhost;Database=audit;Username=postgres;Password=postgres"
dotnet ef migrations add MigrationName --project Core/Audit/Lyo.Audit.Postgres
```

## Schema

- **audit.audit_changes** – `id` (uuid), `timestamp`, `type_assembly_full_name`, `old_values_json` (jsonb), `changed_properties_json` (jsonb)
- **audit.audit_events** – `id` (uuid), `event_type`, `timestamp`, `message`, `actor`, `metadata_json` (jsonb)

<!-- LYO_README_SYNC:BEGIN -->

## Dependencies

*(Synchronized from `Lyo.Audit.Postgres.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package | Version |
| --- | --- |
| `Microsoft.Extensions.Configuration.Abstractions` | `[10,)` |
| `Microsoft.Extensions.Configuration.Binder` | `[10,)` |

### Project references

- `Lyo.Audit`
- `Lyo.Exceptions`
- `Lyo.Health`
- `Lyo.Postgres`

## Public API (generated)

Top-level `public` types in `*.cs` (*10*). Nested types and file-scoped namespaces may omit some entries.

- `AuditChangeEntity`
- `AuditChangeEntityConfiguration`
- `AuditDbContext`
- `AuditDbContextFactory`
- `AuditEventEntity`
- `AuditEventEntityConfiguration`
- `Extensions`
- `InitialCreate`
- `PostgresAuditOptions`
- `PostgresAuditRecorder`

<!-- LYO_README_SYNC:END -->

