namespace Lyo.FileMetadataStore.Sqlite;

/// <summary>Configuration options for SQLite file metadata store service.</summary>
public sealed class SqliteFileMetadataStoreOptions : Lyo.Sqlite.ISqliteMigrationConfig
{
    public const string SectionName = "SqliteFileMetadataStore";

    /// <summary>Gets or sets the SQLite connection string (e.g. <c>Data Source=./filestore.db</c>).</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets whether to enable automatic database migrations on startup.</summary>
    public bool EnableAutoMigrations { get; set; } = false;
}
