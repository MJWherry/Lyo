namespace Lyo.Postgres;

/// <summary>Configuration for PostgreSQL migrations used by PostgresMigrationHostedService.</summary>
public interface IPostgresMigrationConfig
{
    /// <summary>Gets the PostgreSQL connection string.</summary>
    string ConnectionString { get; }

    /// <summary>Gets whether to run migrations when the host starts.</summary>
    bool EnableAutoMigrations { get; }

    /// <summary>Gets the schema name for migrations (hardcoded per package, e.g. "audit", "email").</summary>
    string Schema { get; }
}