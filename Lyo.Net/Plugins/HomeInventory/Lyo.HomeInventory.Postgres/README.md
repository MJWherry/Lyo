# Lyo.HomeInventory.Postgres

EF Core implementation of [`IHomeInventoryStore`](../Lyo.HomeInventory/README.md) backed by PostgreSQL.

- `HomeInventoryDbContext` models items, categories, locations, stock, attachments, and the movement ledger (see `Database/*` plus the migrations snapshot for the FK / cascade story). - `PostgresHomeInventoryStore` runs stock adjustments and transfers inside database transactions (`BeginTransactionAsync`) so the stock row and its paired movement row commit atomically. - The store also implements `Lyo.Health.IHealth` (`HealthCheckName = "home-inventory-postgres"`). Registering the store exposes a database liveness probe. - Schema constant: `PostgresHomeInventoryOptions.Schema = "home_inventory"`. Default configuration section: `PostgresHomeInventory` (`PostgresHomeInventoryOptions.SectionName`).

## Examples

### Register the store

```csharp
services.AddPostgresHomeInventoryStore(new PostgresHomeInventoryOptions {
    ConnectionString = "...",
    EnableAutoMigrations = true
});
```

### appsettings.json

```json
{
  "PostgresHomeInventory": {
    "ConnectionString": "Host=localhost;Database=lyo;...",
    "EnableAutoMigrations": true
  }
}
```

### Bind from configuration

```csharp
services.AddPostgresHomeInventoryStoreFromConfiguration(configuration);
```

## Service registration

- `AddHomeInventoryDbContextFactory(Action<PostgresHomeInventoryOptions>)` / `AddHomeInventoryDbContextFactory(PostgresHomeInventoryOptions)` register the `IDbContextFactory<HomeInventoryDbContext>` only. Handy for tooling and migrations.
- `AddHomeInventoryDbContextFactoryFromConfiguration(IConfiguration, string sectionName = PostgresHomeInventoryOptions.SectionName)` does the same, bound from configuration.
- `AddPostgresHomeInventoryStore(Action<PostgresHomeInventoryOptions>)` / `AddPostgresHomeInventoryStore(PostgresHomeInventoryOptions)` register the DbContext factory **and** the `IHomeInventoryStore` singleton.
- `AddPostgresHomeInventoryStoreFromConfiguration(IConfiguration, string sectionName = PostgresHomeInventoryOptions.SectionName)` registers the store by binding configuration.

## From configuration

The same registration from configuration:

## Migration hygiene

Coordinate schema changes with any API adapters, especially when adjusting movement uniqueness constraints or the `(ItemId, LocationId)` composite key on stock (which must stay aligned with `UpsertStockAsync` semantics). Avoid editing historical migrations retroactively unless you intentionally squash, because that breaks checksums already deployed in production CI.

## Tenant isolation

Inventory entities (`HomeItemEntity`, `HomeCategoryEntity`, `HomeLocationEntity`, `HomeItemStockEntity`, `HomeItemMovementEntity`, `HomeAttachmentEntity`) carry a nullable `tenant_id` (uuid) column with a filtered `ix_home_inv_<entity>_tenant` index. `OwnerEntityType` / `OwnerEntityId` on items still describe which user/household owns the item *within* a tenant. Those are orthogonal concepts.

`IHomeInventoryStore` accepts an explicit `Guid? tenantId` on every method and runs it through `TenancyResolver.Resolve` using the policy configured in `PostgresHomeInventoryOptions.Tenancy` (inheriting from `EntityRefOptions.Mode` when unset). Stock and movement transactional methods (`AdjustStockAsync`, `TransferStockAsync`) resolve the tenant once and stamp it on every row written in the transaction:

- `SystemOnly`. Every row is persisted with `tenant_id = NULL`.
- `SingleTenantDefault` *(default)*, caller value, falling back to `Tenancy.DefaultTenantId` then `EntityRefOptions.DefaultTenantId`.
- `MultiTenantStrict`. Caller must supply a non-empty `tenantId` or the store throws.

See [`Lyo.EntityReference.Postgres`](../../../Core/EntityReference/Lyo.EntityReference.Postgres/README.md#tenancy) for the policy matrix.

```json
{
  "PostgresHomeInventory": {
    "ConnectionString": "Host=localhost;Database=home_inventory;...",
    "Tenancy": { "Mode": "MultiTenantStrict" }
  }
}
```

## Error model

- Deleting a category fails with an operation exception when child categories or assigned items still exist.
- Deleting a location fails when child locations, stock rows, or movement history reference it. Assigned items have `location_id` set to null (`OnDelete: SetNull`).
- `AdjustStockAsync` fails when an adjustment would drive `QuantityOnHand` negative, or when called against a non-existent stock row with a non-positive delta.
- `TransferStockAsync` fails when `quantity <= 0`, the source row is missing, or the source has insufficient on-hand quantity.

## See also

- [`Lyo.HomeInventory`](../Lyo.HomeInventory/README.md). Interface, records, and enum vocabulary.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Api` (direct, lyo)
- `Lyo.EntityReference.Models` (direct, lyo)
- `Lyo.EntityReference.Postgres` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Health` (direct, lyo)
- `Lyo.HomeInventory` (direct, lyo)
- `Lyo.HomeInventory.Models` (direct, lyo)
- `Lyo.Postgres` (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` (direct, microsoft)
- `Lyo.Api.Models` (transitive, lyo)
- `Lyo.Cache` (transitive, lyo)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Lyo.Compression` (transitive, lyo)
- `Lyo.DateAndTime` (transitive, lyo)
- `Lyo.Diagnostic` (transitive, lyo)
- `Lyo.Diagnostic.AspNetCore` (transitive, lyo)
- `Lyo.Diff` (transitive, lyo)
- `Lyo.Encryption` (transitive, lyo)
- `Lyo.Formatter` (transitive, lyo)
- `Lyo.Hashing` (transitive, lyo)
- `Lyo.Json` (transitive, lyo)
- `Lyo.KeyStore` (transitive, lyo)
- `Lyo.Metrics` (transitive, lyo)
- `Lyo.PackageMetadata` (transitive, lyo)
- `Lyo.Parameters` (transitive, lyo)
- `Lyo.Query` (transitive, lyo)
- `Lyo.Query.Evaluation` (transitive, lyo)
- `Lyo.Query.Models` (transitive, lyo)
- `Lyo.Query.Sql` (transitive, lyo)
- `Lyo.Result` (transitive, lyo)
- `Lyo.Streams` (transitive, lyo)
- `Lyo.Validation` (transitive, lyo)
- `Lyo.Validation.Models` (transitive, lyo)
- `BouncyCastle.Cryptography` `2.6.2` (transitive, third-party, netstandard2.0)
- `DynamicExpresso.Core` `2.19.3` (transitive, third-party)
- `EasyCompressor` `2.1.0` (transitive, third-party)
- `Konscious.Security.Cryptography.Argon2` `1.3.1` (transitive, third-party)
- `Microsoft.AspNetCore.OpenApi` `10.0.5` (transitive, microsoft)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore.Analyzers` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Caching.Memory` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft, net10.0, netstandard2.0)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `SmartFormat.NET` `3.6.1` (transitive, third-party)
- `System.Buffers` `4.6.1` (transitive, microsoft, netstandard2.0)
- `System.ComponentModel.Annotations` `5.0.0` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Encoding.CodePages` `10.0.5` (transitive, microsoft)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)
- `System.Threading.Tasks.Extensions` `4.6.3` (transitive, microsoft, netstandard2.0)