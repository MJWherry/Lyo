using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;
using Lyo.Query.Models.Exceptions;
using Lyo.Query.Services.WhereClause;

namespace Lyo.Query.Tests;

public class FuzzyWhereClauseTests : WhereClauseServiceTests
{
    [Fact]
    public void Fuzzy_SimilarNames_MatchAtDefaultThreshold()
    {
        var svc = CreateService();
        var people = new List<Person> { new PersonBuilder().WithName("Alice").Build(), new PersonBuilder().WithName("Alicia").Build(), new PersonBuilder().WithName("Bob").Build() }
            .AsQueryable();

        var node = WhereClauseBuilder.And().Fuzzy("Name", "Alice").Build();
        var res = svc.ApplyWhereClause(people, node).Select(p => p.Name).ToList();
        Assert.Contains("Alice", res);
        Assert.DoesNotContain("Bob", res);
    }

    [Fact]
    public void NotFuzzy_ExcludesCloseMatches()
    {
        var svc = CreateService();
        var people = new List<Person> { new PersonBuilder().WithName("Alice").Build(), new PersonBuilder().WithName("Bob").Build() }.AsQueryable();
        var node = WhereClauseBuilder.And().NotFuzzy("Name", "Alice").Build();
        var res = svc.ApplyWhereClause(people, node).Select(p => p.Name).ToList();
        Assert.DoesNotContain("Alice", res);
        Assert.Contains("Bob", res);
    }

    [Fact]
    public void Fuzzy_ExplicitHighThreshold_DropsWeakerMatch()
    {
        var svc = CreateService();
        var people = new List<Person> { new PersonBuilder().WithName("Alice").Build(), new PersonBuilder().WithName("Alicia").Build() }.AsQueryable();
        var node = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.Fuzzy, "Alice", threshold: 0.99);
        var res = svc.ApplyWhereClause(people, node).Select(p => p.Name).ToList();
        Assert.Equal("Alice", Assert.Single(res));
    }

    [Fact]
    public void Fuzzy_InvalidThreshold_ThrowsInvalidQuery()
    {
        var svc = CreateService();
        var people = new List<Person> { new PersonBuilder().WithName("Alice").Build() }.AsQueryable();
        var node = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.Fuzzy, "Alice", threshold: 1.5);
        Assert.Throws<InvalidQueryException>(() => svc.ApplyWhereClause(people, node).ToList());
    }

    [Fact]
    public void Fuzzy_GuidField_UsesCanonicalStringForm()
    {
        var svc = CreateService();
        var g = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var people = new List<Person> { new PersonBuilder().WithId(g).WithName("A").Build() }.AsQueryable();
        var node = WhereClauseBuilder.Condition("Id", ComparisonOperatorEnum.Fuzzy, "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", threshold: 0.9);
        var res = svc.ApplyWhereClause(people, node).ToList();
        Assert.Single(res);
    }

    [Fact]
    public void GetWhereClauseTreeHash_FuzzyThreshold_ChangesFingerprint()
    {
        var a = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.Fuzzy, "Alice", threshold: 0.3);
        var b = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.Fuzzy, "Alice", threshold: 0.4);
        Assert.NotEqual(WhereClauseHelpers.GetWhereClauseTreeHash(a), WhereClauseHelpers.GetWhereClauseTreeHash(b));
    }

    [Fact]
    public void GetWhereClauseTreeHash_OmittedFuzzyThreshold_MatchesDefault()
    {
        var omitted = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.Fuzzy, "Alice");
        var explicitDefault = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.Fuzzy, "Alice", threshold: ConditionClause.DefaultFuzzyThreshold);
        Assert.Equal(WhereClauseHelpers.GetWhereClauseTreeHash(omitted), WhereClauseHelpers.GetWhereClauseTreeHash(explicitDefault));
    }

    [Fact]
    public void FuzzyMatch_Identical_IsOne() => Assert.Equal(1, FuzzyMatch.Similarity("Alice", "Alice"));

    [Fact]
    public void FuzzyMatch_Empty_IsZero() => Assert.Equal(0, FuzzyMatch.Similarity("", "Alice"));

    [Fact]
    public void Fuzzy_NullField_DoesNotMatch()
    {
        var svc = CreateService();
        var rows = new List<NullableName> { new() { Name = null }, new() { Name = "Alice" } }.AsQueryable();
        var node = WhereClauseBuilder.And().Fuzzy("Name", "Alice").Build();
        var res = svc.ApplyWhereClause(rows, node).ToList();
        Assert.Equal("Alice", Assert.Single(res).Name);
    }

    [Fact]
    public void NotFuzzy_NullField_Matches()
    {
        var svc = CreateService();
        var rows = new List<NullableName> { new() { Name = null }, new() { Name = "Alice" } }.AsQueryable();
        var node = WhereClauseBuilder.And().NotFuzzy("Name", "Alice").Build();
        var res = svc.ApplyWhereClause(rows, node).Select(x => x.Name).ToList();
        Assert.Contains(null, res);
        Assert.DoesNotContain("Alice", res);
    }

    private sealed class NullableName
    {
        public string? Name { get; set; }
    }
}