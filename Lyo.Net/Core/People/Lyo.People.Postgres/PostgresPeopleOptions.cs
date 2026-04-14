using Lyo.Postgres;

namespace Lyo.People.Postgres;

/// <summary>Configuration options for PostgreSQL People persistence.</summary>
public sealed class PostgresPeopleOptions : IPostgresMigrationConfig
{
    public const string SectionName = "PostgresPeople";
    public const string Schema = "people";

    /// <summary>Gets or sets the PostgreSQL connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets whether to enable automatic database migrations on startup.</summary>
    public bool EnableAutoMigrations { get; set; } = false;

    /// <inheritdoc />
    string IPostgresMigrationConfig.Schema => Schema;
}