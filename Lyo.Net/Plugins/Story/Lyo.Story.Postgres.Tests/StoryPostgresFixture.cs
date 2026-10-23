using Lyo.Story.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Story.Postgres.Tests;

public sealed class StoryPostgresFixture : PostgresServiceFixtureBase<StoryDbContext>
{
    protected override LogLevel MinimumLogLevel => LogLevel.Warning;

    protected override void ConfigureServices(IServiceCollection services, string connectionString)
        => services.AddPostgresStoryStore(new PostgresStoryOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
}