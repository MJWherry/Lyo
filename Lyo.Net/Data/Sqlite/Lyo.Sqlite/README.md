# Lyo.Sqlite

Shared SQLite migration plumbing for Lyo libraries that ship their own EF Core schema. The package is a thin **`IHostedService`** that runs **`DbContext.Database.MigrateAsync`** at host startup when the consumer's options opt in.

> Out of scope: health checks, design-time `IDesignTimeDbContextFactory` helpers, and connection-string builders live in consumer libraries when needed.

## Public API

| Type | Role |
|------|------|
| **`ISqliteMigrationConfig`** | `ConnectionString`, `EnableAutoMigrations` |
| **`SqliteMigrationHostedService<TContext, TOptions>`** | Runs `MigrateAsync` on `StartAsync` when enabled |
| **`Extensions.AddSqliteMigrations<TContext, TOptions>()`** | Registers the hosted service |

The hosted service constructs `TContext` via `Activator.CreateInstance(typeof(TContext), dbContextOptions)`, so each consumer DbContext **must expose a public constructor that takes a single `DbContextOptions<TContext>`**.

## Usage

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

## Dependencies

**Target framework:** `net10.0`

- `Microsoft.EntityFrameworkCore.Sqlite`
- `Microsoft.Extensions.Hosting.Abstractions`
- `Microsoft.Extensions.Options`
- [`Lyo.Exceptions`](../../Core/Lyo.Exceptions/README.md)
