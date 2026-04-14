using Lyo.Common.Enums;
using Lyo.Query.Models.Attributes;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;

namespace Lyo.Query.Tests;

/// <summary>Tests that QueryPropertyNameAttribute allows saved query paths (e.g. "DocketCharges") to resolve to C# properties with different names (e.g. "Charges").</summary>
public class QueryPropertyNameTests : WhereClauseServiceTests
{
    [Fact]
    public void ApplyWhereClause_ResolvesViaQueryPropertyNameAttribute_Equals()
    {
        var svc = CreateService();
        var entities = new List<EntityWithQueryProp> { new() { Charges = "fee1" }, new() { Charges = "fee2" }, new() { Charges = "other" } }.AsQueryable();

        // Query uses saved path "DocketCharges" (canonical name from DB/other system)
        var node = WhereClauseBuilder.Condition("DocketCharges", ComparisonOperatorEnum.Equals, "fee1");
        var res = svc.ApplyWhereClause(entities, node).ToList();
        Assert.Single(res);
        Assert.Equal("fee1", res[0].Charges);
    }

    [Fact]
    public void ApplyWhereClause_DirectPropertyName_StillWorks()
    {
        var svc = CreateService();
        var entities = new List<EntityWithQueryProp> { new() { Charges = "fee1" }, new() { Charges = "fee2" } }.AsQueryable();
        var node = WhereClauseBuilder.Condition("Charges", ComparisonOperatorEnum.Equals, "fee1");
        var res = svc.ApplyWhereClause(entities, node).ToList();
        Assert.Single(res);
        Assert.Equal("fee1", res[0].Charges);
    }

    [Fact]
    public void ApplyWhereClause_Contains_ResolvesViaQueryPropertyNameAttribute()
    {
        var svc = CreateService();
        var entities = new List<EntityWithQueryProp> { new() { Charges = "fee-one" }, new() { Charges = "fee-two" }, new() { Charges = "other" } }.AsQueryable();
        var node = WhereClauseBuilder.Condition("DocketCharges", ComparisonOperatorEnum.Contains, "fee");
        var res = svc.ApplyWhereClause(entities, node).ToList();
        Assert.Equal(2, res.Count);
    }

    [Fact]
    public void ApplyWhereClause_In_ResolvesViaQueryPropertyNameAttribute()
    {
        var svc = CreateService();
        var entities = new List<EntityWithQueryProp> { new() { Charges = "a" }, new() { Charges = "b" }, new() { Charges = "c" } }.AsQueryable();
        var node = WhereClauseBuilder.Condition("DocketCharges", ComparisonOperatorEnum.In, "a,c");
        var res = svc.ApplyWhereClause(entities, node).ToList();
        Assert.Equal(2, res.Count);
        Assert.Contains(res, r => r.Charges == "a");
        Assert.Contains(res, r => r.Charges == "c");
    }

    [Fact]
    public void ApplyWhereClause_ResolvesViaQueryPropertyNameAttribute()
    {
        var svc = CreateService();
        var entities = new List<EntityWithQueryProp> { new() { Charges = "A" }, new() { Charges = "B" } }.AsQueryable();
        var node = WhereClauseBuilder.Condition("DocketCharges", ComparisonOperatorEnum.Equals, "A");
        var res = svc.ApplyWhereClause(entities, node).ToList();
        Assert.Single(res);
        Assert.Equal("A", res[0].Charges);
    }

    [Fact]
    public void ApplyWhereClause_AndOr_ResolvesViaQueryPropertyNameAttribute()
    {
        var svc = CreateService();
        var entities = new List<EntityWithQueryProp> { new() { Charges = "match", Count = 5 }, new() { Charges = "match", Count = 3 }, new() { Charges = "other", Count = 10 } }
            .AsQueryable();

        var node = WhereClauseBuilder.And(b => {
            b.Equals("DocketCharges", "match");
            b.GreaterThanOrEqual("Count", 5);
        });

        var res = svc.ApplyWhereClause(entities, node).ToList();
        Assert.Single(res);
        Assert.Equal(5, res[0].Count);
    }

    [Fact]
    public void MatchesWhereClause_ResolvesViaQueryPropertyNameAttribute()
    {
        var svc = CreateService();
        var entity = new EntityWithQueryProp { Charges = "matched" };
        var node = WhereClauseBuilder.Condition("DocketCharges", ComparisonOperatorEnum.Equals, "matched");
        Assert.True(svc.MatchesWhereClause(entity, node));
    }

    [Fact]
    public void MatchesWhereClause_ReturnsFalse_WhenQueryPropertyNameValueDoesNotMatch()
    {
        var svc = CreateService();
        var entity = new EntityWithQueryProp { Charges = "other" };
        var node = WhereClauseBuilder.Condition("DocketCharges", ComparisonOperatorEnum.Equals, "matched");
        Assert.False(svc.MatchesWhereClause(entity, node));
    }

    [Fact]
    public void SortByProperty_ResolvesViaQueryPropertyNameAttribute()
    {
        var svc = CreateService();
        var entities = new List<EntityWithQueryProp> { new() { Charges = "z" }, new() { Charges = "a" }, new() { Charges = "m" } }.AsQueryable();
        var res = svc.SortByProperty(entities, "DocketCharges", SortDirection.Asc).ToList();
        Assert.Equal(["a", "m", "z"], res.Select(e => e.Charges));
    }

    [Fact]
    public void SortByProperty_DirectPropertyName_StillWorks()
    {
        var svc = CreateService();
        var entities = new List<EntityWithQueryProp> { new() { Charges = "z" }, new() { Charges = "a" } }.AsQueryable();
        var res = svc.SortByProperty(entities, "Charges", SortDirection.Asc).ToList();
        Assert.Equal(["a", "z"], res.Select(e => e.Charges));
    }

    [Fact]
    public void ApplyOrdering_ResolvesViaQueryPropertyNameAttribute()
    {
        var svc = CreateService();
        var entities = new List<EntityWithQueryProp> { new() { Charges = "b", Count = 1 }, new() { Charges = "a", Count = 2 }, new() { Charges = "a", Count = 1 } }.AsQueryable();
        var sortProps = new[] { new SortBy("DocketCharges", SortDirection.Asc, 1), new SortBy("Count", SortDirection.Asc, 2) };
        var res = svc.ApplyOrdering(entities, sortProps, e => e.Charges, SortDirection.Asc).ToList();
        Assert.Equal("a", res[0].Charges);
        Assert.Equal(1, res[0].Count);
        Assert.Equal("a", res[1].Charges);
        Assert.Equal(2, res[1].Count);
        Assert.Equal("b", res[2].Charges);
    }

    [Fact]
    public void WhereClauseBuilder_ForT_OutputsQueryPropertyName_InCondition()
    {
        var b = WhereClauseBuilder.And();
        b.For<EntityWithQueryProp>().AddEquals(e => e.Charges, "x");
        var node = b.Build();
        var str = node.ToString();
        Assert.Contains("DocketCharges", str);
    }

    [Fact]
    public void Resolution_IsCaseInsensitive()
    {
        var svc = CreateService();
        var entities = new List<EntityWithQueryProp> { new() { Charges = "match" } }.AsQueryable();

        // Attribute value is "DocketCharges" - ensure "docketcharges" (lowercase) still resolves
        var node = WhereClauseBuilder.Condition("docketcharges", ComparisonOperatorEnum.Equals, "match");
        var res = svc.ApplyWhereClause(entities, node).ToList();
        Assert.Single(res);
    }

    private sealed class EntityWithQueryProp
    {
        [QueryPropertyName("DocketCharges")]
        public string Charges { get; set; } = "";

        public int Count { get; set; }
    }
}