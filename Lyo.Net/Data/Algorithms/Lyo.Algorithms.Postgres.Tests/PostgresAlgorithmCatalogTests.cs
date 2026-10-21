using Lyo.Algorithms.Tests;

namespace Lyo.Algorithms.Postgres.Tests;

public sealed class PostgresAlgorithmCatalogTests(AlgorithmPostgresFixture fixture) : AlgorithmCatalogContractTests
{
    protected override IServiceProvider Services => fixture.ServiceProvider;
}