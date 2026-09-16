using Lyo.Authorization.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Authorization.Postgres.Tests;

public sealed class AuthorizationPostgresFixture : PostgresServiceFixtureBase<AuthorizationDbContext>
{
    public PostgresGrantStore Store => ServiceProvider.GetRequiredService<PostgresGrantStore>();

    protected override void ConfigureServices(IServiceCollection services, string connectionString)
    {
        services.AddPostgresAuthorization(new PostgresAuthorizationOptions {
            ConnectionString = connectionString,
            EnableAutoMigrations = true
        });
    }
}
