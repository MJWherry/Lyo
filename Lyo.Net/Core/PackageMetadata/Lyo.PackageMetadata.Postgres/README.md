# Lyo.PackageMetadata.Postgres

EF Core persistence for **`Lyo.PackageMetadata.IPackageMetadataStore`**.

## Problem it solves

Stack traces contain **stripped namespaces / assembly-qualified tokens**. [`Lyo.PackageMetadata`](../Lyo.PackageMetadata/README.md) resolves **“what NuGet/Git package owns this prefix?”** for diagnostics overlays. **`PostgresPackageMetadataStore`** implements the store against normalized tables (**`PackageStackPrefix`** rows ordered by **`NormalizedPrefix`** length) so resolution is **`O(sorted prefixes)` in-process** after each DB read (bulk API loads all prefixes once per call when caching allows).

## Storage model (conceptual)

- **`PackageMetadataEntity`** holds catalog metadata (**id, package name, version range, SPDX/license expression blobs**, etc.—see EF configurations).
- **`PackageStackPrefixEntity`** maps **`NormalizedPrefix`** → owning package (**longest prefix wins**).

## Implementing behaviors

### Bulk resolution (`TryGetManyForStrippedMethodPrefixesAsync`)

- Input: list of **distinct** stripped prefixes (typically one per decoded stack frame subtree).
- Output: dictionary **including one entry per input key**, value **`null`** when nothing matches contract rules.
- **Never** silently drop requested keys—that invariant keeps **`Lyo.Diagnostic`** bulk decode paths deterministic.

Single-frame API **`TryGetForFrameAsync`** delegates to the bulk method with a singleton list (**`namespacePrefix` parameter is reserved** per core package README).

### Prefix catalog caching (`PostgresPrefixCatalogCachingMode`)

`PostgresPackageMetadataOptions.PrefixCatalogCaching` controls repeated DB chatter:

| Mode | Meaning |
|------|---------|
| **`InvalidateOnRegisterManyOrClear`** | Maintain an immutable in-process snapshot; reload when mutations bump generation counter (**`ClearPrefixCatalogCache`** or **`RegisterManyAsync`** on same instance). |
| **`Disabled`** | Always hit Postgres (wrap with outer cache yourself if importing huge catalogs asynchronously). |

`ClearPrefixCatalogCache()` is harmless when caching disabled (**only bumps generation**).

Cross-process writes **do not** invalidate—document that for ops (*disable snapshot* or bust cache after CLI imports).

## DI registration (`Extensions`)

- **`AddPackageMetadataDbContextFactory`** (+ `FromConfiguration`) — binds **`PostgresPackageMetadataOptions`**, registers **`PackageMetadataDbContext` factory**, attaches **`AddPostgresMigrations`** bootstrap.
- **`AddPostgresPackageMetadataStore`** — factory + **`IPackageMetadataStore` ⇒ PostgresPackageMetadataStore** singleton resolving options + **`IDbContextFactory`**.

## Migrations & schema

Migrations ship under **`Migrations/`** (schema/table names follow **`PostgresPackageMetadataOptions.Schema`** conventions—inspect snapshot for authoritative DDL).

## See also

- [`Lyo.Diagnostic`](../../Diagnostic/Lyo.Diagnostic/README.md) stack decode + breadcrumb tooling that **optionally consumes** **`IPackageMetadataStore`**.
