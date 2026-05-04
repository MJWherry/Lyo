# Lyo.PackageMetadata

NuGet-style **`PackageMetadata`** rows, **`PackageMetadataRegistration`**, **`IPackageMetadataStore`**, and helpers (e.g. **`PackageFileSha512`**) for correlating stack-trace namespaces with persisted package/catalog data.

## Implementing `IPackageMetadataStore`

Custom implementations **must**:

- Honour **longest registered namespace-prefix wins** (`normalizedPrefix.` + **`strippedMethodPrefix.StartsWith(prefix, Ordinal)`**) for **`TryGetForFrameAsync`** and **`TryGetManyForStrippedMethodPrefixesAsync`**.
- Return a map from **`TryGetManyForStrippedMethodPrefixesAsync`** that includes **one entry per distinct requested key**, with **`null`** when no prefix matches. Empty input ⇒ empty map.

**Breaking changes:** Bulk resolve was added later; callers that implement **`IPackageMetadataStore`** themselves must supply **`TryGetManyForStrippedMethodPrefixesAsync`**. (`netstandard2.0` means the interface cannot provide a default bulk implementation via DIM.)

## `namespacePrefix` parameter

Both lookup methods expose a **`namespacePrefix`** argument (**frame namespace**). **Matching currently ignores this value** — it is reserved for possible future narrowing. Passing any value does not affect results today.

## `PostgresPackageMetadataStore` scalability

Bulk resolution loads **all** `(stack_prefix, package)` rows for one **in-process** longest-prefix sweep (reasonable for bounded catalogs).

- **`PostgresPackageMetadataOptions.PrefixCatalogCaching`** (see **`PostgresPrefixCatalogCachingMode`**) can skip re-querying the database on repeated **`TryGetMany`** (`InvalidateOnRegisterManyOrClear`) or **`Disabled`** when you wrap the store with your own cache. **`PostgresPackageMetadataStore.ClearPrefixCatalogCache()`** drops the in-process snapshot. The cache is **per process**. After **`RegisterManyAsync`** on that instance the snapshot is cleared. Other DB writers won't invalidate automatically — disable in-process caching or clear explicitly when imports finish off-process.

