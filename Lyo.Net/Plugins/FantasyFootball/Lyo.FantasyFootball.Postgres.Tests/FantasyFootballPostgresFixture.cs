using Lyo.FantasyFootball.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.FantasyFootball.Postgres.Tests;

/// <summary>Shared Testcontainers Postgres plus a migrated fantasy football context.</summary>
public sealed class FantasyFootballPostgresFixture : PostgresServiceFixtureBase<FantasyFootballDbContext>
{
    protected override LogLevel MinimumLogLevel => LogLevel.Warning;

    protected override void ConfigureServices(IServiceCollection services, string connectionString)
        => services.AddPostgresFantasyFootballStore(new PostgresFantasyFootballOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
}