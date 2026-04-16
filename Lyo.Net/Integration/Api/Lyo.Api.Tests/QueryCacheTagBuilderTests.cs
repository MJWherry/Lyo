using Lyo.Api.Services.Crud.Read.Query;

namespace Lyo.Api.Tests;

public sealed class QueryCacheTagBuilderTests
{
    [Fact]
    public void EntityTypeTag_UsesLowercaseClrName()
    {
        Assert.Equal("entity:querycachetagbuildertests", QueryCacheTagBuilder.EntityTypeTag(typeof(QueryCacheTagBuilderTests)));
    }

    [Fact]
    public void FormatPrimaryKeySegment_JoinsCompositeKeysWithPipe()
    {
        Assert.Equal("1|a", QueryCacheTagBuilder.FormatPrimaryKeySegment([1, "a"]));
        Assert.Equal("null", QueryCacheTagBuilder.FormatPrimaryKeySegment([null]));
    }

    [Fact]
    public void EntityInstanceTag_AppendsSegmentAfterEntityTypeTag()
    {
        var t = typeof(QueryCacheTagBuilderTests);
        Assert.Equal("entity:querycachetagbuildertests:42", QueryCacheTagBuilder.EntityInstanceTag(t, [42]));
    }

    [Fact]
    public void QueryScopeTag_IsQueries()
    {
        Assert.Equal("queries", QueryCacheTagBuilder.QueryScopeTag);
    }
}
