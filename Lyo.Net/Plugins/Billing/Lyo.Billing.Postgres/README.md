# Lyo.Billing.Postgres

PostgreSQL storage for Lyo.Billing. Issue/pay/void wrap `ILedgerStore` in a `TransactionScope`. Bill-to clients live in `billing.client`. Cipher columns hold client/invoice tax
snapshots and biller remittance. Schema constant: `PostgresBillingOptions.Schema = "billing"`.

## Examples

### Register the store

```csharp
services.AddPostgresLedgerStore(ledgerOptions);
services.AddPostgresBillingStore(new PostgresBillingOptions {
    ConnectionString = "...",
    EnableAutoMigrations = true
});
services.AddBillingFieldEncryption(BillingEncryptionKeys.Fields);
```

### Migrations

```bash
export BILLING_CONNECTION_STRING="Host=localhost;Database=lyo;Username=postgres;Password=postgres"
dotnet ef migrations add MigrationName --project Plugins/Billing/Lyo.Billing.Postgres
```

## See also

- [`Lyo.Billing`](../Lyo.Billing/README.md). Store interface and records.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Billing` (direct, lyo)
- `Lyo.Encryption` (direct, lyo)
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
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)