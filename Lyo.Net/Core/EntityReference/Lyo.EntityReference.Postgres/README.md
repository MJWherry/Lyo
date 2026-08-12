# Lyo.EntityReference.Postgres

Entity Framework Core building blocks for **relation** rows (subject/actor associations) and **source link** rows (import provenance) on PostgreSQL.

## Column shapes

**Relations** — subject/actor endpoints (persisted as legacy `for_entity_*` / `from_entity_*` names):

| Property (C#)                           | Column                                | Role                                                     |
|-----------------------------------------|---------------------------------------|----------------------------------------------------------|
| `SubjectEntityType` / `SubjectEntityId` | `for_entity_type` / `for_entity_id`   | Entity the relation applies to (e.g. `Docket`)           |
| `ActorEntityType` / `ActorEntityId`     | `from_entity_type` / `from_entity_id` | Entity that performed or owns the relation (e.g. `User`) |

Endpoint columns are **nullable at the DB level**; stores and **`EntityRelationValidation`** enforce both endpoints.

**Source provenance** — inline on parent rows (same pattern as relations on `comment`):

| Property (C#)                         | Column                                    |
|---------------------------------------|-------------------------------------------|
| `SourceEntityType` / `SourceEntityId` | `source_entity_type` / `source_entity_id` |
| `ImportedAt`                          | `imported_at`                             |

## When to use which base

| Base                                    | Use case                                                                                                                                                                                                 |
|-----------------------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **`EntityRelationEndpointsEntityBase`** | Shared subject/actor columns only. Rarely used directly.                                                                                                                                                 |
| **`EntityRelationEntityBase`**          | Canonical tenant-scoped relation: soft-delete, visibility, jsonb metadata, lifecycle timestamps. Subclass per module (`FavoriteEntity`, `TagEntity`, …).                                                 |
| **`EntitySourceEntityBase`**            | Inline import provenance on parent rows (`source_entity_*`, `imported_at`). Subclass per module (`PersonEntity`, `AddressEntity`, …) — mirrors **`EntityRelationEntityBase`** for source instead of ref. |
| **`EntitySourceDerivedEntityBase`**     | Extends **`EntitySourceEntityBase`** with **`Id`**, lifecycle timestamps, and **`LocallyModifiedAt`** when content may diverge from imported source(s).                                                  |
| **`EntityRelationOptionalActorBase`**   | String ids with optional actor columns and **nullable** tenant id (audit / change-tracker style rows).                                                                                                   |

## Index strategy

- Partial unique on `(tenant_id, for_entity_*, from_entity_*)` where `deleted_at IS NULL`
- Tenant lookups on subject and actor endpoints, plus context, `created_at`, and filtered `expires_at`

## Types

- **`EntityRelationEndpointConfigurationExtensions.ConfigureEntityRelationEndpointColumns`** — Maps subject/actor columns to `for_entity_*` / `from_entity_*`.
- **`EntitySourceLinkConfigurationExtensions.ConfigureEntitySourceColumns`** — Inline source columns + lookup index. **`ConfigureEntitySourceDerivedColumns`** maps **`Id`** and * *
  `LocallyModifiedAt`** on derived aggregates.
- **`EntitySourceConfiguration<TEntity>`** — Shared source column names/types and indexes. Subclass per module (optional; modules may call extensions directly).
- **`EntityRelationConfiguration<TEntity>`** — Shared relation column names/types and indexes. Subclass per module.
- **`EntityRelationOptionalActorExtensions.MapOptionalActorColumns`** — Endpoint columns plus nullable `tenant_id`.
- **`EntityRelationMapping`** / **`EntitySourceMapping`** — Domain ↔ EF mapping for endpoints and provenance records.
- **`EntityRefModuleDbContext`** — Override `SaveChanges` / `SaveChangesAsync` to set **`CreatedAt`** to UTC for new **`EntityRelationEntityBase`** entities when still default.
- **`EntityRefPostgresStoreBase`** — DI-friendly base for stores: resolves **`EntityRefOptions`**, holds **`IEntityRefActionInterceptor`** pipeline, exposes **`ResolveTenant`** and
  **`RunInterceptorsAsync`**.
- **`EntityRefPostgresStoreHelpers`** — **`WhereActive`**, **`WhereTenant`** (for `EntityRelationEntityBase`), **`RunInterceptorsAsync`**.
- **`EntityRelationOptionalActorHelpers`** — **`WhereTenant`** and **`WhereTenantOrSystem`** for the optional-actor base.

## Typical module wiring

- Define an EF entity inheriting **`EntityRelationEntityBase`** (relations) or **`EntitySourceEntityBase`** / **`EntitySourceDerivedEntityBase`** (inline provenance).
- Implement **`IEntityTypeConfiguration<T>`** inheriting **`EntityRelationConfiguration<T>`** or call **`ConfigureEntitySourceColumns`**, then map module-specific columns/indexes
  after **`ToTable`** / **`HasKey`**.
- Use **`EntityRefModuleDbContext`** (or replicate **`StampCreatedAtUtc`** logic) so **`created_at`** is populated automatically.
- In the store layer, map **`EntityRef`** to string columns via **`EntityRefPersistedGuid.PersistedEntityId()`** when callers still pass Guid values in **`EntityRef.EntityId`**.

## Debugging

**`EntityRelationEntityBase`** and **`EntityRelationOptionalActorBase`** implement **`[DebuggerDisplay(...)]`** and **`ToString()`** for quick inspection in the debugger and logs.

## Tenancy

Per-feature tenancy policy is configured via **`TenancyOptions`** on each `Postgres*Options` (e.g. `PostgresFavoriteOptions.Tenancy`). The store resolves caller-supplied
`Guid? tenantId` through **`TenancyResolver.Resolve`** before reads and writes:

| Mode                                | Caller value | Behaviour                                                                                 |
|-------------------------------------|--------------|-------------------------------------------------------------------------------------------|
| **`SystemOnly`**                    | any          | Always resolves to `null`; only valid for stores backed by a nullable `tenant_id` column. |
| **`SingleTenantDefault`** (default) | non-empty    | Returns the caller value.                                                                 |
| **`SingleTenantDefault`**           | null / empty | Falls back to `TenancyOptions.DefaultTenantId`, then `EntityRefOptions.DefaultTenantId`.  |
| **`MultiTenantStrict`**             | non-empty    | Returns the caller value.                                                                 |
| **`MultiTenantStrict`**             | null / empty | Throws `ArgumentNullException` — callers must supply an explicit tenant.                  |
| **`MultiTenantOptional`**           | non-empty    | Returns the caller value.                                                                 |
| **`MultiTenantOptional`**           | null / empty | Returns `null` — row is system-level / untenanted (audit, change-tracker, …).             |

If a per-feature `TenancyOptions.Mode` is unset it inherits **`EntityRefOptions.Mode`** (default `SingleTenantDefault`). The same fallback applies to `DefaultTenantId`.

`EntityRefPostgresStoreBase` rejects `SystemOnly` at construction time for stores that map to a non-nullable `tenant_id` column. Pass `requiresNonNullTenant: false` from the base
ctor for stores backed by `EntityRelationOptionalActorBase` (or any nullable-tenant entity).

## Tenancy — `appsettings.json`

```json
{
  "EntityRef": {
    "Mode": "SingleTenantDefault",
    "DefaultTenantId": "00000000-0000-0000-0000-000000000000"
  },
  "PostgresAudit": { "Tenancy": { "Mode": "MultiTenantStrict" } },
  "PostgresChangeTracker": { "Tenancy": { "Mode": "SystemOnly" } },
  "PostgresFavorite": { }
}
```

Bind `EntityRefOptions` once at the host with **`AddEntityRefOptionsFromConfiguration`**; each module's `Extensions.cs` is responsible for binding its own `Postgres*Options`
section.

## See also

- **`Lyo.EntityReference.Models`** — `EntityRef`, `EntityRelationRow`, `EntitySourceRecord`, `EntityRelationValidation`, `EntitySourceValidation`, composite encoding, JSON
  converter, and interceptors.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.EntityReference.Models` — (direct, lyo)
- `Lyo.Postgres` — (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` — (direct, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` — (direct, microsoft)
- `Lyo.Common` — (transitive, lyo)
- `Lyo.Exceptions` — (transitive, lyo)
- `Microsoft.EntityFrameworkCore` `10.0.5` — (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` — (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` — (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` — (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` — (transitive, third-party)
- `System.Memory` `4.6.3` — (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` — (transitive, microsoft, netstandard2.0)