using Lyo.People.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.People.Postgres.Tests;

public sealed class PeoplePostgresFixture : PostgresServiceFixtureBase<PeopleDbContext>
{
    protected override void ConfigureServices(IServiceCollection services, string connectionString) =>
        services.AddPeopleDbContextFactory(new PostgresPeopleOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
}
