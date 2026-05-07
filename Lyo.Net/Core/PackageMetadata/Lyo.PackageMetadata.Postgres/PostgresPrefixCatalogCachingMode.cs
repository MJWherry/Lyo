namespace Lyo.PackageMetadata.Postgres;

/// <summary>Whether <see cref="PostgresPackageMetadataStore" /> keeps an in-process snapshot of ordered stack-prefix rows to avoid reloading the catalog on every lookup.</summary>
public enum PostgresPrefixCatalogCachingMode
{
    /// <summary>No snapshot; each bulk lookup reloads prefixes from PostgreSQL.</summary>
    Disabled = 0,

    /// <summary>
    /// Caches the ordered prefix list in memory. Entries are invalidated when <see cref="IPackageMetadataStore.RegisterManyAsync" /> succeeds on this instance, or when
    /// <see cref="PostgresPackageMetadataStore.ClearPrefixCatalogCache" /> is called.
    /// </summary>
    InvalidateOnRegisterManyOrClear = 1
}