using Lyo.Postgres;

namespace Lyo.Web.Reporting.Postgres;

/// <summary>Configuration options for PostgreSQL reporting service.</summary>
public sealed class PostgresReportingOptions : IPostgresMigrationConfig
{
    public const string SectionName = "PostgresReporting";
    public const string Schema = "report";

    /// <summary>Gets or sets the PostgreSQL connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets whether to enable automatic database migrations on startup.</summary>
    public bool EnableAutoMigrations { get; set; } = false;

    /// <inheritdoc />
    string IPostgresMigrationConfig.Schema => Schema;
}