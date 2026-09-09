using Lyo.Geolocation.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Geolocation.Postgres.Tests;

public sealed class GeolocationPostgresFixture : PostgresServiceFixtureBase<GeolocationDbContext>
{
    protected override void ConfigureServices(IServiceCollection services, string connectionString) =>
        services.AddPostgresGeolocationStore(new PostgresGeolocationOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
}
