using Lyo.Story.Tests;

[assembly: AssemblyFixture(typeof(Lyo.Story.Sqlite.Tests.StorySqliteFixture))]

namespace Lyo.Story.Sqlite.Tests;

public sealed class SqliteStoryStoreTests(StorySqliteFixture fixture) : StoryStoreContractTests
{
    protected override IServiceProvider Services => fixture.ServiceProvider;
}
