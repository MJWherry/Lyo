using Lyo.Postgres;

namespace Lyo.Algorithms.Postgres;

/// <summary>Settings for the PostgreSQL algorithm catalog. Section <see cref="SectionName" />, schema <see cref="Schema" />.</summary>
public sealed class PostgresAlgorithmOptions : PostgresOptionsBase
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "PostgresAlgorithms";

    /// <summary>PostgreSQL schema that owns the catalog tables.</summary>
    public const string Schema = "algorithms";

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}
