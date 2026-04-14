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
    public void QueryReqBuilder_AddQuery_BuildsNode()
    {
        var node = WhereClauseBuilder.And(b => b.Equals("Name", "Joe"));
        var qr = QueryReqBuilder.New().AddQuery(node).Build();
        Assert.NotNull(qr.WhereClause);
        Assert.Contains("Name", qr.WhereClause!.ToString());
    }

    [Fact]
    public void QueryReqBuilder_AddQuery_WithBuilderFunc()
    {
        var qr = QueryReqBuilder.New().AddQuery(b => b.AddCondition("Name", ComparisonOperatorEnum.Equals, "Joe").AddAnd(inner => inner.Equals("Status", "Active"))).Build();
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
    public void QueryReqBuilder_ForT_AddQuery_UsesQueryPropertyNameAttribute()
    {
        var builder = QueryReqBuilder.New().For<TestEntityWithQueryProp>();
        builder.AddQuery(q => q.AddEquals(e => e.Charges, "x"));
        var qr = builder.Done().Build();
        Assert.NotNull(qr.WhereClause);
        Assert.Contains("DocketCharges", qr.WhereClause!.ToString());
    }

    [Fact]
    public void QueryReqBuilder_ForT_AddQuery_BuildsNode()
    {
        var builder = QueryReqBuilder.New().For<Person>();
        builder.AddQuery(q => q.AddEquals(p => p.Name, "Zoe"));
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