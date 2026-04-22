using Lyo.Postgres;

namespace Lyo.Comic.Postgres;

/// <summary>Configuration options for the PostgreSQL comic store.</summary>
public sealed class PostgresComicOptions : IPostgresMigrationConfig
{
    public const string SectionName = "PostgresComic";
    public const string Schema = "comic";

    /// <summary>Gets or sets the PostgreSQL connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets whether to apply EF migrations automatically on host startup.</summary>
    public bool EnableAutoMigrations { get; set; } = false;

    /// <inheritdoc />
    string IPostgresMigrationConfig.Schema => Schema;
}
