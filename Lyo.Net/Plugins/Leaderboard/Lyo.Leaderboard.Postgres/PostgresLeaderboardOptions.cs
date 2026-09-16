using Lyo.Postgres;

namespace Lyo.Leaderboard.Postgres;

/// <summary>Configuration for the leaderboard PostgreSQL schema.</summary>
public sealed class PostgresLeaderboardOptions : PostgresOptionsBase
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "PostgresLeaderboard";

    /// <summary>PostgreSQL schema.</summary>
    public const string Schema = "leaderboard";

    /// <inheritdoc />
    protected override string SchemaName => Schema;
}
