using Lyo.Postgres;

namespace Lyo.Job.Postgres;

/// <summary>Configuration options for PostgreSQL job management database.</summary>
public sealed class PostgresJobOptions : IPostgresMigrationConfig
{
    public const string SectionName = "PostgresJob";
    public const string Schema = "job";

    /// <summary>Gets or sets the PostgreSQL connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets whether to enable automatic database migrations on startup.</summary>
    public bool EnableAutoMigrations { get; set; } = false;

    /// <inheritdoc />
    string IPostgresMigrationConfig.Schema => Schema;

    /// <summary>Validates the options, throwing <see cref="ArgumentException" /> when invalid.</summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            throw new ArgumentException($"{nameof(PostgresJobOptions)}.{nameof(ConnectionString)} is required.", nameof(ConnectionString));
    }
}