using Lyo.Postgres;

namespace Lyo.FileMetadataStore.Postgres;

/// <summary>Configuration options for PostgreSQL file metadata store service.</summary>
public sealed class PostgresFileMetadataStoreOptions : IPostgresMigrationConfig
{
    public const string SectionName = "PostgresFileMetadataStore";
    public const string Schema = "filestore";

    /// <summary>Gets or sets the PostgreSQL connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets whether to enable automatic database migrations on startup.</summary>
    public bool EnableAutoMigrations { get; set; } = false;

    /// <inheritdoc />
    string IPostgresMigrationConfig.Schema => Schema;
}