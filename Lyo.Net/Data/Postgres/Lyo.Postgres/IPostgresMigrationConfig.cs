namespace Lyo.Postgres;

/// <summary>Settings the PostgreSQL migration hosted service reads before applying migrations.</summary>
public interface IPostgresMigrationConfig
{
    /// <summary>PostgreSQL connection string.</summary>
    string ConnectionString { get; }

    /// <summary>If true, migrations run when the host starts.</summary>
    bool EnableAutoMigrations { get; }

    /// <summary>Schema name for this package, fixed per package (for example <c>audit</c> or <c>email</c>).</summary>
    string Schema { get; }
}