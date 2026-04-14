using Lyo.Postgres;

namespace Lyo.Comment.Postgres;

/// <summary>Configuration options for PostgreSQL comment store.</summary>
public sealed class PostgresCommentOptions : IPostgresMigrationConfig
{
    public const string SectionName = "PostgresComment";
    public const string Schema = "comment";

    /// <summary>Gets or sets the PostgreSQL connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets whether to enable automatic database migrations on startup.</summary>
    public bool EnableAutoMigrations { get; set; } = false;

    /// <inheritdoc />
    string IPostgresMigrationConfig.Schema => Schema;
}