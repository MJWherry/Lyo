using Lyo.Algorithms.Tests;

namespace Lyo.Algorithms.Sqlite.Tests;

public sealed class SqliteAlgorithmCatalogTests(AlgorithmSqliteFixture fixture) : AlgorithmCatalogContractTests
{
    protected override IServiceProvider Services => fixture.ServiceProvider;
}