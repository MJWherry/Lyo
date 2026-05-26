# Lyo.PackageMetadata

Multi-ecosystem **`PackageMetadata`** rows, **`PackageMetadataRegistration`**, **`IPackageMetadataStore`**, and **`PackageArtifactDigest`** helpers for correlating stack-trace
namespaces with persisted package/catalog data.

## Public surface

- **`PackageMetadata`** — `sealed record` carrying ecosystem, name/version, optional `ArtifactDigestAlgorithm`/`ArtifactDigestHex`, project/repository/license URLs, SPDX
  `LicenseExpression` (+ parsed `LicenseExpressionSyntax`), `CreatedAt`/`UpdatedAt` timestamps.
- **`PackageEcosystem`** enum — `Unknown`, `NuGet`, `Maven`, `Gradle`, `Conan`, `Vcpkg`, `Debian`, `Rpm`, `Msi`, `Other`.
- **`ArtifactDigestAlgorithm`** enum — `None`, `Sha512`, `Sha256`, `Sha1`.
- **`PackageMetadataRegistration`** — pair of namespace prefixes and the **`PackageMetadata`** to register.
- **`IPackageMetadataStore`** — `TryGetForFrameAsync`, `TryGetManyForStrippedMethodPrefixesAsync`, `RegisterManyAsync`.
- **`InMemoryPackageMetadataStore`** — thread-safe store; build with `new InMemoryPackageMetadataStore()`, then call `Register(prefixes, package)` or
  `RegisterManyAsync(registrations)`. No DI extension ships in this package (a Postgres-backed store with its own DI extensions lives in `Lyo.PackageMetadata.Postgres`).
- **`PackageArtifactDigest`** — `ComputeHex(algorithm, byte[]|Stream)` and `ComputeHexSha512(...)` convenience overloads for canonical artifact bytes.
- **`PackageLicenseExpression`** — `TryParseSyntax(expression)` and `TryGetSpdxLicenseIdentifiers(expression)` over SPDX 2.x (`AND`, `OR`, `WITH`).
- **`SpdxLicenseExpressionSyntax`** — JSON-friendly parsed tree (`license`, `exception`, `and`, `or`, `with`).

## Implementing `IPackageMetadataStore`

Custom implementations **must**:

- Honour **longest registered namespace-prefix wins** (`normalizedPrefix.` + **`strippedMethodPrefix.StartsWith(prefix, Ordinal)`**) for **`TryGetForFrameAsync`** and *
  *`TryGetManyForStrippedMethodPrefixesAsync`**.
- Return a map from **`TryGetManyForStrippedMethodPrefixesAsync`** that includes **one entry per distinct requested key**, with **`null`** when no prefix matches. Empty input ⇒
  empty map.

**Breaking changes:** Bulk resolve was added later; callers that implement **`IPackageMetadataStore`** themselves must supply **`TryGetManyForStrippedMethodPrefixesAsync`**. (
`netstandard2.0` means the interface cannot provide a default bulk implementation via DIM.)

## `namespacePrefix` parameter

Both lookup methods expose a **`namespacePrefix`** argument (**frame namespace**). **Matching currently ignores this value** — it is reserved for possible future narrowing. Passing
any value does not affect results today.

## `PostgresPackageMetadataStore` scalability

Bulk resolution loads **all** `(stack_prefix, package)` rows for one **in-process** longest-prefix sweep (reasonable for bounded catalogs).

- **`PostgresPackageMetadataOptions.PrefixCatalogCaching`** (see **`PostgresPrefixCatalogCachingMode`**) can skip re-querying the database on repeated **`TryGetMany`** (
  `InvalidateOnRegisterManyOrClear`) or **`Disabled`** when you wrap the store with your own cache. **`PostgresPackageMetadataStore.ClearPrefixCatalogCache()`** drops the
  in-process snapshot. The cache is **per process**. After **`RegisterManyAsync`** on that instance the snapshot is cleared. Other DB writers won't invalidate automatically —
  disable in-process caching or clear explicitly when imports finish off-process.
