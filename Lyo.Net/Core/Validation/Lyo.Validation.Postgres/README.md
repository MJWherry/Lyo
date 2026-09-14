# Lyo.Validation.Postgres

PostgreSQL + EF Core implementation of [`IValidationSchemaStore`](../Lyo.Validation/README.md). Named validation schemas live in schema `validation`, table `schema`, with `constraints_json` / `messages_json` as jsonb.

**Archetype A (Lyo domain).** Hosts expose or fetch `ValidationSchema` JSON on their own API. This package is the database backend only.

## Features

- **PostgresValidationSchemaStore.** `IValidationSchemaStore` backed by `IDbContextFactory<ValidationDbContext>`.
- **JSONB WhereClause.** The same AST query filters use (`In`, `NotIn`, `Regex`, groups).
- **Unique key.** `key` is constrained by `ux_validation_schema_key`.

## Examples

### Registration

```csharp
services.AddPostgresValidationStoreFromConfiguration(configuration);
```

## Database schema

PostgreSQL schema `validation`. Table `schema` columns: `id`, unique `key`, `target_type_name`, `description`, `constraints_json` (WhereClause), `messages_json`, timestamps. Migrations history lives in `__EFMigrationsHistory` in the same schema.

## Migrations

At design time set `VALIDATION_CONNECTION_STRING` and run `dotnet ef migrations add MigrationName --project Core/Validation/Lyo.Validation.Postgres --context ValidationDbContext`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Json` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Postgres` (direct, lyo)
- `Lyo.Query.Models` (direct, lyo)
- `Lyo.Validation` (direct, lyo)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Query.Evaluation` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Validation.Models` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)