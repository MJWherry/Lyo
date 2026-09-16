using Lyo.Story.Tests;

namespace Lyo.Story.Postgres.Tests;

public sealed class PostgresStoryStoreTests(StoryPostgresFixture fixture) : StoryStoreContractTests
{
    protected override IServiceProvider Services => fixture.ServiceProvider;
}
