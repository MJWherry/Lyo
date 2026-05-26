# Lyo.HomeInventory.Postgres

EF Core implementation of [`IHomeInventoryStore`](../Lyo.HomeInventory/README.md)
backed by PostgreSQL.

- `HomeInventoryDbContext` models items, categories, locations, stock, and the
  movement ledger (see `Database/*` plus the migrations snapshot for the
  authoritative FK / cascade story).
- `PostgresHomeInventoryStore` runs stock adjustments and transfers inside
  database transactions (`BeginTransactionAsync`) so the stock row and its
  paired movement row commit atomically.
- The store also implements `Lyo.Health.IHealth`
  (`HealthCheckName = "home-inventory-postgres"`), so registering the store
  exposes a database liveness probe.
- Schema constant: `PostgresHomeInventoryOptions.Schema = "home_inventory"`.
  The default configuration section name is `PostgresHomeInventory`
  (`PostgresHomeInventoryOptions.SectionName`).

## DI extensions

Defined in `Extensions.cs` as `IServiceCollection` extensions:

- `AddHomeInventoryDbContextFactory(Action<PostgresHomeInventoryOptions>)` /
  `AddHomeInventoryDbContextFactory(PostgresHomeInventoryOptions)` — register
  only the `IDbContextFactory<HomeInventoryDbContext>` (useful for tooling and
  migrations).
- `AddHomeInventoryDbContextFactoryFromConfiguration(IConfiguration, string sectionName = PostgresHomeInventoryOptions.SectionName)`
  — same, bound from configuration.
- `AddPostgresHomeInventoryStore(Action<PostgresHomeInventoryOptions>)` /
  `AddPostgresHomeInventoryStore(PostgresHomeInventoryOptions)` — register the
  DbContext factory **and** the `IHomeInventoryStore` singleton.
- `AddPostgresHomeInventoryStoreFromConfiguration(IConfiguration, string sectionName = PostgresHomeInventoryOptions.SectionName)`
  — register the store using configuration binding.

All paths call `AddPostgresMigrations<HomeInventoryDbContext, PostgresHomeInventoryOptions>`
from [`Lyo.Postgres`](../../../Data/Postgres/Lyo.Postgres/README.md), keeping
hosted migration startup consistent with the rest of the framework.

## Usage

```csharp
services.AddPostgresHomeInventoryStore(new PostgresHomeInventoryOptions {
    ConnectionString = "...",
    EnableAutoMigrations = true
});
```

Or with configuration:

```json
{
  "PostgresHomeInventory": {
    "ConnectionString": "Host=localhost;Database=lyo;...",
    "EnableAutoMigrations": true
  }
}
```

```csharp
services.AddPostgresHomeInventoryStoreFromConfiguration(configuration);
```

## Migrations hygiene

Coordinate schema changes with any API adapters — especially when adjusting
movement uniqueness constraints or the `(ItemId, LocationId)` composite key
on stock (which must stay aligned with `UpsertStockAsync` semantics). Avoid
editing historical migrations retroactively unless you intentionally squash,
because that breaks checksums already deployed in production CI.

## Error model

`Lyo.Exceptions` argument helpers (`ArgumentHelpers.ThrowIfNull`,
`ThrowIfNullOrWhiteSpace`) guard malformed inputs. Domain-level conflicts use
`Lyo.Exceptions.OperationHelpers.ThrowIf` / `ThrowIfNull`, so for example:

- Deleting a category fails with an operation exception when child categories
  or assigned items still exist.
- Deleting a location fails when child locations, stock rows, or movement
  history reference it.
- `AdjustStockAsync` fails when an adjustment would drive `QuantityOnHand`
  negative, or when called against a non-existent stock row with a
  non-positive delta.
- `TransferStockAsync` fails when `quantity <= 0`, the source row is missing,
  or the source has insufficient on-hand quantity.

## See also

- [`Lyo.HomeInventory`](../Lyo.HomeInventory/README.md) — interface, records,
  and enum vocabulary.
