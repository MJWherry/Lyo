# Lyo.Sqlite

Common SQLite migration host bits for Lyo libraries that own an EF Core schema. When the consumer options opt in, a thin `IHostedService` calls `DbContext.Database.MigrateAsync` as the host starts.

> Out of scope: health checks, design-time `IDesignTimeDbContextFactory` helpers, and connection-string builders stay in consumer libraries when those need them.

## Examples

### How to wire it

```csharp
using Lyo.Sqlite;
using Microsoft.Extensions.DependencyInjection;

public sealed class FileStoreSqliteOptions : ISqliteMigrationConfig
{
    public string ConnectionString { get; init; } = "Data Source=./filestore.db";
    public bool EnableAutoMigrations { get; init; }
}

services.Configure<FileStoreSqliteOptions>(configuration.GetSection("SqliteFileMetadataStore"));
services.AddSqliteMigrations<SqliteFileMetadataStoreDbContext, FileStoreSqliteOptions>();
```

## Types

| Type | Role |
| ------------------------------------------------------ | --------------------------------------------------- |
| `ISqliteMigrationConfig` | `ConnectionString`, `EnableAutoMigrations` |
| `SqliteMigrationHostedService<TContext, TOptions>` | Calls `MigrateAsync` from `StartAsync` when enabled |
| `Extensions.AddSqliteMigrations<TContext, TOptions>()` | Adds the hosted service |

`TContext` is built with `Activator.CreateInstance(typeof(TContext), dbContextOptions)`, so every consumer DbContext **must expose a public constructor that
takes a single `DbContextOptions<TContext>`**.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Sqlite` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (direct, microsoft)