# Lyo.EntityReference.Postgres

Entity Framework Core building blocks for **tenant-scoped association rows** on PostgreSQL where **`for_entity_id`** and **`from_entity_id`** are stored as **`uuid`**, aligned with
**`EntityRef`** Option A persistence in **`Lyo.EntityReference.Models`**.

## When to use which base

| Base                                             | Use case                                                                                                                                                                       |
|--------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **`EntityRefEntityBase`**                        | Canonical association: **required** tenant id, GUID targets, soft-delete, visibility, jsonb metadata. Subclass per module (`FavoriteEntity`, `TagEntity`, …).                  |
| **`EntityRefOptionalFromStringAssociationBase`** | String ids (including composite text), optional actor columns, and **nullable** tenant id. For change-tracker / audit style rows where some rows are system-level (no tenant). |

## Types

- **`EntityRefConfiguration<TEntity>`** — Shared column names/types (`uuid`, `timestamp with time zone`, `jsonb`) and indexes (partial unique on active rows, tenant lookups, expiry
  filter). Pass an **`indexPrefix`** (e.g. `tag`) for stable index names per module.
- **`EntityRefOptionalFromStringAssociationExtensions.MapOptionalFromStringAssociationColumns`** — Maps the four string association columns plus the nullable `tenant_id` column, with a configurable max length on the strings.
- **`EntityRefModuleDbContext`** — Override `SaveChanges` / `SaveChangesAsync` to set **`CreatedAt`** to UTC for new **`EntityRefEntityBase`** entities when still default.
- **`EntityRefPostgresStoreBase`** — DI-friendly base for stores: resolves **`EntityRefOptions`**, holds **`IEntityRefActionInterceptor`** pipeline, exposes **`ResolveTenant`** and
  **`RunInterceptorsAsync`**.
- **`EntityRefPostgresStoreHelpers`** — **`ResolveTenantId`**, **`WhereActive`**, **`WhereTenant`** (for `EntityRefEntityBase`), **`RunInterceptorsAsync`**.
- **`EntityRefOptionalFromStringAssociationHelpers`** — **`WhereTenant`** and **`WhereTenantOrSystem`** for the string base. The latter returns rows matching the tenant plus untenanted system rows.

## Typical module wiring

1. Define an EF entity inheriting **`EntityRefEntityBase`** (or the string base if applicable).
2. Implement **`IEntityTypeConfiguration<T>`** inheriting **`EntityRefConfiguration<T>`**, call **`MapColumns`** / **`MapIndexes`** after **`ToTable`** / **`HasKey`**.
3. Use **`EntityRefModuleDbContext`** (or replicate **`StampCreatedAtUtc`** logic) so **`created_at`** is populated automatically.
4. In the store layer, inherit **`EntityRefPostgresStoreBase`** and use **`EntityRefPersistedGuid`** from the Models package when mapping **`EntityRef`** to **`Guid`** columns.

## Debugging

**`EntityRefEntityBase`** and **`EntityRefOptionalFromStringAssociationBase`** implement **`[DebuggerDisplay(...)]`** and **`ToString()`** for quick inspection in the debugger and
logs.

## Tenancy

Per-feature tenancy policy is configured via **`TenancyOptions`** on each `Postgres*Options` (e.g. `PostgresFavoriteOptions.Tenancy`). The store resolves caller-supplied `Guid? tenantId` through **`TenancyResolver.Resolve`** before reads and writes:

| Mode                              | Caller value | Behaviour                                                                                                  |
|-----------------------------------|--------------|------------------------------------------------------------------------------------------------------------|
| **`SystemOnly`**                  | any          | Always resolves to `null`; only valid for stores backed by a nullable `tenant_id` column.                  |
| **`SingleTenantDefault`** (default) | non-empty    | Returns the caller value.                                                                                |
| **`SingleTenantDefault`**         | null / empty | Falls back to `TenancyOptions.DefaultTenantId`, then `EntityRefOptions.DefaultTenantId`.                   |
| **`MultiTenantStrict`**           | non-empty    | Returns the caller value.                                                                                  |
| **`MultiTenantStrict`**           | null / empty | Throws `ArgumentNullException` — callers must supply an explicit tenant.                                   |

If a per-feature `TenancyOptions.Mode` is unset it inherits **`EntityRefOptions.Mode`** (default `SingleTenantDefault`). The same fallback applies to `DefaultTenantId`.

`EntityRefPostgresStoreBase` rejects `SystemOnly` at construction time for stores that map to a non-nullable `tenant_id` column. Pass `requiresNonNullTenant: false` from the base ctor for stores backed by `EntityRefOptionalFromStringAssociationBase` (or any nullable-tenant entity).

### `appsettings.json`

```json
{
  "EntityRef": {
    "Mode": "SingleTenantDefault",
    "DefaultTenantId": "00000000-0000-0000-0000-000000000000"
  },
  "PostgresAudit":         { "Tenancy": { "Mode": "MultiTenantStrict" } },
  "PostgresChangeTracker": { "Tenancy": { "Mode": "SystemOnly" } },
  "PostgresFavorite":      { }
}
```

Bind `EntityRefOptions` once at the host with **`AddEntityRefOptionsFromConfiguration`**; each module's `Extensions.cs` is responsible for binding its own `Postgres*Options` section.

## See also

- **`Lyo.EntityReference.Models`** — `EntityRef`, composite encoding, JSON converter, interceptors, and `EntityRefRow` domain shape.
