using Lyo.Api.Services.Crud.Read.Query;
using Lyo.Common.Core.Enums;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using WidgetA = Lyo.Api.Tests.Fixtures.NamespaceA.Widget;
using WidgetB = Lyo.Api.Tests.Fixtures.NamespaceB.Widget;

namespace Lyo.Api.Tests;

/// <summary>
/// Regression suite for cache-key collisions. Every dimension that changes the result set must change the key; anything omitted lets one request serve another request's rows.
/// </summary>
public sealed class QueryCacheKeyHardeningTests
{
    [Fact]
    public void FormatPrimaryKeySegment_ComponentsContainingSeparator_DoNotCollide()
        => Assert.NotEqual(QueryCacheTagBuilder.FormatPrimaryKeySegment(["a|b", "c"]), QueryCacheTagBuilder.FormatPrimaryKeySegment(["a", "b|c"]));

    [Fact]
    public void FormatPrimaryKeySegment_NullComponent_DoesNotCollideWithLiteralNullText()
        => Assert.NotEqual(QueryCacheTagBuilder.FormatPrimaryKeySegment([null]), QueryCacheTagBuilder.FormatPrimaryKeySegment(["null"]));

    [Fact]
    public void EntityTypeTag_TypesWithSameShortNameInDifferentNamespaces_DoNotCollide()
        => Assert.NotEqual(QueryCacheTagBuilder.EntityTypeTag(typeof(WidgetA)), QueryCacheTagBuilder.EntityTypeTag(typeof(WidgetB)));

    [Fact]
    public void BuildSingleEntityGetCacheKey_DifferentResponseTypes_ProduceDifferentKeys()
    {
        object[] pk = [42];
        var asEntity = QueryCacheKeyBuilder.BuildSingleEntityGetCacheKey(typeof(WidgetA), pk, responseType: typeof(WidgetA));
        var asDto = QueryCacheKeyBuilder.BuildSingleEntityGetCacheKey(typeof(WidgetA), pk, responseType: typeof(WidgetB));
        Assert.NotEqual(asEntity, asDto);
    }

    [Fact]
    public void BuildSingleEntityGetCacheKey_IncludeCasing_IsNormalized()
    {
        object[] pk = [42];
        var upper = QueryCacheKeyBuilder.BuildSingleEntityGetCacheKey(typeof(WidgetA), pk, ["JobRuns"]);
        var lower = QueryCacheKeyBuilder.BuildSingleEntityGetCacheKey(typeof(WidgetA), pk, ["jobruns"]);
        Assert.Equal(upper, lower);
    }

    /// <summary>Execution applies the caller's default order when SortBy is empty, so two callers with different defaults cannot share an entry.</summary>
    [Fact]
    public void BuildTree_DifferentDefaultSort_ProducesDifferentKeys()
        => Assert.NotEqual(BuildTreeKey("w => w.Name", SortDirection.Asc), BuildTreeKey("w => w.Id", SortDirection.Asc));

    [Fact]
    public void BuildTree_DifferentDefaultSortDirection_ProducesDifferentKeys()
        => Assert.NotEqual(BuildTreeKey("w => w.Name", SortDirection.Asc), BuildTreeKey("w => w.Name", SortDirection.Desc));

    [Fact]
    public void BuildTree_DifferentInClauseValues_ProduceDifferentKeys()
    {
        var g1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var g2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var first = BuildTreeKeyForClause(WhereClauseBuilder.Condition("Id", ComparisonOperatorEnum.In, new[] { g1 }));
        var second = BuildTreeKeyForClause(WhereClauseBuilder.Condition("Id", ComparisonOperatorEnum.In, new[] { g2 }));
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Build_KeysContainingSeparator_DoNotCollide() => Assert.NotEqual(BuildConcreteKey([["a|b", "c"]]), BuildConcreteKey([["a", "b|c"]]));

    private static string BuildTreeKey(string defaultSortExpression, SortDirection direction)
        => QueryCacheKeyBuilder.BuildTree<WidgetA, WidgetA>(
            null, 0, 10, [], [], QueryTotalCountMode.Exact, QueryIncludeFilterMode.Full, defaultSortKey: QueryCacheKeyBuilder.FormatDefaultSortKey(defaultSortExpression, direction));

    private static string BuildTreeKeyForClause(WhereClause clause)
        => QueryCacheKeyBuilder.BuildTree<WidgetA, WidgetA>(clause, 0, 10, [], [], QueryTotalCountMode.Exact, QueryIncludeFilterMode.Full);

    private static string BuildConcreteKey(IReadOnlyList<object[]> keys)
        => QueryCacheKeyBuilder.Build<WidgetA, WidgetA>(new QueryConcreteReq { Start = 0, Amount = 10, Keys = [.. keys] });
}
