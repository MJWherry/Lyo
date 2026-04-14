using Lyo.Postgres;

namespace Lyo.Discord.Postgres;

/// <summary>Configuration options for the PostgreSQL Discord database.</summary>
public sealed class PostgresDiscordOptions : IPostgresMigrationConfig
{
    public const string SectionName = "PostgresDiscord";
    public const string Schema = "discord";

    /// <summary>Gets or sets the PostgreSQL connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets whether to enable automatic database migrations on startup.</summary>
    public bool EnableAutoMigrations { get; set; }

    /// <inheritdoc />
    string IPostgresMigrationConfig.Schema => Schema;
}