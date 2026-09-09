using Lyo.Favorite.Postgres;
using Lyo.Favorite.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lyo.Favorite.Postgres.Tests;

public sealed class FavoritePostgresFixture : PostgresServiceFixtureBase<FavoriteDbContext>
{
    protected override LogLevel MinimumLogLevel => LogLevel.Warning;

    protected override void ConfigureServices(IServiceCollection services, string connectionString)
        => services.AddPostgresFavoriteStore(new PostgresFavoriteOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
}
