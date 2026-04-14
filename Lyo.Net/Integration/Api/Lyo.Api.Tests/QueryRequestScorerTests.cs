using Lyo.Api.Models.Common.Request;
using Lyo.Common.Enums;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;

namespace Lyo.Api.Tests;

public class QueryRequestScorerTests
{
    [Fact]
    public void Score_NullRequest_ReturnsZero()
    {
        var score = QueryRequestScorer.Score((QueryReq?)null);
        var detailed = QueryRequestScorer.ScoreDetailed((QueryReq?)null);
        Assert.Equal(0, score);
        Assert.Equal(0, detailed.TotalScore);
        Assert.Equal(0, detailed.WhereClauseCount);
    }

    [Fact]
    public void ScoreDetailed_TracksPagingSortAndKeys()
    {
        var request = new QueryReq {
            Start = 200,
            Amount = 300,
            Keys = [[1], [2], [3]],
            SortBy = [new("LastName", SortDirection.Asc, 0), new("FirstName", SortDirection.Asc, 1)]
        };

        var detailed = QueryRequestScorer.ScoreDetailed(request);
        Assert.True(detailed.TotalScore > 0);
        Assert.Equal(200, detailed.Start);
        Assert.Equal(300, detailed.Amount);
        Assert.Equal(3, detailed.KeyCount);
        Assert.Equal(2, detailed.SortCount);
        Assert.True(detailed.PagingScore > 0);
        Assert.True(detailed.KeysScore > 0);
        Assert.True(detailed.SortScore > 0);
    }

    [Fact]
    public void ScoreDetailed_DeeperIncludes_IncreaseIncludeScore()
    {
        var shallow = new QueryReq { Amount = 100, Include = ["addresses"] };
        var deep = new QueryReq { Amount = 100, Include = ["contactaddresses.address", "contactphonenumbers.phonenumber", "contactemailaddresses.emailaddress"] };
        var shallowScore = QueryRequestScorer.ScoreDetailed(shallow);
        var deepScore = QueryRequestScorer.ScoreDetailed(deep);
        Assert.True(deepScore.IncludeScore > shallowScore.IncludeScore);
        Assert.True(deepScore.IncludeMaxDepth > shallowScore.IncludeMaxDepth);
        Assert.True(deepScore.IncludeCount > shallowScore.IncludeCount);
        Assert.True(deepScore.TotalScore > shallowScore.TotalScore);
    }

    [Fact]
    public void ScoreDetailed_SelectWithNavigationsAndWildcards_IncreaseScore()
    {
        var rootOnly = new ProjectionQueryReq { Amount = 100, Select = ["Id", "Name"] };
        var withNav = new ProjectionQueryReq { Amount = 100, Select = ["Id", "Addresses.City", "ContactPhones.PhoneNumber.Number"] };
        var withWildcard = new ProjectionQueryReq { Amount = 100, Select = ["Id", "Addresses.*"] };
        var rootScore = QueryRequestScorer.ScoreDetailed(rootOnly);
        var navScore = QueryRequestScorer.ScoreDetailed(withNav);
        var wildcardScore = QueryRequestScorer.ScoreDetailed(withWildcard);
        Assert.True(navScore.IncludeScore > rootScore.IncludeScore);
        Assert.True(navScore.IncludeMaxDepth > rootScore.IncludeMaxDepth);
        Assert.True(wildcardScore.IncludeScore > rootScore.IncludeScore);
        Assert.True(wildcardScore.TotalScore > rootScore.TotalScore);
    }

    [Fact]
    public void ScoreDetailed_ComparisonComplexity_RegexAndInIncreaseScore()
    {
        var simple = new QueryReq { WhereClause = WhereClauseBuilder.Condition("FirstName", ComparisonOperatorEnum.Equals, "Matt") };
        var complex = new QueryReq {
            WhereClause = WhereClauseBuilder.And(b => {
                b.AddCondition("FirstName", ComparisonOperatorEnum.Regex, "^(?i)m[a-z]+$");
                b.AddCondition("Source", ComparisonOperatorEnum.In, "A,B,C,D,E,F,G,H,I,J");
            })
        };

        var simpleScore = QueryRequestScorer.ScoreDetailed(simple);
        var complexScore = QueryRequestScorer.ScoreDetailed(complex);
        Assert.True(complexScore.WhereClauseScore > simpleScore.WhereClauseScore);
        Assert.True(complexScore.TotalScore > simpleScore.TotalScore);
    }

    [Fact]
    public void ScoreDetailed_WhereClauseAndSubQueryDepthIncreaseNodeScore()
    {
        var noSubQuery = new QueryReq {
            WhereClause = new GroupClause(
                GroupOperatorEnum.And, [new ConditionClause("FirstName", ComparisonOperatorEnum.NotEquals, null), new ConditionClause("LastName", ComparisonOperatorEnum.NotEquals, null)])
        };

        var withSubQuery = new QueryReq {
            WhereClause = new GroupClause(
                GroupOperatorEnum.And,
                [
                    new ConditionClause("FirstName", ComparisonOperatorEnum.NotEquals, null) {
                        SubClause = new GroupClause(
                            GroupOperatorEnum.Or,
                            [new ConditionClause("Source", ComparisonOperatorEnum.In, "A,B,C,D"), new ConditionClause("LastName", ComparisonOperatorEnum.Regex, "^[A-Z][a-z]+$")])
                    },
                    new ConditionClause("LastName", ComparisonOperatorEnum.NotEquals, null)
                ])
        };

        var baseScore = QueryRequestScorer.ScoreDetailed(noSubQuery);
        var subScore = QueryRequestScorer.ScoreDetailed(withSubQuery);
        Assert.True(subScore.WhereClauseScore > baseScore.WhereClauseScore);
        Assert.True(subScore.QuerySubClauseCount > baseScore.QuerySubClauseCount);
        Assert.True(subScore.QuerySubClauseMaxDepth > baseScore.QuerySubClauseMaxDepth);
        Assert.True(subScore.WhereClauseCount > baseScore.WhereClauseCount);
        Assert.True(subScore.TotalScore > baseScore.TotalScore);
    }

    [Fact]
    public void ScoreDetailed_ComputedFields_BasePerTemplatePlusUniquePlaceholders()
    {
        var without = new ProjectionQueryReq { Amount = 100, Select = ["FirstName"] };
        var with = new ProjectionQueryReq { Amount = 100, Select = ["FirstName"], ComputedFields = [new("FullName", "{LastName}, {FirstName}"), new("Greeting", "Hello {FirstName}")] };
        var a = QueryRequestScorer.ScoreDetailed(without);
        var b = QueryRequestScorer.ScoreDetailed(with);
        Assert.Equal(0, a.ComputedFieldsScore);
        // 2 templates: base min(20, 8) = 8; distinct placeholders LastName, FirstName → min(25, 6) = 6
        Assert.Equal(14, b.ComputedFieldsScore);
        Assert.Equal(a.TotalScore + 14, b.TotalScore);
    }

    [Fact]
    public void ScoreDetailed_TotalCountModeExactCostsMoreThanNone()
    {
        var none = new QueryReq { Options = new QueryRequestOptions { TotalCountMode = QueryTotalCountMode.None } };
        var hasMore = new QueryReq { Options = new QueryRequestOptions { TotalCountMode = QueryTotalCountMode.HasMore } };
        var exact = new QueryReq { Options = new QueryRequestOptions { TotalCountMode = QueryTotalCountMode.Exact } };
        var noneScore = QueryRequestScorer.ScoreDetailed(none);
        var hasMoreScore = QueryRequestScorer.ScoreDetailed(hasMore);
        var exactScore = QueryRequestScorer.ScoreDetailed(exact);
        Assert.Equal(0, noneScore.TotalCountModeScore);
        Assert.True(hasMoreScore.TotalCountModeScore > noneScore.TotalCountModeScore);
        Assert.True(exactScore.TotalCountModeScore > hasMoreScore.TotalCountModeScore);
    }
}