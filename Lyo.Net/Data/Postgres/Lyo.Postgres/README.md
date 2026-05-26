# Lyo.Postgres

Shared PostgreSQL migration plumbing for Lyo libraries that ship their own EF Core schema (Audit, Email, ChangeTracker, EntityReference, etc.). The package is a thin
**`IHostedService`** that runs **`DbContext.Database.MigrateAsync`** at host startup when the consumer’s options opt in.

> Out of scope: this package does **not** ship health checks, design-time `IDesignTimeDbContextFactory` helpers, or connection-string builders. Those live in the consumer
> libraries (e.g. `Lyo.Audit.Postgres`, `Lyo.Email.Postgres`) when needed.

## Public API

| Type                                                          | Role                                                                                                                                                                                |
|---------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **`IPostgresMigrationConfig`**                                | Contract for options classes that want to opt into hosted migrations. Members: `string ConnectionString`, `bool EnableAutoMigrations`, `string Schema`.                             |
| **`PostgresMigrationHostedService<TContext, TOptions>`**      | `IHostedService` that, on `StartAsync`, scopes a fresh `TContext` over the configured connection string, runs `CREATE SCHEMA IF NOT EXISTS "<Schema>"`, then `MigrateAsync`.        |
| **`Extensions.AddPostgresMigrations<TContext, TOptions>()`**  | Registers the hosted service. Both type parameters are constrained: `TContext : DbContext`, `TOptions : class, IPostgresMigrationConfig`.                                           |

The hosted service activates a `TContext` instance via `Activator.CreateInstance(typeof(TContext), dbContextOptions)`, so each consumer DbContext **must expose a public
constructor that takes a single `DbContextOptions<TContext>`**. The migrations history table is stored in the schema returned by `IPostgresMigrationConfig.Schema` as
`"<schema>"."__EFMigrationsHistory"`.

`StartAsync` short-circuits when `EnableAutoMigrations` is false. If the flag is true, both `ConnectionString` and `Schema` are required (whitespace throws via
`Lyo.Exceptions.ArgumentHelpers`).

## Usage

Consumer libraries (Audit, Email, etc.) typically own their own DbContext + options pair, then call this extension during their own DI registration:

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

When the host starts:

1. The hosted service resolves `IOptions<AuditDbOptions>`.
2. If `EnableAutoMigrations` is false, it returns immediately.
3. Otherwise it constructs an `AuditDbContext` with `UseNpgsql(connectionString, opt => opt.MigrationsHistoryTable("__EFMigrationsHistory", schema))`.
4. It executes `CREATE SCHEMA IF NOT EXISTS "<schema>"` (the schema name is escaped by doubling embedded `"` characters before interpolation).
5. It runs `Database.MigrateAsync(ct)`.

The host must be started — typically `Host.CreateDefaultBuilder()` (worker / console) or `WebApplication.CreateBuilder()` (ASP.NET Core).

## Dependencies

*(Synchronized from `Lyo.Postgres.csproj`.)*

**Target framework:** `net10.0`

### NuGet packages

| Package                                     | Version | Notes                                            |
|---------------------------------------------|---------|--------------------------------------------------|
| `Microsoft.EntityFrameworkCore`             | `[10,)` |                                                  |
| `Microsoft.EntityFrameworkCore.Design`      | `[10,)` | `PrivateAssets=all`; consumed only at design time. |
| `Microsoft.EntityFrameworkCore.Relational`  | `[10,)` |                                                  |
| `Microsoft.Extensions.Hosting.Abstractions` | `[10,)` |                                                  |
| `Microsoft.Extensions.Logging.Abstractions` | `[10,)` |                                                  |
| `Microsoft.Extensions.Options`              | `[10,)` |                                                  |
| `Npgsql.EntityFrameworkCore.PostgreSQL`     | `[10,)` |                                                  |

### Project references

- [`Lyo.Exceptions`](../../../Core/Lyo.Exceptions/README.md)

## Consumers

Examples of libraries that wire their own options + context into this hosted service:

- `Lyo.Audit.Postgres`
- `Lyo.Email.Postgres`
- `Lyo.ChangeTracker.Postgres`
- `Lyo.EntityReference.Postgres`
- `Lyo.FileMetadataStore.Postgres`
