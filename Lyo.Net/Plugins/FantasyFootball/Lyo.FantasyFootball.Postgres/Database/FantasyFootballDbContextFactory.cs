using Lyo.Postgres;
using Microsoft.EntityFrameworkCore.Design;

namespace Lyo.FantasyFootball.Postgres.Database;

/// <summary>Design-time factory for EF Core migrations.</summary>
public sealed class FantasyFootballDbContextFactory : IDesignTimeDbContextFactory<FantasyFootballDbContext>
{
    /// <inheritdoc />
    public FantasyFootballDbContext CreateDbContext(string[] args)
        => new(PostgresDesignTime.CreateOptions<FantasyFootballDbContext>(
            "FANTASY_FOOTBALL_CONNECTION_STRING",
            PostgresFantasyFootballOptions.Schema,
            "Host=localhost;Port=5432;Database=postgres;Username=user;Password=password"));
}
