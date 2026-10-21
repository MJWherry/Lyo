using Lyo.Algorithms.Postgres.Database;
using Lyo.Testing.Containers;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Algorithms.Postgres.Tests;

public sealed class AlgorithmPostgresFixture : PostgresServiceFixtureBase<AlgorithmDbContext>
{
    protected override void ConfigureServices(IServiceCollection services, string connectionString)
    {
        services.AddAlgorithms();
        services.AddPostgresAlgorithmStore(new PostgresAlgorithmOptions { ConnectionString = connectionString, EnableAutoMigrations = true });
    }
}