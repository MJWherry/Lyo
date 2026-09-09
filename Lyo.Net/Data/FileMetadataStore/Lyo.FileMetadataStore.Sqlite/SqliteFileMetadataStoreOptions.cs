using Lyo.Sqlite;

namespace Lyo.FileMetadataStore.Sqlite;

/// <summary>Options for the SQLite file metadata store.</summary>
public sealed class SqliteFileMetadataStoreOptions : ISqliteMigrationConfig
{
    public const string SectionName = "SqliteFileMetadataStore";

    /// <summary>SQLite connection string (e.g. <c>Data Source=./filestore.db</c>).</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>When true, applies database migrations on startup. Starts as false.</summary>
    public bool EnableAutoMigrations { get; set; } = false;
}