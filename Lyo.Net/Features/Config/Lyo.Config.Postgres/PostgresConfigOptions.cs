using Lyo.Postgres;

namespace Lyo.Config.Postgres;

/// <summary>Configuration options for PostgreSQL config storage.</summary>
public sealed class PostgresConfigOptions : IPostgresMigrationConfig
{
    public const string SectionName = "PostgresConfig";
    public const string Schema = "config";

    /// <summary>Gets or sets the PostgreSQL connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets whether to enable automatic database migrations on startup.</summary>
    public bool EnableAutoMigrations { get; set; }

    /// <inheritdoc />
    string IPostgresMigrationConfig.Schema => Schema;
}