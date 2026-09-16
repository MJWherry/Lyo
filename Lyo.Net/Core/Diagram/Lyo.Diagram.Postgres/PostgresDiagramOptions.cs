using Lyo.Postgres;

namespace Lyo.Diagram.Postgres;

/// <summary>Settings for PostgreSQL diagram storage.</summary>
public sealed class PostgresDiagramOptions : PostgresOptionsBase
{
    /// <summary>Name of the configuration section.</summary>
    public const string SectionName = "PostgresDiagram";

    /// <summary>PostgreSQL schema that holds diagram tables.</summary>
    public const string Schema = "diagram";

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}
