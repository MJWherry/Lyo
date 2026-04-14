using Lyo.Postgres;

namespace Lyo.Rating.Postgres;

/// <summary>Configuration options for PostgreSQL rating store.</summary>
public sealed class PostgresRatingOptions : IPostgresMigrationConfig
{
    public const string SectionName = "PostgresRating";
    public const string Schema = "rating";

    /// <summary>Gets or sets the PostgreSQL connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets whether to enable automatic database migrations on startup.</summary>
    public bool EnableAutoMigrations { get; set; } = false;

    /// <inheritdoc />
    string IPostgresMigrationConfig.Schema => Schema;
}