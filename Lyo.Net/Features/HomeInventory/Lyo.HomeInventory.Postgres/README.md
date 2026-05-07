# Lyo.HomeInventory.Postgres

EF Core implementation of [`IHomeInventoryStore`](../Lyo.HomeInventory/README.md):

- **`HomeInventoryDbContext`** models items, categories, locations, quantities, movement ledger tables (see **`Database/*`** + migrations snapshot for authoritative FK cascades).
- **`PostgresHomeInventoryStore`** implements transactional stock adjustments + transfers leveraging PostgreSQL concurrency guarantees (row-level locking patterns—inspect
  implementation before exposing high-contention kiosk endpoints).

Also surfaces **`IHealth`** for readiness checks.

## DI (`Extensions`)

| Method                                                                    | Behavior                                                                                                                    |
|---------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------|
| **`AddPostgresHomeInventoryStore(Action<PostgresHomeInventoryOptions>)`** | Fluent configuration (connection string, auto-migrations, schema overrides). Registers factory + **`IHomeInventoryStore`**. |
| **`AddPostgresHomeInventoryStoreFromConfiguration`**                      | Binds **`PostgresHomeInventoryOptions`** section (`SectionName` constant on options type).                                  |
| **`AddPostgresHomeInventoryStore(PostgresHomeInventoryOptions)`**         | Direct options object (tests/integration harness).                                                                          |

All paths call **`AddPostgresMigrations<HomeInventoryDbContext, PostgresHomeInventoryOptions>`** from [`Lyo.Postgres`](../../../Data/Postgres/Lyo.Postgres/README.md) ensuring
hosted migration startup consistent with Comic/Note modules.

## Migrations hygiene

Coordinate schema changes with any API adapters—especially when adjusting movement uniqueness constraints or stock composite keys (`(ItemId, LocationId)` uniqueness must stay
aligned with **`UpsertStockAsync`** semantics).

Avoid editing historical migrations retroactively unless you intentionally squash (breaks checksums deployed in prod CI).

## Error model

Throws **`Lyo.Exceptions`** guarded argument failures for malformed SKUs/`Guid.Empty`. Domain-level conflicts (“delete category with dependents”) derive from Postgres exceptions
surfaced as either thrown `InvalidOperationException` wrappers—read store code paths for specifics before mapping to Problem Details statuses.

## See also

[`Lyo.HomeInventory`](../Lyo.HomeInventory/README.md) conceptual overview + movement vocabulary.
