using Lyo.Postgres;

namespace Lyo.ShortUrl.Postgres;

/// <summary>Configuration options for PostgreSQL URL shortener service.</summary>
public sealed class PostgresShortUrlOptions : IPostgresMigrationConfig
{
    public const string SectionName = "PostgresShortUrl";
    public const string Schema = "url";

    /// <summary>Gets or sets the PostgreSQL connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets whether to enable automatic database migrations on startup.</summary>
    public bool EnableAutoMigrations { get; set; } = false;

    /// <inheritdoc />
    string IPostgresMigrationConfig.Schema => Schema;
}