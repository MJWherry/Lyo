using Lyo.Postgres;

namespace Lyo.PackageMetadata.Postgres;

/// <summary>Configuration for the package metadata PostgreSQL store.</summary>
public sealed class PostgresPackageMetadataOptions : IPostgresMigrationConfig
{
    public const string SectionName = "PostgresPackageMetadata";
    public const string Schema = "package_metadata";

    /// <summary>PostgreSQL connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>When true, run EF migrations at application startup.</summary>
    public bool EnableAutoMigrations { get; set; }

    /// <summary>
    /// In-process caching of ordered stack-prefix rows. See <see cref="PostgresPrefixCatalogCachingMode" />. Consumers can bypass caching with
    /// <see cref="PostgresPrefixCatalogCachingMode.Disabled" /> and implement their own layer, or clear the snapshot via <see cref="PostgresPackageMetadataStore.ClearPrefixCatalogCache"/>.
    /// </summary>
    public PostgresPrefixCatalogCachingMode PrefixCatalogCaching { get; set; } = PostgresPrefixCatalogCachingMode.InvalidateOnRegisterManyOrClear;

    /// <inheritdoc />
    string IPostgresMigrationConfig.Schema => Schema;
}
