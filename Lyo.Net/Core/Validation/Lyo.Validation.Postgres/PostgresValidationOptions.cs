using Lyo.Postgres;

namespace Lyo.Validation.Postgres;

/// <summary>Settings for PostgreSQL validation schema storage.</summary>
public sealed class PostgresValidationOptions : PostgresOptionsBase
{
    /// <summary>Name of the configuration section.</summary>
    public const string SectionName = "PostgresValidation";

    /// <summary>PostgreSQL schema that holds validation tables.</summary>
    public const string Schema = "validation";

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}
