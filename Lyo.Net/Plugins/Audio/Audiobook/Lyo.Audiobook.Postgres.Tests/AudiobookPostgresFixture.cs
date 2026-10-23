using Lyo.Audiobook.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Audiobook.Postgres.Tests;

public sealed class AudiobookPostgresFixture : PostgresServiceFixtureBase<AudiobookDbContext>
{
    protected override LogLevel MinimumLogLevel => LogLevel.Warning;

    protected override void ConfigureServices(IServiceCollection services, string connectionString)
        => services.AddPostgresAudiobookStore(new PostgresAudiobookOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
}