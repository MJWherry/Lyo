using Lyo.Postgres;

namespace Lyo.FantasyFootball.Postgres;

/// <summary>Configuration for the fantasy football PostgreSQL schema.</summary>
public sealed class PostgresFantasyFootballOptions : PostgresOptionsBase
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "PostgresFantasyFootball";

    /// <summary>PostgreSQL schema.</summary>
    public const string Schema = "fantasy_football";

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}
