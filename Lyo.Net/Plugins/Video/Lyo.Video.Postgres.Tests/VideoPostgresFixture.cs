using Lyo.Testing.Containers;
using Lyo.Video.Postgres.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Video.Postgres.Tests;

public sealed class VideoPostgresFixture : PostgresServiceFixtureBase<VideoDbContext>
{
    protected override LogLevel MinimumLogLevel => LogLevel.Warning;

    protected override void ConfigureServices(IServiceCollection services, string connectionString)
        => services.AddPostgresVideoStore(new PostgresVideoOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
}