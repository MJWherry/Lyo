using Lyo.Postgres;

namespace Lyo.Law.Postgres;

/// <summary>Settings for the state-statute PostgreSQL schema.</summary>
public sealed class PostgresLawOptions : PostgresOptionsBase
{
    /// <summary>Configuration section this type binds from.</summary>
    public const string SectionName = "PostgresLaw";

    /// <summary>PostgreSQL schema that owns state statute tables.</summary>
    public const string Schema = "law";

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}