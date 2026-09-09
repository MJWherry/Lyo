using Lyo.Tag;
using Lyo.EntityReference.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Tag.Postgres.Tests;

[Trait("Category", "Fast")]
public sealed class TagPostgresStoreTests(TagPostgresFixture fixture)
{
    [Fact]
    public async Task AddTag_ThenGetTagsForEntity_RoundTrips()
    {
        var ct = TestContext.Current.CancellationToken;
        var store = fixture.ServiceProvider.GetRequiredService<ITagStore>();
        var subject = EntityRef.ForGuid("thing", Guid.NewGuid());
        await store.AddTagAsync(subject, "blue", ct: ct);
        var tags = await store.GetTagsForEntityAsync(subject, ct: ct);
        var tag = Assert.Single(tags);
        Assert.Equal("blue", tag.Name);
    }
}
