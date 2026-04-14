using Lyo.Postgres;

namespace Lyo.Audit.Postgres;

/// <summary>Configuration options for PostgreSQL audit recorder.</summary>
public sealed class PostgresAuditOptions : IPostgresMigrationConfig
{
    public const string SectionName = "PostgresAudit";
    public const string Schema = "audit";

    /// <summary>Gets or sets the PostgreSQL connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets whether to enable automatic database migrations on startup.</summary>
    public bool EnableAutoMigrations { get; set; } = false;

    /// <inheritdoc />
    string IPostgresMigrationConfig.Schema => Schema;
}