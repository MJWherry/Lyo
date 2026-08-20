# Lyo.Validation.Postgres

PostgreSQL + EF Core implementation of [`IValidationSchemaStore`](../Lyo.Validation/README.md). Stores named validation schemas in schema `validation`, table `schema`, with `constraints_json` / `messages_json` as jsonb.

**Archetype A (Lyo domain).** Hosts expose or fetch `ValidationSchema` JSON on their own API. This package is the database backend only.

## Features

- **PostgresValidationSchemaStore.** `IValidationSchemaStore` via `IDbContextFactory<ValidationDbContext>`.
- **JSONB WhereClause.** Same AST as query filters (`In`, `NotIn`, `Regex`, groups).
- **Unique key.** `ux_validation_schema_key` on `key`.

## Examples

### Registration

```csharp
services.AddPostgresValidationStoreFromConfiguration(configuration);
```

## Schema

PostgreSQL schema `validation`. Table `schema`: `id`, unique `key`, `target_type_name`, `description`, `constraints_json` (WhereClause), `messages_json`, timestamps. Migrations history lives in `__EFMigrationsHistory` in the same schema.

## Migrations

Design-time: set `VALIDATION_CONNECTION_STRING` and run `dotnet ef migrations add MigrationName --project Core/Validation/Lyo.Validation.Postgres --context ValidationDbContext`.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Postgres` (direct, lyo)
- `Lyo.Query.Models` (direct, lyo)
- `Lyo.Validation` (direct, lyo)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Lyo.Cache` (transitive, lyo)
- `Lyo.Compression` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.Query` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)