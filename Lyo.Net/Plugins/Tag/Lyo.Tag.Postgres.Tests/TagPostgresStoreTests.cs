using Lyo.Common.Core.Enums;
using Lyo.Common.Core.Identifiers;
using Lyo.EntityReference.Models;
using Lyo.Exceptions.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Tag.Postgres.Tests;

[Trait("Category", "Fast")]
public sealed class TagPostgresStoreTests(TagPostgresFixture fixture)
{
    [Fact]
    public async Task AddTag_ThenGetTagsForEntity_RoundTrips()
    {
        var ct = TestCt;
        var store = fixture.ServiceProvider.GetRequiredService<ITagStore>();
        var subject = EntityRef.ForGuid("thing", LyoGuid.CreateV7());
        await store.AddTagAsync(subject, "blue", ct: ct);
        var tags = await store.GetTagsForEntityAsync(subject, ct: ct);
        var tag = Assert.Single(tags);
        Assert.Equal("blue", tag.Name);
    }

    [Fact]
    public async Task GetTopTags_SortByCountDesc_HighestFirst()
    {
        var ct = TestCt;
        var store = Store();
        var entityType = EntityType();
        await AddManyAsync(store, entityType, "beta", 3, ct);
        await AddManyAsync(store, entityType, "alpha", 2, ct);
        await AddManyAsync(store, entityType, "gamma", 1, ct);
        var top = await store.GetTopTagsAsync(2, TagSort.Count, SortDirection.Desc, entityType, ct: ct);
        Assert.Equal(2, top.Count);
        Assert.Equal(("beta", 3), (top[0].Name, top[0].Count));
        Assert.Equal(("alpha", 2), (top[1].Name, top[1].Count));
        var tieType = EntityType();
        await AddManyAsync(store, tieType, "cedar", 2, ct);
        await AddManyAsync(store, tieType, "alder", 2, ct);
        var ties = await store.GetTopTagsAsync(2, TagSort.Count, SortDirection.Desc, tieType, ct: ct);
        Assert.Equal(["alder", "cedar"], ties.Select(t => t.Name));
    }

    [Fact]
    public async Task GetTopTags_SortByCountAsc_LowestFirst()
    {
        var ct = TestCt;
        var store = Store();
        var entityType = EntityType();
        await AddManyAsync(store, entityType, "beta", 3, ct);
        await AddManyAsync(store, entityType, "alpha", 2, ct);
        await AddManyAsync(store, entityType, "gamma", 1, ct);
        var top = await store.GetTopTagsAsync(2, TagSort.Count, SortDirection.Asc, entityType, ct: ct);
        Assert.Equal(("gamma", 1), (top[0].Name, top[0].Count));
        Assert.Equal(("alpha", 2), (top[1].Name, top[1].Count));
    }

    [Fact]
    public async Task GetTopTags_SortByNameAsc_Alphabetical()
    {
        var ct = TestCt;
        var store = Store();
        var entityType = EntityType();
        await AddManyAsync(store, entityType, "zeta", 1, ct);
        await AddManyAsync(store, entityType, "alpha", 3, ct);
        await AddManyAsync(store, entityType, "mid", 2, ct);
        var top = await store.GetTopTagsAsync(10, TagSort.Name, SortDirection.Asc, entityType, ct: ct);
        Assert.Equal(["alpha", "mid", "zeta"], top.Select(t => t.Name));
    }

    [Fact]
    public async Task GetTopTags_SortByNameDesc_ReverseAlphabetical()
    {
        var ct = TestCt;
        var store = Store();
        var entityType = EntityType();
        await AddManyAsync(store, entityType, "zeta", 1, ct);
        await AddManyAsync(store, entityType, "alpha", 3, ct);
        await AddManyAsync(store, entityType, "mid", 2, ct);
        var top = await store.GetTopTagsAsync(10, TagSort.Name, SortDirection.Desc, entityType, ct: ct);
        Assert.Equal(["zeta", "mid", "alpha"], top.Select(t => t.Name));
    }

    [Fact]
    public async Task GetTopTags_TagTypeAndEntityType_FilterTheGroup()
    {
        var ct = TestCt;
        var store = Store();
        var typeA = EntityType();
        var typeB = EntityType();
        await AddManyAsync(store, typeA, "shared", 2, ct);
        await AddManyAsync(store, typeA, "shared", 1, ct, "label");
        await AddManyAsync(store, typeB, "shared", 4, ct);
        var byType = await store.GetTopTagsAsync(10, forEntityType: typeA, ct: ct);
        Assert.Equal(2, byType.Count);
        Assert.Equal(2, byType.Single(t => t.TagType == "tag").Count);
        Assert.Equal(1, byType.Single(t => t.TagType == "label").Count);
        var labels = await store.GetTopTagsAsync(10, tagType: "label", forEntityType: typeA, ct: ct);
        var label = Assert.Single(labels);
        Assert.Equal(1, label.Count);
        var otherEntity = await store.GetTopTagsAsync(10, forEntityType: typeB, ct: ct);
        var row = Assert.Single(otherEntity);
        Assert.Equal(4, row.Count);
    }

    [Fact]
    public async Task GetTopTags_NameFilter_KeepsOnlyThatName()
    {
        var ct = TestCt;
        var store = Store();
        var entityType = EntityType();
        await AddManyAsync(store, entityType, "alpha", 2, ct);
        await AddManyAsync(store, entityType, "beta", 3, ct);
        var all = await store.GetTopTagsAsync(10, name: null, forEntityType: entityType, ct: ct);
        Assert.Equal(2, all.Count);
        var named = await store.GetTopTagsAsync(10, name: "alpha", forEntityType: entityType, ct: ct);
        var only = Assert.Single(named);
        Assert.Equal("alpha", only.Name);
        Assert.Equal(2, only.Count);
        var blank = await store.GetTopTagsAsync(10, name: "  ", forEntityType: entityType, ct: ct);
        Assert.Equal(2, blank.Count);
    }

    [Fact]
    public async Task GetTopTags_Text_MatchesNameOrTagType()
    {
        var ct = TestCt;
        var store = Store();
        var entityType = EntityType();
        await AddManyAsync(store, entityType, "alpha", 2, ct);
        await AddManyAsync(store, entityType, "alpine", 3, ct);
        await AddManyAsync(store, entityType, "beta", 4, ct);
        await AddManyAsync(store, entityType, "genre", 1, ct, "label");
        var byName = await store.GetTopTagsAsync(10, text: "alp", forEntityType: entityType, ct: ct);
        Assert.Equal(["alpine", "alpha"], byName.Select(t => t.Name).ToArray());
        var byType = await store.GetTopTagsAsync(10, text: "lab", forEntityType: entityType, ct: ct);
        var label = Assert.Single(byType);
        Assert.Equal("genre", label.Name);
        Assert.Equal("label", label.TagType);
        var all = await store.GetTopTagsAsync(10, text: null, forEntityType: entityType, ct: ct);
        Assert.Equal(4, all.Count);
    }

    [Fact]
    public async Task GetTopTags_SoftDeleted_ExcludedFromCount()
    {
        var ct = TestCt;
        var store = Store();
        var entityType = EntityType();
        var subject = EntityRef.ForGuid(entityType, LyoGuid.CreateV7());
        await store.AddTagAsync(subject, "gone", ct: ct);
        await store.AddTagAsync(EntityRef.ForGuid(entityType, LyoGuid.CreateV7()), "gone", ct: ct);
        var before = Assert.Single(await store.GetTopTagsAsync(10, forEntityType: entityType, ct: ct));
        Assert.Equal(2, before.Count);
        await store.RemoveTagAsync(subject, "gone", ct: ct);
        var after = Assert.Single(await store.GetTopTagsAsync(10, forEntityType: entityType, ct: ct));
        Assert.Equal(1, after.Count);
    }

    [Fact]
    public async Task GetTopTags_Skip_OffsetsTheWindow()
    {
        var ct = TestCt;
        var store = Store();
        var entityType = EntityType();
        await AddManyAsync(store, entityType, "beta", 3, ct);
        await AddManyAsync(store, entityType, "alpha", 2, ct);
        await AddManyAsync(store, entityType, "gamma", 1, ct);
        var page = await store.GetTopTagsAsync(1, skip: 1, forEntityType: entityType, ct: ct);
        var row = Assert.Single(page);
        Assert.Equal("alpha", row.Name);
        Assert.Equal(2, row.Count);
        var pastEnd = await store.GetTopTagsAsync(1, skip: 3, forEntityType: entityType, ct: ct);
        Assert.Empty(pastEnd);
    }

    [Fact]
    public async Task GetTopTags_TakeOutOfRange_Throws()
    {
        var store = Store();
        await Assert.ThrowsAsync<ArgumentOutsideRangeException>(() => store.GetTopTagsAsync(0, ct: TestCt));
    }

    [Fact]
    public async Task GetTopTags_SkipNegative_Throws()
    {
        var store = Store();
        await Assert.ThrowsAsync<ArgumentOutsideRangeException>(() => store.GetTopTagsAsync(1, skip: -1, ct: TestCt));
    }

    private ITagStore Store() => fixture.ServiceProvider.GetRequiredService<ITagStore>();

    private static string EntityType() => "top-" + LyoGuid.CreateV7().ToString("N");

    private static async Task AddManyAsync(ITagStore store, string entityType, string name, int count, CancellationToken ct, string tagType = "tag")
    {
        for (var i = 0; i < count; i++)
            await store.AddTagAsync(EntityRef.ForGuid(entityType, LyoGuid.CreateV7()), name, tagType, ct: ct);
    }
}