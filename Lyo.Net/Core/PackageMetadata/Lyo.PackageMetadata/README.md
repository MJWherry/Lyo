# Lyo.PackageMetadata

Multi-ecosystem `PackageMetadata` rows, `PackageMetadataRegistration`, `IPackageMetadataStore`, and `PackageArtifactDigest` helpers for correlating stack-trace namespaces with persisted package/catalog data.

## Types and methods

- **PackageMetadata.** `sealed record` carrying ecosystem, name/version, optional `ArtifactDigestAlgorithm`/`ArtifactDigestHex`, project/repository/license URLs, SPDX `LicenseExpression` (plus parsed `LicenseExpressionSyntax`), `CreatedAt`/`UpdatedAt` timestamps.
- **PackageEcosystem.** Enum: `Unknown`, `NuGet`, `Maven`, `Gradle`, `Conan`, `Vcpkg`, `Debian`, `Rpm`, `Msi`, `Other`.
- **ArtifactDigestAlgorithm.** Enum: `None`, `Sha512`, `Sha256`, `Sha1`.
- **PackageMetadataRegistration.** Pair of namespace prefixes and the `PackageMetadata` to register.
- **IPackageMetadataStore.** `TryGetForFrameAsync`, `TryGetManyForStrippedMethodPrefixesAsync`, `RegisterManyAsync`.
- **InMemoryPackageMetadataStore.** Thread-safe store. Build with `new InMemoryPackageMetadataStore()`, then call `Register(prefixes, package)` or `RegisterManyAsync(registrations)`. No DI extension ships in this package. A Postgres-backed store with its own DI extensions lives in `Lyo.PackageMetadata.Postgres`.
- **PackageArtifactDigest.** `ComputeHex(algorithm, byte[]|Stream)` and `ComputeHexSha512(...)` overloads for canonical artifact bytes.
- **PackageLicenseExpression.** `TryParseSyntax(expression)` and `TryGetSpdxLicenseIdentifiers(expression)` over SPDX 2.x (`AND`, `OR`, `WITH`).
- **SpdxLicenseExpressionSyntax.** JSON-friendly parsed tree (`license`, `exception`, `and`, `or`, `with`).

## Implementing `IPackageMetadataStore`

- Honour longest registered namespace-prefix wins (`normalizedPrefix.` + `strippedMethodPrefix.StartsWith(prefix, Ordinal)`) for `TryGetForFrameAsync` and `TryGetManyForStrippedMethodPrefixesAsync`.
- Return a map from `TryGetManyForStrippedMethodPrefixesAsync` that includes one entry per distinct requested key, with `null` when no prefix matches. Empty input returns an empty map.

## `namespacePrefix` parameter

Both lookup methods expose a `namespacePrefix` argument (frame namespace). Matching currently ignores this value. It is reserved for possible future narrowing. Passing any value does not affect results today.

## `PostgresPackageMetadataStore` scalability

- `PostgresPackageMetadataOptions.PrefixCatalogCaching` (see `PostgresPrefixCatalogCachingMode`) can skip re-querying the database on repeated `TryGetMany` (`InvalidateOnRegisterManyOrClear`) or `Disabled` when you wrap the store with your own cache. `PostgresPackageMetadataStore.ClearPrefixCatalogCache()` drops the in-process snapshot. The cache is per process. After `RegisterManyAsync` on that instance the snapshot is cleared. Other DB writers will not invalidate automatically. Disable in-process caching or clear explicitly when imports finish off-process.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Hashing` (direct, lyo)
- `System.Threading.Tasks.Extensions` `4.6.3` (direct, microsoft)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `Microsoft.Extensions.Logging.Abstractions` `10.0.5` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)