# Lyo.Algorithms.Sqlite

IAlgorithmDefinitionStore on a SQLite file. Table names match the Postgres catalog without a schema, including the optional payload column on revisions. A unique revision conflict is retried once. Migrations go through Lyo.Sqlite. The store implements IHealth with SELECT 1. Multi-instance hosts should use Lyo.Algorithms.Postgres instead.

## Features

- **Options.** SqliteAlgorithmOptions (section SqliteAlgorithms) implements ISqliteMigrationConfig. ConnectionString is required.
- **DI.** AddSqliteAlgorithmStore, the Action overload, and AddSqliteAlgorithmStoreFromConfiguration register the store and IHealth.

## Examples

### Register a file database

```csharp
services.AddAlgorithms();
services.AddSqliteAlgorithmStore(new SqliteAlgorithmOptions { ConnectionString = "Data Source=./algorithms.db" });
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Algorithms` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Health` (direct, lyo)
- `Lyo.Sqlite` (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Lyo.Algorithms.Models` (transitive, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `DynamicExpresso.Core` `2.19.3` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore.Sqlite` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)