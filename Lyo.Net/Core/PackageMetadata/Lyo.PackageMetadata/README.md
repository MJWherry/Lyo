# Lyo.PackageMetadata

`PackageMetadata` rows across ecosystems, `PackageMetadataRegistration`, `IPackageMetadataStore`, and `PackageArtifactDigest` helpers that correlate stack-trace namespaces with persisted package/catalog data.

## Methods and types

- **PackageMetadata.** `sealed record` carrying ecosystem, name/version, optional `ArtifactDigestHex`/`ArtifactDigestAlgorithm`, project/repository/license URLs, SPDX `LicenseExpression` (plus parsed `LicenseExpressionSyntax`), `UpdatedAt`/`CreatedAt` timestamps.
- **PackageEcosystem.** Enum: `Unknown`, `NuGet`, `Maven`, `Gradle`, `Conan`, `Vcpkg`, `Debian`, `Rpm`, `Msi`, `Other`.
- **ArtifactDigestAlgorithm.** Enum: `None`, `Sha256`, `Sha512`, `Sha1`.
- **PackageMetadataRegistration.** A `PackageMetadata` plus the namespace prefixes to register.
- **IPackageMetadataStore.** `TryGetManyForStrippedMethodPrefixesAsync`, `TryGetForFrameAsync`, `RegisterManyAsync`.
- **InMemoryPackageMetadataStore.** Thread-safe store. Build with `new InMemoryPackageMetadataStore()`, then call `Register(prefixes, package)` or `RegisterManyAsync(registrations)`. No DI extension ships in this package. A Postgres-backed store with its own DI extensions lives in `Lyo.PackageMetadata.Postgres`.
- **PackageArtifactDigest.** `ComputeHex(algorithm, Stream|byte[])` and `ComputeHexSha512(...)` overloads for canonical artifact bytes.
- **PackageLicenseExpression.** `TryGetSpdxLicenseIdentifiers(expression)` and `TryParseSyntax(expression)` over SPDX 2.x (`OR`, `AND`, `WITH`).
- **SpdxLicenseExpressionSyntax.** JSON-friendly parsed tree (`exception`, `license`, `or`, `and`, `with`).

## Writing an `IPackageMetadataStore`

- Honour longest registered namespace-prefix wins (`normalizedPrefix.` + `strippedMethodPrefix.StartsWith(prefix, Ordinal)`) for `TryGetManyForStrippedMethodPrefixesAsync` and `TryGetForFrameAsync`.
- Return a map from `TryGetManyForStrippedMethodPrefixesAsync` that includes one entry per distinct requested key, with `null` when no prefix matches. Empty input returns an empty map.

## `namespacePrefix` argument

A `namespacePrefix` argument (frame namespace) exists on both lookup methods. Matching currently ignores this value. It is reserved for possible future narrowing. Passing any value does not affect results today.

## How `PostgresPackageMetadataStore` scales

- `PostgresPackageMetadataOptions.PrefixCatalogCaching` (see `PostgresPrefixCatalogCachingMode`) can skip re-querying the database on repeated `TryGetMany` (`InvalidateOnRegisterManyOrClear`) or `Disabled` when you wrap the store with your own cache. `PostgresPackageMetadataStore.ClearPrefixCatalogCache()` drops the in-process snapshot. The cache is per process. After `RegisterManyAsync` on that instance the snapshot is cleared. Other DB writers will not invalidate automatically. Disable in-process caching or clear explicitly when imports finish off-process.

## Dependencies

Generated from `ProjectReference` / `PackageReference` (same model as `docs/Lyo.ProjectGraph.html`).

- `Lyo.Common.Core` (direct, lyo)
- `Lyo.Exceptions` (direct, lyo)
- `Lyo.Hashing` (direct, lyo)
- `System.Threading.Tasks.Extensions` `4.6.3` (direct, microsoft)
- `Lyo.Common.Metadata` (transitive, lyo)
- `Microsoft.Bcl.AsyncInterfaces` `10.0.5` (transitive, microsoft, netstandard2.0)
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.5` (transitive, microsoft)
- `System.IO.Hashing` `10.0.5` (transitive, microsoft, net10.0)
- `System.Memory` `4.6.3` (transitive, microsoft, netstandard2.0)
- `System.Text.Json` `10.0.5` (transitive, microsoft, netstandard2.0)