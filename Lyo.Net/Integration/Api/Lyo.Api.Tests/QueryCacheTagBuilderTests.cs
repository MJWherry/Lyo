using Lyo.Api.Services.Crud.Read.Project;
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

    [Fact]
    public void FormatProjShapeTag_IsStableAcrossSelectOrder()
    {
        var specs1 = new[] {
            new ProjectedFieldSpec("a", "Alpha", ["Alpha"]),
            new ProjectedFieldSpec("b", "Beta", ["Beta"]),
        };
        var specs2 = new[] {
            new ProjectedFieldSpec("b", "Beta", ["Beta"]),
            new ProjectedFieldSpec("a", "Alpha", ["Alpha"]),
        };
        var shape1 = QueryCacheTagBuilder.FormatProjShapeTag(specs1, [], false);
        var shape2 = QueryCacheTagBuilder.FormatProjShapeTag(specs2, [], false);
        Assert.Equal(shape1, shape2);
        Assert.StartsWith("projshape:", shape1, StringComparison.Ordinal);
    }

    [Fact]
    public void FormatProjShapeTag_ChangesWhenNormalizedPathSetDiffers()
    {
        var specsA = new[] { new ProjectedFieldSpec("a", "A", ["A"]) };
        var specsB = new[] { new ProjectedFieldSpec("b", "B", ["B"]) };
        Assert.NotEqual(
            QueryCacheTagBuilder.FormatProjShapeTag(specsA, [], false),
            QueryCacheTagBuilder.FormatProjShapeTag(specsB, [], false));
    }

    [Fact]
    public void BuildSingleEntityGetCacheKey_BaseMatchesEntityInstanceTag()
    {
        var t = typeof(QueryCacheTagBuilderTests);
        object?[] pk = [42];
        Assert.Equal(
            QueryCacheTagBuilder.EntityInstanceTag(t, pk),
            QueryCacheKeyBuilder.BuildSingleEntityGetCacheKey(t, pk, includes: null, rawResponse: false));
    }

    [Fact]
    public void BuildSingleEntityGetCacheKey_RawSuffix()
    {
        var t = typeof(QueryCacheTagBuilderTests);
        object?[] pk = [7];
        Assert.Equal(
            QueryCacheTagBuilder.EntityInstanceTag(t, pk) + ":raw",
            QueryCacheKeyBuilder.BuildSingleEntityGetCacheKey(t, pk, includes: null, rawResponse: true));
    }
}
