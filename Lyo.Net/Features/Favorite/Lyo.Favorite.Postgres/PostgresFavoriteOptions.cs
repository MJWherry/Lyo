using Lyo.Postgres;

namespace Lyo.Favorite.Postgres;

/// <summary>Configuration options for PostgreSQL favorite store.</summary>
public sealed class PostgresFavoriteOptions : IPostgresMigrationConfig
{
    public const string SectionName = "PostgresFavorite";
    public const string Schema = "favorite";

    /// <summary>Gets or sets the PostgreSQL connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets whether to enable automatic database migrations on startup.</summary>
    public bool EnableAutoMigrations { get; set; } = false;

    /// <inheritdoc />
    string IPostgresMigrationConfig.Schema => Schema;
}