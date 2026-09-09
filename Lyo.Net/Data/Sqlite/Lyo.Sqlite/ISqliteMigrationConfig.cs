namespace Lyo.Sqlite;

/// <summary>Settings <see cref="SqliteMigrationHostedService{TContext,TOptions}" /> reads before applying SQLite migrations.</summary>
public interface ISqliteMigrationConfig
{
    /// <summary>SQLite connection string, usually a file path such as <c>Data Source=./filestore.db</c>.</summary>
    string ConnectionString { get; }

    /// <summary>If true, migrations run when the host starts.</summary>
    bool EnableAutoMigrations { get; }
}