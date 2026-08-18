using Lyo.Postgres;

namespace Lyo.Validation.Postgres;

/// <summary>Configuration options for PostgreSQL validation schema storage.</summary>
public sealed class PostgresValidationOptions : IPostgresMigrationConfig
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "PostgresValidation";

    /// <summary>PostgreSQL schema that owns validation tables.</summary>
    public const string Schema = "validation";

    /// <summary>Gets or sets the PostgreSQL connection string.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets whether to run migrations when the host starts.</summary>
    public bool EnableAutoMigrations { get; set; }

    /// <inheritdoc />
    string IPostgresMigrationConfig.Schema => Schema;

    /// <summary>Throws when <see cref="ConnectionString" /> is missing.</summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            throw new ArgumentException($"{nameof(PostgresValidationOptions)}.{nameof(ConnectionString)} is required.", nameof(ConnectionString));
    }
}
