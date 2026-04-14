using Lyo.Postgres;

namespace Lyo.Note.Postgres;

/// <summary>Configuration options for PostgreSQL note store.</summary>
public sealed class PostgresNoteOptions : IPostgresMigrationConfig
{
    public const string SectionName = "PostgresNote";
    public const string Schema = "note";

    /// <summary>Gets or sets the PostgreSQL connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets whether to enable automatic database migrations on startup.</summary>
    public bool EnableAutoMigrations { get; set; } = false;

    /// <inheritdoc />
    string IPostgresMigrationConfig.Schema => Schema;
}