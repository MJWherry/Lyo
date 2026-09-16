using Lyo.Music.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Music.Postgres.Tests;

public sealed class MusicPostgresFixture : PostgresServiceFixtureBase<MusicDbContext>
{
    protected override LogLevel MinimumLogLevel => LogLevel.Warning;

    protected override void ConfigureServices(IServiceCollection services, string connectionString)
        => services.AddPostgresMusicStore(new PostgresMusicOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
}
