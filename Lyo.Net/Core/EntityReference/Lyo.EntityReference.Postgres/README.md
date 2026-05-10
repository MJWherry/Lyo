# Lyo.EntityReference.Postgres

Entity Framework Core building blocks for **tenant-scoped association rows** on PostgreSQL where **`for_entity_id`** and **`from_entity_id`** are stored as **`uuid`**, aligned with **`EntityRef`** Option A persistence in **`Lyo.EntityReference.Models`**.

## When to use which base

| Base | Use case |
|------|-----------|
| **`EntityRefEntityBase`** | Canonical association: tenant id, GUID targets, soft-delete, visibility, jsonb metadata. Subclass per module (`FavoriteEntity`, `TagEntity`, …). |
| **`EntityRefOptionalFromStringAssociationBase`** | String ids (including composite text) and optional actor columns — for example change-tracker style rows. Not the uuid Option A layout. |

## Types

- **`EntityRefConfiguration<TEntity>`** — Shared column names/types (`uuid`, `timestamp with time zone`, `jsonb`) and indexes (partial unique on active rows, tenant lookups, expiry filter). Pass an **`indexPrefix`** (e.g. `tag`) for stable index names per module.
- **`EntityRefOptionalFromStringAssociationExtensions.MapOptionalFromStringAssociationColumns`** — Maps the four string association columns with a configurable max length.
- **`EntityRefModuleDbContext`** — Override `SaveChanges` / `SaveChangesAsync` to set **`CreatedAt`** to UTC for new **`EntityRefEntityBase`** entities when still default.
- **`EntityRefPostgresStoreBase`** — DI-friendly base for stores: resolves **`EntityRefOptions`**, holds **`IEntityRefActionInterceptor`** pipeline, exposes **`ResolveTenant`** and **`RunInterceptorsAsync`**.
- **`EntityRefPostgresStoreHelpers`** — **`ResolveTenantId`**, **`WhereActive`**, **`WhereTenant`**, **`RunInterceptorsAsync`**.

## Typical module wiring

1. Define an EF entity inheriting **`EntityRefEntityBase`** (or the string base if applicable).
2. Implement **`IEntityTypeConfiguration<T>`** inheriting **`EntityRefConfiguration<T>`**, call **`MapColumns`** / **`MapIndexes`** after **`ToTable`** / **`HasKey`**.
3. Use **`EntityRefModuleDbContext`** (or replicate **`StampCreatedAtUtc`** logic) so **`created_at`** is populated automatically.
4. In the store layer, inherit **`EntityRefPostgresStoreBase`** and use **`EntityRefPersistedGuid`** from the Models package when mapping **`EntityRef`** to **`Guid`** columns.

## Debugging

**`EntityRefEntityBase`** and **`EntityRefOptionalFromStringAssociationBase`** implement **`[DebuggerDisplay(...)]`** and **`ToString()`** for quick inspection in the debugger and logs.

## See also

- **`Lyo.EntityReference.Models`** — `EntityRef`, composite encoding, JSON converter, interceptors, and `EntityRefRow` domain shape.
