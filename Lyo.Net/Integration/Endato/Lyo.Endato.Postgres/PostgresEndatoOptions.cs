using Lyo.Postgres;

namespace Lyo.Endato.Postgres;

/// <summary>Configuration options for PostgreSQL Endato persistence.</summary>
public sealed class PostgresEndatoOptions : IPostgresMigrationConfig
{
    public const string SectionName = "PostgresEndato";
    public const string Schema = "endato";

    /// <summary>Gets or sets the PostgreSQL connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets whether to enable automatic database migrations on startup.</summary>
    public bool EnableAutoMigrations { get; set; } = false;

    /// <inheritdoc />
    string IPostgresMigrationConfig.Schema => Schema;
}