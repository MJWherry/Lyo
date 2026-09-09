# Lyo.Postgres

Shared PostgreSQL host bits for Lyo libraries that own an EF Core schema (Audit, Email, ChangeTracker, EntityReference, etc.). When the consumer options opt in, a thin `IHostedService` calls `DbContext.Database.MigrateAsync` at host startup. The rest of the package supplies the pieces every `Lyo.*.Postgres` package used to re-implement: schema-scoped `DbContextOptions` building, `IDbContextFactory<TContext>` registration, design-time option construction, and a standard health probe.

> Out of scope: entity models and mappings. Each consumer package owns its own `DbContext`, entities, and migrations; this package only supplies the plumbing around them.

## Features

- **Hosted migrations.** `AddPostgresMigrations<TContext, TOptions>()` plus `PostgresMigrationHostedService<TContext, TOptions>`, driven from the `IPostgresMigrationConfig` options contract.
- **`PostgresOptionsBase`.** Base options class that implements `IPostgresMigrationConfig` with `ConnectionString`, `EnableAutoMigrations`, and a `Validate()` hook. Derived options supply the schema through the abstract `SchemaName` property, so a package declares its schema once instead of repeating it on every path that builds a connection.
- **`PostgresSchema`.** `BuildOptions<TContext>(connectionString, schema)` and `CreateContext<TContext>` produce a context whose migrations history lives at `"<schema>"."__EFMigrationsHistory"`. `EnsureAsync` issues `CREATE SCHEMA IF NOT EXISTS` with the schema name escaped.
- **`PostgresDesignTime.CreateOptions<TContext>`.** Builds `DbContextOptions<TContext>` for `dotnet ef` from an environment variable plus an optional fallback connection string, so each package's `IDesignTimeDbContextFactory` stays a one-liner.
- **`PostgresHealth.CheckAsync`.** Yields a `Lyo.Health` `HealthResult` from either an `IDbContextFactory<TContext>` or a live `DbContext`, and reports the schema under the `database` metadata key.
- **`AddPostgresDbContextFactory<TContext, TOptions>(options)`.** Registers the options instance plus a pooled-style `IDbContextFactory<TContext>` bound to that options' connection string and schema.

## Examples

### Usage

```csharp
using Lyo.Postgres;
using Microsoft.Extensions.DependencyInjection;

public sealed class AuditDbOptions : IPostgresMigrationConfig
{
    public string ConnectionString { get; init; } = "";
    public bool EnableAutoMigrations { get; init; }
    public string Schema { get; init; } = "audit";
}

services.Configure<AuditDbOptions>(configuration.GetSection("Audit:Postgres"));
services.AddPostgresMigrations<AuditDbContext, AuditDbOptions>();
```

## Types

| Type | Role |
| --------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `IPostgresMigrationConfig` | Options contract for hosted migrations. Members are `string ConnectionString`, `bool EnableAutoMigrations`, `string Schema`. |
| `PostgresMigrationHostedService<TContext, TOptions>` | `IHostedService` that, on `StartAsync`, scopes a fresh `TContext` over the configured connection string, runs `CREATE SCHEMA IF NOT EXISTS "<Schema>"`, then calls `MigrateAsync`. |
| `Extensions.AddPostgresMigrations<TContext, TOptions>()` | Registers the hosted service. Both type parameters are constrained (`TContext : DbContext`, `TOptions : class, IPostgresMigrationConfig`). |
| `Extensions.AddPostgresDbContextFactory<TContext, TOptions>(options)` | Registers `options` as a singleton and an `IDbContextFactory<TContext>` over `PostgresSchema.BuildOptions`. Every `Add{Feature}Postgres` shares this registration. |
| `PostgresOptionsBase` | Abstract `IPostgresMigrationConfig` implementation. Derived options declare their schema once via the protected abstract `SchemaName`, and override `Validate()` for package-specific checks. |
| `PostgresSchema` | `MigrationsHistoryTable` constant, `BuildOptions<TContext>` / `CreateContext<TContext>` (schema-scoped `UseNpgsql`), plus `EnsureAsync` (`CREATE SCHEMA IF NOT EXISTS`). |
| `PostgresDesignTime.CreateOptions<TContext>` | Design-time `DbContextOptions<TContext>` from an environment variable, plus an optional fallback connection string for local `dotnet ef` runs. |
| `PostgresHealth.CheckAsync` | `HealthResult` probe over an `IDbContextFactory<TContext>` or a live `DbContext`. Reports the schema under the `database` metadata key. |

The hosted service activates a `TContext` instance via `Activator.CreateInstance(typeof(TContext), dbContextOptions)`, so each consumer DbContext **must expose a public
constructor that takes a single `DbContextOptions<TContext>`**. The migrations history table is stored in the schema returned by `IPostgresMigrationConfig.Schema` as
`"<schema>"."__EFMigrationsHistory"`.

`StartAsync` short-circuits when `EnableAutoMigrations` is false. If the flag is true, both `ConnectionString` and `Schema` are required (whitespace throws via
`Lyo.Exceptions.ArgumentHelpers`).

## Usage

- The hosted service resolves `IOptions<AuditDbOptions>`.
- When `EnableAutoMigrations` is false, it returns immediately.
- Otherwise it builds an `AuditDbContext` with `UseNpgsql(connectionString, opt => opt.MigrationsHistoryTable("__EFMigrationsHistory", schema))`.
- It runs `CREATE SCHEMA IF NOT EXISTS "<schema>"` (the schema name is escaped by doubling embedded `"` characters before interpolation).
- Then it runs `Database.MigrateAsync(ct)`.

## Consumers

- `Lyo.Audit.Postgres`
- `Lyo.Email.Postgres`
- `Lyo.ChangeTracker.Postgres`
- `Lyo.EntityReference.Postgres`
- `Lyo.FileMetadataStore.Postgres`

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Health` (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (direct, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (direct, third-party)