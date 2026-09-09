using Lyo.Query.Models.Attributes;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;

namespace Lyo.Query.Tests;

public class QueryBuilderTests
{
    [Fact]
    public void WhereClauseBuilder_Builds_Logical()
    {
        var b = WhereClauseBuilder.And();
        b.Equals("A", 1).Contains("B", "x");
        var node = b.Build();
        Assert.NotNull(node);
        Assert.Contains("A", node.ToString());
    }

    [Fact]
    public void QueryReqBuilder_FromJoinSelect_BuildsRequest()
    {
        var qr = QueryReqBuilder.New()
            .From("o", "OrderEntity")
            .Join("p", "PersonEntity", JoinType.Left, on => on.Add(new() { From = "o.PersonId", To = "p.Id" }), "recipient")
            .AddSelects("o.Id", "p.FirstName")
            .SetPagination(0, 10)
            .Build();

        Assert.Equal("o", qr.From.Alias);
        Assert.Equal("OrderEntity", qr.From.EntityType);
        Assert.Single(qr.Joins);
        Assert.Equal("recipient", qr.Joins[0].As);
        Assert.Equal(2, qr.Select.Count);
    }

    [Fact]
    public void QueryConcreteReqBuilder_AddKey_AddKeys_AppendsRows()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();
        var qr = QueryConcreteReqBuilder.New().AddKey(id1).AddKey("tenant-a", 1).AddKeys([id2], [id3]).AddKeys([[42]]).Build();
        Assert.Equal(5, qr.Keys.Count);
        Assert.Equal(new object[] { id1 }, qr.Keys[0]);
        Assert.Equal(new object[] { "tenant-a", 1 }, qr.Keys[1]);
        Assert.Equal(new object[] { id2 }, qr.Keys[2]);
        Assert.Equal(new object[] { id3 }, qr.Keys[3]);
        Assert.Equal(new object[] { 42 }, qr.Keys[4]);
    }

    [Fact]
    public void ProjectionQueryReqBuilder_AddKey_AddKeys_AppendsRows()
    {
        var qr = ProjectionQueryReqBuilder.New().AddSelects("Id").AddKey(7).AddKeys(["tenant-b", 2], [8]).Build();
        Assert.Equal(3, qr.Keys.Count);
        Assert.Equal(new object[] { 7 }, qr.Keys[0]);
        Assert.Equal(new object[] { "tenant-b", 2 }, qr.Keys[1]);
        Assert.Equal(new object[] { 8 }, qr.Keys[2]);
    }

    [Fact]
    public void QueryReqBuilder_AddKey_AddKeys_AppendsRows()
    {
        var qr = QueryReqBuilder.New().From("o", "OrderEntity").AddSelects("o.Id").AddKey(9).AddKeys([10], [11]).Build();
        Assert.Equal(3, qr.Keys.Count);
        Assert.Equal(new object[] { 9 }, qr.Keys[0]);
        Assert.Equal(new object[] { 10 }, qr.Keys[1]);
        Assert.Equal(new object[] { 11 }, qr.Keys[2]);
    }

    [Fact]
    public void QueryConcreteReqBuilder_AddWhere_WithBuilderFunc()
    {
        var qr = QueryConcreteReqBuilder.New()
            .AddWhere(b => b.AddCondition("Name", ComparisonOperatorEnum.Equals, "Joe").AddAnd(inner => inner.Equals("Status", "Active")))
            .Build();

        Assert.NotNull(qr.WhereClause);
        Assert.Contains("Name", qr.WhereClause!.ToString());
        Assert.Contains("Status", qr.WhereClause.ToString());
    }

    [Fact]
    public void WhereClauseBuilder_AddSubClause_BuildsNode()
    {
        var node = WhereClauseBuilder.And().Equals("Age", 5).AddSubClause(sub => sub.AddAnd(subAnd => subAnd.Equals("Name", "B"))).Build();
        Assert.NotNull(node);
        var ln = Assert.IsType<GroupClause>(node);
        Assert.NotNull(ln.SubClause);
    }

    [Fact]
    public void WhereClauseBuilder_AddConditionWithSubClause_BuildsNode()
    {
        var node = WhereClauseBuilder.And().AddConditionWithSubClause("Age", ComparisonOperatorEnum.GreaterThan, 5, sub => sub.Equals("Name", "B")).Build();
        Assert.NotNull(node);
        var cond = Assert.Single(Assert.IsType<GroupClause>(node).Children);
        var c = Assert.IsType<ConditionClause>(cond);
        Assert.Equal("Age", c.Field);
        Assert.Equal(ComparisonOperatorEnum.GreaterThan, c.Comparison);
        Assert.Equal(5, c.Value);
        Assert.NotNull(c.SubClause);
    }

    [Fact]
    public void WhereClauseBuilder_ForT_AddConditionWithSubClause_BuildsNode()
    {
        var b = WhereClauseBuilder.And();
        b.For<Person>().AddConditionWithSubClause(p => p.Age, ComparisonOperatorEnum.GreaterThan, 5, sub => sub.AddEquals(p => p.Name, "B"));
        var node = b.Build();
        Assert.NotNull(node);
        var cond = Assert.Single(Assert.IsType<GroupClause>(node).Children);
        var c = Assert.IsType<ConditionClause>(cond);
        Assert.Equal("Age", c.Field);
        Assert.NotNull(c.SubClause);
    }

    [Fact]
    public void QueryConcreteReqBuilder_ForT_AddWhere_UsesQueryPropertyNameAttribute()
    {
        var builder = QueryConcreteReqBuilder.New().For<TestEntityWithQueryProp>();
        builder.AddWhere(q => q.AddEquals(e => e.Charges, "x"));
        var qr = builder.Done().Build();
        Assert.NotNull(qr.WhereClause);
        Assert.Contains("DocketCharges", qr.WhereClause!.ToString());
    }

    [Fact]
    public void QueryConcreteReqBuilder_ForT_AddWhere_BuildsNode()
    {
        var builder = QueryConcreteReqBuilder.New().For<Person>();
        builder.AddWhere(q => q.AddEquals(p => p.Name, "Zoe"));
        var qr = builder.Done().Build();
        Assert.NotNull(qr.WhereClause);
        Assert.Contains("Name", qr.WhereClause!.ToString());
    }

    [Fact]
    public void WhereClauseBuilder_ForT_TypedMethods()
    {
        var b = WhereClauseBuilder.And();
        var fb = b.For<Person>();
        fb.AddEquals(p => p.Name, "Alice");
        fb.Contains(p => p.Name, "Al");
        fb.In(p => p.Name, "Alice", "Bob");
        var node = b.Build();
        Assert.NotNull(node);
        Assert.Contains("Name", node.ToString());
    }

    [Fact]
    public void WhereClauseBuilder_ForT_NestedAndOr()
    {
        var b = WhereClauseBuilder.And();
        var fb = b.For<Person>();
        fb.AddEquals(p => p.Name, "X");
        fb.AddGroupAnd(a => a.AddEquals(p => p.Name, "Y"));
        fb.AddGroupOr(o => o.AddEquals(p => p.Name, "Z"));
        var node = b.Build();
        Assert.NotNull(node);
        Assert.IsType<GroupClause>(node);
        var ln = (GroupClause)node;
        Assert.Equal(GroupOperatorEnum.And, ln.Operator);
        Assert.NotNull(ln.Children);
        Assert.NotEmpty(ln.Children);
        Assert.Contains(ln.Children, c => c is GroupClause l && l.Operator is GroupOperatorEnum.And or GroupOperatorEnum.Or);
    }

    private sealed class TestEntityWithQueryProp
    {
        [QueryPropertyName("DocketCharges")]
        public string Charges { get; } = "";
    }
}