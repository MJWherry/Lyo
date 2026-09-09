using Lyo.FileSystemWatcher.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.FileSystemWatcher.Postgres.Tests;

public sealed class FileSystemWatcherPostgresFixture : PostgresServiceFixtureBase<FileSystemWatcherDbContext>
{
    protected override LogLevel MinimumLogLevel => LogLevel.Warning;

    protected override void ConfigureServices(IServiceCollection services, string connectionString)
        => services.AddPostgresFileSystemWatcherStore(new PostgresFileSystemWatcherOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
}
