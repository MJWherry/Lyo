# Lyo.Idempotency.Postgres

PostgresIdempotencyStore writes kind=api from TryBegin and kind=outbox from InsertOutbox. A caller transaction gets a savepoint around the insert. On 23505 the store rolls back to
that savepoint and re-reads the live row. The caller transaction stays open.

## Features

- **One live row per scope and key.** The unique index is (scope, key) where state is in_progress, completed, or failed. Expired rows stay in the table and are not in the index.
- **Outbox insert does not commit.** InsertOutbox uses the caller's DbTransaction. A different payload rolls that transaction back and throws ConflictException.

## Examples

### Register

```csharp
services.AddPostgresIdempotency(new PostgresIdempotencyOptions {
    ConnectionString = connectionString,
    EnableAutoMigrations = true
});
```

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Health` (direct, lyo)
- `Lyo.Idempotency` (direct, lyo)
- `Lyo.Postgres` (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Npgsql` `10.0.3` (direct, third-party)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (direct, third-party)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)