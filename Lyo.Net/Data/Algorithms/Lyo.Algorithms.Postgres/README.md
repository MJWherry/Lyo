# Lyo.Algorithms.Postgres

IAlgorithmDefinitionStore on PostgreSQL. Tables algorithm_definition, algorithm_definition_revision, and algorithm_definition_label live in schema algorithms. Reads are
AsNoTracking. SaveRevision locks the definition row, then allocates the next revision inside a transaction. Revision rows have an optional payload column for structured scorers.
The store also implements IHealth.

## Features

- **Options.** PostgresAlgorithmOptions (section PostgresAlgorithms) extends PostgresOptionsBase. Schema is algorithms.
- **DI.** AddPostgresAlgorithmStore, the Action overload, and AddPostgresAlgorithmStoreFromConfiguration register the store and IHealth.
- **Migrations.** AddPostgresMigrations applies the schema when EnableAutoMigrations is true.

## Examples

### Register from configuration

```csharp
services.AddAlgorithms();
services.AddPostgresAlgorithmStoreFromConfiguration(configuration);
```

## When to use

Use this package when more than one process shares the catalog. SQLite is single-writer. Product tables keep their own algorithm key and optional revision or label; this schema
stores the source.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Algorithms` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Health` (direct, lyo)
- `Lyo.Postgres` (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Lyo.Algorithms.Models` (transitive, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `DynamicExpresso.Core` `2.19.3` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)