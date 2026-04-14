using Lyo.Postgres;

namespace Lyo.Tag.Postgres;

/// <summary>Configuration options for PostgreSQL tag store.</summary>
public sealed class PostgresTagOptions : IPostgresMigrationConfig
{
    public const string SectionName = "PostgresTag";
    public const string Schema = "tag";

    /// <summary>Gets or sets the PostgreSQL connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets whether to enable automatic database migrations on startup.</summary>
    public bool EnableAutoMigrations { get; set; } = false;

    /// <inheritdoc />
    string IPostgresMigrationConfig.Schema => Schema;
}