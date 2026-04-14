using System.Linq.Expressions;
using Lyo.Common.Enums;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;
using Lyo.Query.Services.WhereClause;

namespace Lyo.Query.Tests;

public class ExplainMatchTests : WhereClauseServiceTests
{
    [Fact]
    public void ExplainMatch_Passed_matches_MatchesWhereClause()
    {
        var svc = CreateService();
        var person = new PersonBuilder().WithName("Alice").Build();
        var clause = WhereClauseBuilder.And(b => b.Equals("Name", "Alice"));

        Assert.True(svc.MatchesWhereClause(person, clause));
        var explain = svc.ExplainMatch(person, clause);
        Assert.True(explain.Passed);
        Assert.Null(explain.BlockingPath);
        Assert.Null(explain.FailureSummary);
        Assert.True(explain.Root.Passed);
        Assert.Equal(WhereClauseExplainKind.Group, explain.Root.Kind);
        Assert.NotNull(explain.Root.Children);
        Assert.Single(explain.Root.Children!);
        Assert.True(explain.Root.Children![0].Passed);
    }

    [Fact]
    public void ExplainMatch_null_entity_or_clause_is_not_passed()
    {
        var svc = CreateService();
        var clause = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.Equals, "X");
        var noEntity = svc.ExplainMatch<Person>(null!, clause);
        Assert.False(noEntity.Passed);
        Assert.Equal(WhereClauseExplainKind.None, noEntity.Root.Kind);
        Assert.Equal("Entity is null.", noEntity.FailureSummary);

        var person = new PersonBuilder().WithName("A").Build();
        var noClause = svc.ExplainMatch(person, null);
        Assert.False(noClause.Passed);
        Assert.Equal(WhereClauseExplainKind.None, noClause.Root.Kind);
        Assert.Equal("Where clause is null.", noClause.FailureSummary);
    }

    [Fact]
    public void ExplainMatch_failed_Or_populates_OrBranchOutcomes_per_alternative()
    {
        var svc = CreateService();
        var person = new PersonBuilder().WithName("Charlie").Build();
        var clause = WhereClauseBuilder.Or(b => b.Equals("Name", "Alpha").Equals("Name", "Bravo"));

        Assert.False(svc.MatchesWhereClause(person, clause));
        var explain = svc.ExplainMatch(person, clause);
        Assert.NotNull(explain.OrBranchOutcomes);
        Assert.Equal(2, explain.OrBranchOutcomes!.Count);
        Assert.All(explain.OrBranchOutcomes, o => Assert.Equal("", o.OrGroupPath));
        Assert.Contains(explain.OrBranchOutcomes, o => o.BranchPath == "0" && !o.Passed);
        Assert.Contains(explain.OrBranchOutcomes, o => o.BranchPath == "1" && !o.Passed);
        Assert.All(explain.OrBranchOutcomes, o => Assert.Contains("Name", o.Summary, StringComparison.Ordinal));
    }

    [Fact]
    public void ExplainMatch_And_fails_when_first_child_fails()
    {
        var svc = CreateService();
        // Name fails the first predicate; Age satisfies the second — overall And still false.
        var person = new PersonBuilder().WithName("Bob").WithAge(10).Build();
        var clause = WhereClauseBuilder.And(b => b.Equals("Name", "Alice").Equals("Age", 10));

        Assert.False(svc.MatchesWhereClause(person, clause));
        var explain = svc.ExplainMatch(person, clause);
        Assert.False(explain.Passed);
        Assert.Equal("0", explain.BlockingPath);
        Assert.Contains("Name", explain.FailureSummary!, StringComparison.Ordinal);
        Assert.Contains("Bob", explain.FailureSummary!, StringComparison.Ordinal);
        Assert.Equal("Bob", explain.Root.Children![0].ActualValueSummary);
        Assert.False(explain.Root.Children![0].Passed);
        Assert.True(explain.Root.Children![1].Passed);
    }

    [Fact]
    public void ExplainMatch_condition_with_SubClause_shows_PrimaryPredicatePassed()
    {
        var svc = CreateService();
        var person = new PersonBuilder().WithName("Alice").WithAge(5).Build();
        var sub = WhereClauseBuilder.Condition("Age", ComparisonOperatorEnum.Equals, 10);
        var clause = WhereClauseBuilder.ConditionWithSubClause("Name", ComparisonOperatorEnum.Equals, "Alice", sub);

        Assert.False(svc.MatchesWhereClause(person, clause));
        var explain = svc.ExplainMatch(person, clause);
        Assert.Equal(WhereClauseExplainKind.Condition, explain.Root.Kind);
        Assert.Equal("sub", explain.BlockingPath);
        Assert.Contains("Age", explain.FailureSummary!, StringComparison.Ordinal);
        Assert.Contains("5", explain.FailureSummary!, StringComparison.Ordinal);
        Assert.True(explain.Root.PrimaryPredicatePassed);
        Assert.NotNull(explain.Root.SubClause);
        Assert.False(explain.Root.SubClause!.Passed);
    }

    [Fact]
    public void IWhereClauseService_default_ExplainMatch_throws_for_database_style_implementations()
    {
        IWhereClauseService dbOnly = new DatabaseOnlyWhereClauseStub();
        var person = new PersonBuilder().Build();
        var clause = WhereClauseBuilder.Condition("Name", ComparisonOperatorEnum.Equals, "A");
        Assert.Throws<NotImplementedException>(() => dbOnly.ExplainMatch(person, clause));
    }

    /// <summary>Minimal stub with no ExplainMatch: default interface implementation should throw.</summary>
    private sealed class DatabaseOnlyWhereClauseStub : IWhereClauseService
    {
        public IQueryable<TEntity> ApplyWhereClause<TEntity>(IQueryable<TEntity> source, WhereClause? queryNode, bool includeSubClauses = true) =>
            throw new NotImplementedException();

        public IQueryable<TEntity> SortByProperty<TEntity>(IQueryable<TEntity> source, string propertyName, SortDirection? direction = null) =>
            throw new NotImplementedException();

        public IQueryable<TEntity> ApplyOrdering<TEntity>(
            IQueryable<TEntity> queryable,
            IEnumerable<SortBy> sortByProps,
            Expression<Func<TEntity, object?>> defaultOrder,
            SortDirection defaultSortDirection) => throw new NotImplementedException();

        public bool MatchesWhereClause<TEntity>(TEntity entity, WhereClause? queryNode) => throw new NotImplementedException();

        public IEnumerable<string> GetCollectionIncludePathsForWhereClause<TEntity>(WhereClause? queryNode) => throw new NotImplementedException();

        public bool TryValidatePropertyPath<TEntity>(string propertyName, out string? errorMessage) => throw new NotImplementedException();
    }
}
