namespace Lyo.Sqlite;

/// <summary>Configuration for SQLite migrations used by <see cref="SqliteMigrationHostedService{TContext,TOptions}" />.</summary>
public interface ISqliteMigrationConfig
{
    /// <summary>Gets the SQLite connection string (typically a file path such as <c>Data Source=./filestore.db</c>).</summary>
    string ConnectionString { get; }

    /// <summary>Gets whether to run migrations when the host starts.</summary>
    bool EnableAutoMigrations { get; }
}
