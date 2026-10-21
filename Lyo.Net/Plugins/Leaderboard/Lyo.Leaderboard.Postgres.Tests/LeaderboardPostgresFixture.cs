using Lyo.Algorithms;
using Lyo.Algorithms.Postgres;
using Lyo.Algorithms.Postgres.Database;
using Lyo.Leaderboard.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Leaderboard.Postgres.Tests;

/// <summary>Shared Testcontainers Postgres plus a migrated <see cref="LeaderboardDbContext" />.</summary>
public sealed class LeaderboardPostgresFixture : PostgresServiceFixtureBase<LeaderboardDbContext>
{
    protected override LogLevel MinimumLogLevel => LogLevel.Warning;

    protected override void ConfigureServices(IServiceCollection services, string connectionString)
    {
        services.AddPostgresAlgorithmStore(new PostgresAlgorithmOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
        services.AddAlgorithms();
        services.AddPostgresLeaderboardStore(new PostgresLeaderboardOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
    }

    /// <summary>The fixture base migrates the leaderboard context. The catalog lives in schema <c>algorithms</c> and needs its own migration.</summary>
    protected override async ValueTask MigrateAsync(CancellationToken cancellationToken)
    {
        await base.MigrateAsync(cancellationToken);
        var algorithms = ServiceProvider.GetRequiredService<IDbContextFactory<AlgorithmDbContext>>();
        await using var context = await algorithms.CreateDbContextAsync(cancellationToken);
        await context.Database.MigrateAsync(cancellationToken);
    }
}