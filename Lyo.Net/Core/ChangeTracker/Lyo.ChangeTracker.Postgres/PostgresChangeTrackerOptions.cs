using Lyo.Postgres;

namespace Lyo.ChangeTracker.Postgres;

/// <summary>Configuration options for PostgreSQL change tracking.</summary>
public sealed class PostgresChangeTrackerOptions : IPostgresMigrationConfig
{
    public const string SectionName = "PostgresChangeTracker";
    public const string Schema = "change_tracker";

    /// <summary>Gets or sets the PostgreSQL connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets whether to enable automatic database migrations on startup.</summary>
    public bool EnableAutoMigrations { get; set; }

    string IPostgresMigrationConfig.Schema => Schema;
}