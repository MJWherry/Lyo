using Lyo.Endato.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Endato.Postgres.Tests;

public sealed class EndatoPostgresFixture : PostgresServiceFixtureBase<EndatoDbContext>
{
    protected override void ConfigureServices(IServiceCollection services, string connectionString) =>
        services.AddEndatoDbContextFactory(new PostgresEndatoOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
}
