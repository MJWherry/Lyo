# Lyo.HomeInventory.Postgres

EF Core implementation of [`IHomeInventoryStore`](../Lyo.HomeInventory/README.md) backed by PostgreSQL.

- `HomeInventoryDbContext` models items, categories, locations, stock, and the movement ledger (see `Database/*` plus the migrations snapshot for the authoritative FK / cascade story). - `PostgresHomeInventoryStore` runs stock adjustments and transfers inside database transactions (`BeginTransactionAsync`) so the stock row and its paired movement row commit atomically. - The store also implements `Lyo.Health.IHealth` (`HealthCheckName = "home-inventory-postgres"`), so registering the store exposes a database liveness probe. - Schema constant: `PostgresHomeInventoryOptions.Schema = "home_inventory"`. The default configuration section name is `PostgresHomeInventory` (`PostgresHomeInventoryOptions.SectionName`).

## Examples

### Usage

```csharp
services.AddPostgresHomeInventoryStore(new PostgresHomeInventoryOptions {
    ConnectionString = "...",
    EnableAutoMigrations = true
});
```

### Usage (2)

```json
{
  "PostgresHomeInventory": {
    "ConnectionString": "Host=localhost;Database=lyo;...",
    "EnableAutoMigrations": true
  }
}
```

### Usage (3)

```csharp
services.AddPostgresHomeInventoryStoreFromConfiguration(configuration);
```

## DI extensions

- `AddHomeInventoryDbContextFactory(Action<PostgresHomeInventoryOptions>)` / `AddHomeInventoryDbContextFactory(PostgresHomeInventoryOptions)` — register only the `IDbContextFactory<HomeInventoryDbContext>` (useful for tooling and migrations).
- `AddHomeInventoryDbContextFactoryFromConfiguration(IConfiguration, string sectionName = PostgresHomeInventoryOptions.SectionName)` — same, bound from configuration.
- `AddPostgresHomeInventoryStore(Action<PostgresHomeInventoryOptions>)` / `AddPostgresHomeInventoryStore(PostgresHomeInventoryOptions)` — register the DbContext factory **and** the `IHomeInventoryStore` singleton.
- `AddPostgresHomeInventoryStoreFromConfiguration(IConfiguration, string sectionName = PostgresHomeInventoryOptions.SectionName)` — register the store using configuration binding.

## Usage

Or with configuration:

## Migrations hygiene

Coordinate schema changes with any API adapters — especially when adjusting movement uniqueness constraints or the `(ItemId, LocationId)` composite key on stock (which must stay aligned with `UpsertStockAsync` semantics). Avoid editing historical migrations retroactively unless you intentionally squash, because that breaks checksums already deployed in production CI.

## Tenancy

All five entities (`HomeItemEntity`, `HomeCategoryEntity`, `HomeLocationEntity`, `HomeItemStockEntity`, `HomeItemMovementEntity`) carry a nullable `tenant_id` (uuid)
column with a filtered `ix_home_inv_<entity>_tenant` index. `OwnerEntityType` / `OwnerEntityId` on items still describe which user/household owns the item *within* a
tenant — they're orthogonal concepts.

`IHomeInventoryStore` accepts an explicit `Guid? tenantId` on every method and runs it through `TenancyResolver.Resolve` using the policy configured in
`PostgresHomeInventoryOptions.Tenancy` (inheriting from `EntityRefOptions.Mode` when unset). Stock and movement transactional methods (`AdjustStockAsync`,
`TransferStockAsync`) resolve the tenant once and stamp it on every row written in the transaction:

- `SystemOnly` — every row is persisted with `tenant_id = NULL`.
- `SingleTenantDefault` *(default)* — caller value, falling back to `Tenancy.DefaultTenantId` then `EntityRefOptions.DefaultTenantId`.
- `MultiTenantStrict` — caller must supply a non-empty `tenantId` or the store throws.

See [`Lyo.EntityReference.Postgres`](../../../Core/EntityReference/Lyo.EntityReference.Postgres/README.md#tenancy) for the full matrix.

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
- Deleting a location fails when child locations, stock rows, or movement history reference it.
- `AdjustStockAsync` fails when an adjustment would drive `QuantityOnHand` negative, or when called against a non-existent stock row with a non-positive delta.
- `TransferStockAsync` fails when `quantity <= 0`, the source row is missing, or the source has insufficient on-hand quantity.

## See also

- [`Lyo.HomeInventory`](../Lyo.HomeInventory/README.md) — interface, records, and enum vocabulary.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.EntityReference.Models` — (direct, lyo)
- `Lyo.EntityReference.Postgres` — (direct, lyo)
- `Lyo.Exceptions` — (direct, lyo)
- `Lyo.Health` — (direct, lyo)
- `Lyo.HomeInventory` — (direct, lyo)
- `Lyo.Postgres` — (direct, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` — (direct, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Configuration.Binder` `10.0.5` — (direct, microsoft)
- `Lyo.Common` — (transitive, lyo)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` — (transitive, third-party)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)