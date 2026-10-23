using Lyo.Podcast.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Podcast.Postgres.Tests;

public sealed class PodcastPostgresFixture : PostgresServiceFixtureBase<PodcastDbContext>
{
    protected override LogLevel MinimumLogLevel => LogLevel.Warning;

    protected override void ConfigureServices(IServiceCollection services, string connectionString)
        => services.AddPostgresPodcastStore(new PostgresPodcastOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
}