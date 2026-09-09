# Lyo.EntityReference.Postgres

EF Core building blocks on PostgreSQL for relation rows (subject/actor associations) and source-link rows (import provenance).

## Column shapes

**Relations.** Legacy `for_entity_*` / `from_entity_*` columns store the subject/actor endpoints:

| Property (C#) | Column | Role |
| --------------------------------------- | ------------------------------------- | -------------------------------------------------------- |
| `SubjectEntityType` / `SubjectEntityId` | `for_entity_type` / `for_entity_id` | Entity the relation applies to (e.g. `Docket`) |
| `ActorEntityType` / `ActorEntityId` | `from_entity_type` / `from_entity_id` | Entity that performed or owns the relation (e.g. `User`) |

Stores plus `EntityRelationValidation` require both endpoints even though the DB columns are nullable.

**Source provenance.** Same inline-on-parent pattern used for relations on `comment`:

| Property (C#) | Column |
|---------------------------------------|-------------------------------------------|
| `SourceEntityType` / `SourceEntityId` | `source_entity_type` / `source_entity_id` |
| `ImportedAt` | `imported_at` |

## Which base to pick

| Base | Use case |
| ----------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `EntityRelationEndpointsEntityBase` | Shared subject/actor columns only. Rarely used directly. |
| `EntityRelationEntityBase` | Canonical tenant-scoped relation: soft-delete, visibility, jsonb metadata, lifecycle timestamps. Subclass per module (`FavoriteEntity`, `TagEntity`, …). |
| `EntitySourceEntityBase` | Inline import provenance on parent rows (`source_entity_*`, `imported_at`). Subclass per module (`PersonEntity`, `AddressEntity`, …). Mirrors `EntityRelationEntityBase` for source instead of ref. |
| `EntitySourceDerivedEntityBase` | Extends `EntitySourceEntityBase` with `Id`, lifecycle timestamps, and `LocallyModifiedAt` when content may diverge from imported source(s). |
| `EntityRelationOptionalActorBase` | String ids with optional actor columns and nullable tenant id (audit / change-tracker style rows). |

## Index strategy

- Partial unique on `(tenant_id, for_entity_*, from_entity_*)` where `deleted_at IS NULL`
- Tenant lookups on subject and actor endpoints, plus context, `created_at`, and filtered `expires_at`

## Types

- **`EntityRelationEndpointConfigurationExtensions.ConfigureEntityRelationEndpointColumns`.** Maps subject/actor columns to `for_entity_*` / `from_entity_*`.
- **`EntitySourceLinkConfigurationExtensions.ConfigureEntitySourceColumns`.** Inline source columns + lookup index. `ConfigureEntitySourceDerivedColumns` maps `Id` and `LocallyModifiedAt` on derived aggregates.
- **`EntitySourceConfiguration<TEntity>`.** Shared source column names/types and indexes. Subclass per module. Optional. Modules may call extensions directly.
- **`EntityRelationConfiguration<TEntity>`.** Shared relation column names/types and indexes. Subclass per module.
- **`EntityRelationOptionalActorExtensions.MapOptionalActorColumns`.** Endpoint columns plus nullable `tenant_id`.
- **`EntityRelationMapping` / `EntitySourceMapping`.** Domain to EF mapping for endpoints and provenance records.
- **`EntityRefModuleDbContext`.** Override `SaveChanges` / `SaveChangesAsync` to set `CreatedAt` to UTC for new `EntityRelationEntityBase` entities when still default.
- **`EntityRefPostgresStoreBase`.** DI-friendly base for stores. Resolves `EntityRefOptions`, holds `IEntityRefActionInterceptor` pipeline, exposes `ResolveTenant` and `RunInterceptorsAsync`.
- **`EntityRefPostgresStoreHelpers`.** `WhereActive`, `WhereTenant` (for `EntityRelationEntityBase`), `RunInterceptorsAsync`.
- **`EntityRelationOptionalActorHelpers`.** `WhereTenant` and `WhereTenantOrSystem` for the optional-actor base.

## Typical module wiring

- Define an EF entity inheriting `EntityRelationEntityBase` (relations) or `EntitySourceEntityBase` / `EntitySourceDerivedEntityBase` (inline provenance).
- Implement `IEntityTypeConfiguration<T>` inheriting `EntityRelationConfiguration<T>` or call `ConfigureEntitySourceColumns`, then map module-specific columns/indexes after `ToTable` / `HasKey`.
- Use `EntityRefModuleDbContext` (or replicate `StampCreatedAtUtc` logic) so `created_at` is populated automatically.
- In the store layer, map `EntityRef` to string columns via `EntityRefPersistedGuid.PersistedEntityId()` when callers still pass Guid values in `EntityRef.EntityId`.

## Debugging

Debugger and log inspection come from `[DebuggerDisplay(...)]` and `ToString()` on `EntityRelationEntityBase` and `EntityRelationOptionalActorBase`.

## Tenancy

Each `Postgres*Options` carries a `TenancyOptions` (for example `PostgresFavoriteOptions.Tenancy`) that sets that feature's tenancy policy. Before a read or write, the store runs `TenancyResolver.Resolve` on the caller-supplied
`Guid? tenantId`:

| Mode | Caller value | Behaviour |
| ------------------------------- | ------------ | ----------------------------------------------------------------------------------------- |
| `SystemOnly` | any | Always resolves to `null`. Only valid for stores backed by a nullable `tenant_id` column. |
| `SingleTenantDefault` (default) | non-empty | Returns the caller value. |
| `SingleTenantDefault` | null / empty | Falls back to `TenancyOptions.DefaultTenantId`, then `EntityRefOptions.DefaultTenantId`. |
| `MultiTenantStrict` | non-empty | Returns the caller value. |
| `MultiTenantStrict` | null / empty | Throws `ArgumentNullException`. Callers must supply an explicit tenant. |
| `MultiTenantOptional` | non-empty | Returns the caller value. |
| `MultiTenantOptional` | null / empty | Returns `null`. Row is system-level / untenanted (audit, change-tracker, …). |

An unset per-feature `TenancyOptions.Mode` falls back to `EntityRefOptions.Mode` (default `SingleTenantDefault`). `DefaultTenantId` follows the same inheritance.

Stores that map a non-nullable `tenant_id` column cannot be constructed with `SystemOnly`; `EntityRefPostgresStoreBase` rejects that pairing. For `EntityRelationOptionalActorBase` (or any nullable-tenant entity), pass `requiresNonNullTenant: false` from the base
ctor.

## Tenancy in `appsettings.json`

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

`AddEntityRefOptionsFromConfiguration` binds `EntityRefOptions` once on the host. Each module's `Extensions.cs` binds that module's own `Postgres*Options`
section.

## See also

- `Lyo.EntityReference.Models`. `EntityRef`, `EntityRelationRow`, `EntitySourceRecord`, `EntityRelationValidation`, `EntitySourceValidation`, composite encoding, JSON converter, and interceptors.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.EntityReference.Models` (direct, lyo)
- `Lyo.Postgres` (direct, lyo)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (direct, microsoft)
- `Microsoft.Extensions.Options.ConfigurationExtensions` `10.0.5` (direct, microsoft)
- `Lyo.Common.Core` (transitive, lyo)
- `Lyo.Exceptions` (transitive, lyo)
- `Lyo.Health` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.EntityFrameworkCore` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Design` `10.0.5` (transitive, microsoft)
- `Microsoft.EntityFrameworkCore.Relational` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Hosting.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Options` `10.0.5` (transitive, microsoft)
- `Npgsql.EntityFrameworkCore.PostgreSQL` `10.0.3` (transitive, third-party)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)