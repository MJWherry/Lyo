using Lyo.Postgres;

namespace Lyo.PackageMetadata.Postgres;

/// <summary>Settings for the package metadata PostgreSQL store.</summary>
public sealed class PostgresPackageMetadataOptions : PostgresOptionsBase
{
    public const string SectionName = "PostgresPackageMetadata";
    public const string Schema = "package_metadata";

    /// <summary>
    /// In-process cache of ordered stack-prefix rows. See <see cref="PostgresPrefixCatalogCachingMode" />. Callers can skip caching with
    /// <see cref="PostgresPrefixCatalogCachingMode.Disabled" /> and supply their own layer, or drop the snapshot via
    /// <see cref="PostgresPackageMetadataStore.ClearPrefixCatalogCache" />.
    /// </summary>
    public PostgresPrefixCatalogCachingMode PrefixCatalogCaching { get; set; } = PostgresPrefixCatalogCachingMode.InvalidateOnRegisterManyOrClear;

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}