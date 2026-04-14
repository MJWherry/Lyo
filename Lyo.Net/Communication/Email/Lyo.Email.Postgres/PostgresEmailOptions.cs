using Lyo.Postgres;

namespace Lyo.Email.Postgres;

/// <summary>Configuration options for PostgreSQL email logging.</summary>
public sealed class PostgresEmailOptions : IPostgresMigrationConfig
{
    public const string SectionName = "PostgresEmail";
    public const string Schema = "email";

    /// <summary>Gets or sets the PostgreSQL connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets whether to enable automatic database migrations on startup.</summary>
    public bool EnableAutoMigrations { get; set; } = false;

    /// <inheritdoc />
    string IPostgresMigrationConfig.Schema => Schema;
}