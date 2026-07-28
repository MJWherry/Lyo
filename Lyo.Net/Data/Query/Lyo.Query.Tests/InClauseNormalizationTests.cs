using System.Text.Json;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;

namespace Lyo.Query.Tests;

/// <summary>
/// Regression tests for In/NotIn values passed as a collection. C# overload resolution picks the <c>params T[]</c> overload for <c>In(field, someList)</c> (with T inferred
/// as the list type), which used to wrap the collection in a single-element array and break server-side value conversion.
/// </summary>
public class InClauseNormalizationTests : WhereClauseServiceTests
{
    private static IQueryable<Person> People()
        => new List<Person> { new PersonBuilder().WithName("Alice").Build(), new PersonBuilder().WithName("Bob").Build(), new PersonBuilder().WithName("Carol").Build() }
            .AsQueryable();

    [Fact]
    public void In_Builder_WithList_UnwrapsToFlatValues()
    {
        var clause = WhereClauseBuilder.And(b => b.In("Name", new List<string> { "Alice", "Bob" }));
        var condition = Assert.IsType<ConditionClause>(Assert.IsType<GroupClause>(clause).Children![0]);
        var values = Assert.IsAssignableFrom<IEnumerable<string>>(condition.Value);
        Assert.Equal(["Alice", "Bob"], values);
    }

    [Fact]
    public void In_Builder_WithList_FiltersCorrectly()
    {
        var svc = CreateService();
        var clause = WhereClauseBuilder.And(b => b.In("Name", new List<string> { "Alice", "Bob" }));
        var res = svc.ApplyWhereClause(People(), clause).ToList();
        Assert.Equal(2, res.Count);
    }

    [Fact]
    public void In_Builder_WithParams_FiltersCorrectly()
    {
        var svc = CreateService();
        var clause = WhereClauseBuilder.And(b => b.In("Name", "Alice", "Bob"));
        var res = svc.ApplyWhereClause(People(), clause).ToList();
        Assert.Equal(2, res.Count);
    }

    [Fact]
    public void NotIn_Builder_WithList_FiltersCorrectly()
    {
        var svc = CreateService();
        var clause = WhereClauseBuilder.And(b => b.NotIn("Name", new List<string> { "Alice", "Bob" }));
        var res = svc.ApplyWhereClause(People(), clause).ToList();
        Assert.Single(res);
        Assert.Equal("Carol", res[0].Name);
    }

    [Fact]
    public void In_Builder_WithList_JsonRoundTrip_FiltersCorrectly()
    {
        var svc = CreateService();
        var clause = WhereClauseBuilder.And(b => b.In("Name", new List<string> { "Alice", "Bob" }));
        var roundTripped = JsonSerializer.Deserialize<WhereClause>(JsonSerializer.Serialize(clause))!;
        var res = svc.ApplyWhereClause(People(), roundTripped).ToList();
        Assert.Equal(2, res.Count);
    }

    [Fact]
    public void In_NestedJsonArray_IsFlattenedServerSide()
    {
        // Payload shape produced by clients built before the builder fix: value is [["Alice","Bob"]].
        var svc = CreateService();
        var nested = JsonSerializer.Deserialize<JsonElement>("""[["Alice","Bob"]]""");
        var clause = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.In, nested);
        var res = svc.ApplyWhereClause(People(), clause).ToList();
        Assert.Equal(2, res.Count);
    }

    [Fact]
    public void In_NestedInProcessEnumerable_IsFlattenedServerSide()
    {
        var svc = CreateService();
        var clause = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.In, new object[] { new List<string> { "Alice", "Bob" } });
        var res = svc.ApplyWhereClause(People(), clause).ToList();
        Assert.Equal(2, res.Count);
    }
}