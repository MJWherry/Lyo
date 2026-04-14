using Lyo.Postgres;

namespace Lyo.ContactUs.Postgres;

/// <summary>Configuration options for PostgreSQL contact form service.</summary>
public sealed class PostgresContactUsOptions : IPostgresMigrationConfig
{
    public const string SectionName = "PostgresContactUs";
    public const string Schema = "contact";

    /// <summary>Gets or sets the PostgreSQL connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets whether to enable automatic database migrations on startup.</summary>
    public bool EnableAutoMigrations { get; set; } = false;

    /// <inheritdoc />
    string IPostgresMigrationConfig.Schema => Schema;
}