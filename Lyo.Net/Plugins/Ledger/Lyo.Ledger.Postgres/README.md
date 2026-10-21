# Lyo.Ledger.Postgres

PostgreSQL storage for Lyo.Ledger. Schema constant: `PostgresLedgerOptions.Schema = "ledger"`. Configuration section: `PostgresLedger`. Journal lines store optional `client_id`
without a cross-schema FK.

## Examples

### Register the store

```csharp
services.AddPostgresLedgerStore(new PostgresLedgerOptions {
    ConnectionString = "...",
    EnableAutoMigrations = true
});
```

### Migrations

```bash
export LEDGER_CONNECTION_STRING="Host=localhost;Database=lyo;Username=postgres;Password=postgres"
dotnet ef migrations add MigrationName --project Plugins/Ledger/Lyo.Ledger.Postgres
```

## See also

- [`Lyo.Ledger`](../Lyo.Ledger/README.md). Store interface and records.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.EntityReference.Models` (direct, lyo)
- `Lyo.EntityReference.Postgres` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Health` (direct, lyo)
- `Lyo.Ledger` (direct, lyo)
- `Lyo.Postgres` (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)