using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.Leaderboard.Postgres.Database;

/// <summary>Design-time factory for EF Core migrations.</summary>
public sealed class LeaderboardDbContextFactory : IDesignTimeDbContextFactory<LeaderboardDbContext>
{
    /// <inheritdoc />
    public LeaderboardDbContext CreateDbContext(string[] args)
        => new(PostgresDesignTime.CreateOptions<LeaderboardDbContext>("LEADERBOARD_CONNECTION_STRING", PostgresLeaderboardOptions.Schema));
}