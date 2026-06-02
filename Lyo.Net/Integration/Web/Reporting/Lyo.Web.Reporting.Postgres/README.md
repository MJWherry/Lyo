# Lyo.Web.Reporting.Postgres

PostgreSQL **schema and migrations** for storing pre-built reports. Schema name is `report`.

> **Scope:** This package is currently a database / migrations layer. It ships an EF Core `DbContext`, a single `ReportEntity`, options, migrations, and DI registrations. It
> does **not** ship a domain `PostgresReportingService` — read/write logic lives in the consumer (typically a `Lyo.Api` host that maps `ReportEntity` through `Lyo.Api`'s
> generic CRUD pipeline, or a custom service built on the `ReportingDbContext` factory). A higher-level domain service is on the roadmap; the documentation below tracks what
> actually compiles today.

## What ships

- **`ReportingDbContext`** ([`Database/ReportingDbContext.cs`](Database/ReportingDbContext.cs)) with a single `DbSet<ReportEntity> Reports` and `HasDefaultSchema("report")`.
  `ReportingDbContextFactory` is provided for design-time tooling.
- **`ReportEntity`** ([`Database/ReportEntity.cs`](Database/ReportEntity.cs)) — the row shape:

  | Column                | Type / Constraints                                          |
        |-----------------------|-------------------------------------------------------------|
  | `Id`                  | `Guid`, primary key (default `Guid.NewGuid()`).             |
  | `Name`                | `string`, required, max length 500.                         |
  | `Description`         | `string?`, max length 2000.                                 |
  | `ReportDataJson`      | `string`, required, max length 32 768 (serialized payload). |
  | `ParameterTypeName`   | `string?`, max length 500 (for typed deserialization).      |
  | `CreatedTimestamp`    | `DateTime`, required (defaults to `UtcNow`).                |
  | `UpdatedTimestamp`    | `DateTime`, required (defaults to `UtcNow`).                |
  | `Tags`                | `string?`, max length 1000 (comma-separated or JSON array). |
  | `IsActive`            | `bool` (used for soft-delete semantics by callers).         |

  Fluent column metadata lives in `ReportEntityConfiguration`.
- **`PostgresReportingOptions`** ([`PostgresReportingOptions.cs`](PostgresReportingOptions.cs)) — `IPostgresMigrationConfig`. Section `"PostgresReporting"`. Properties:
  `ConnectionString`, `EnableAutoMigrations` (default `false`); `Schema = "report"` is constant.
- **Migrations** under [`Migrations/`](Migrations) — initial schema (`20250101000000_InitialCreate`) plus the model snapshot.

## DI surface ([`Extensions.cs`](Extensions.cs))

All registrations are extension methods on `IServiceCollection` (declared inside `extension(IServiceCollection services)` blocks):

| Method                                                                | Description                                                                                                                                                                                |
|-----------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `AddReportingDbContext(string connectionString)`                      | Convenience: registers the factory plus a scoped `ReportingDbContext` resolved from the factory.                                                                                           |
| `AddReportingDbContext(Action<DbContextOptionsBuilder>)`              | Standard EF `AddDbContext` overload.                                                                                                                                                       |
| `AddReportingDbContextFactory(Action<PostgresReportingOptions>)`      | Builds options inline.                                                                                                                                                                     |
| `AddReportingDbContextFactoryFromConfiguration(config, sectionName?)` | Binds options from configuration (default section `"PostgresReporting"`).                                                                                                                  |
| `AddReportingDbContextFactory(PostgresReportingOptions)`              | Registers `IDbContextFactory<ReportingDbContext>` (Npgsql provider), wires `AddPostgresMigrations<ReportingDbContext, …>`, and points the migrations history table at the `report` schema. |

## Quick start

```csharp
using Lyo.Web.Reporting.Postgres;

services.AddReportingDbContextFactoryFromConfiguration(builder.Configuration);

// or inline:
services.AddReportingDbContextFactory(o => {
    o.ConnectionString = "Host=localhost;Database=lyo;Username=postgres;Password=postgres";
    o.EnableAutoMigrations = true;
});
```

Once registered, write reports through the EF context. For example, exposing `ReportEntity` as a CRUD endpoint via `Lyo.Api`:

```csharp
services.AddLyoCrudServices<ReportingDbContext>();

app.CreateBuilder<ReportingDbContext, ReportEntity, ReportEntity, ReportEntity, Guid>("/api/Reports", "Reports")
    .WithCrud(c => c.WithFlags(ApiFeatureFlag.FullCrud | ApiFeatureFlag.Query))
    .Build();
```

…or build a custom domain service against `IDbContextFactory<ReportingDbContext>`. Soft-delete semantics (toggle `IsActive`) and tag/name filtering are not implemented in this
package — wire them in your service or via `WhereClause` filters when querying through `Lyo.Api`.

## Related projects

- [`Lyo.Web.Reporting`](../Lyo.Web.Reporting/README.md) — domain models for the report payload (`Report<T>`, sections, columns) serialized into `ReportDataJson`.
- [`Lyo.Postgres`](../../../../Data/Postgres/Lyo.Postgres/README.md) — migrations infrastructure.
- [`Lyo.Exceptions`](../../../../Core/Lyo.Exceptions/README.md)
- [`Lyo.Metrics`](../../../../Core/Metrics/Lyo.Metrics/README.md)
