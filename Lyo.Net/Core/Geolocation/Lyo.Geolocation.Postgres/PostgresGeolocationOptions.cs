using Lyo.Postgres;

namespace Lyo.Geolocation.Postgres;

/// <summary>Configuration options for PostgreSQL geolocation store.</summary>
public sealed class PostgresGeolocationOptions : IPostgresMigrationConfig
{
    public const string SectionName = "PostgresGeolocation";
    public const string Schema = "geolocation";

    /// <summary>Gets or sets the PostgreSQL connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets whether to enable automatic database migrations on startup.</summary>
    public bool EnableAutoMigrations { get; set; }

    /// <inheritdoc />
    string IPostgresMigrationConfig.Schema => Schema;
}
