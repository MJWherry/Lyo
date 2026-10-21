using Lyo.Story.Sqlite.Tests;
using Lyo.Story.Tests;

[assembly: AssemblyFixture(typeof(StorySqliteFixture))]

namespace Lyo.Story.Sqlite.Tests;

public sealed class SqliteStoryStoreTests(StorySqliteFixture fixture) : StoryStoreContractTests
{
    protected override IServiceProvider Services => fixture.ServiceProvider;
}