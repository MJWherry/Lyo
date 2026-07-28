using Lyo.Api.Models.Common.Response;
using Lyo.Common.Enums;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common.Request;

namespace Lyo.Api.Tests;

public class QueryResExtensionsTests
{
    [Fact]
    public void QueryRes_WithStart_ClonesAndSetsStartOnly()
    {
        var where = WhereClauseBuilder.And(b => b.Equals("LastName", "Lovelace"));
        var request = new QueryConcreteReq {
            Start = 0,
            Amount = 20,
            WhereClause = where,
            Include = ["phones"],
            SortBy = [new("Id", SortDirection.Asc)]
        };

        var result = ResultFactory.QuerySuccess(request, ["a", "b"], 0, 2, 100, true);
        var next = result.WithStart(40);
        Assert.NotSame(request, next);
        Assert.Equal(40, next.Start);
        Assert.Equal(20, next.Amount);
        Assert.Same(where, next.WhereClause);
        Assert.Equal(["phones"], next.Include);
        Assert.Equal(0, request.Start);
    }

    [Fact]
    public void QueryRes_ToNextQueryRequest_AdvancesByRequestAmount()
    {
        var request = new QueryConcreteReq { Start = 10, Amount = 25 };
        var result = ResultFactory.QuerySuccess(request, new[] { 1, 2, 3 }, 10, 3, 100, true);
        var next = result.ToNextQueryRequest();
        Assert.Equal(35, next.Start);
        Assert.Equal(25, next.Amount);
    }

    [Fact]
    public void QueryRes_ToNextQueryRequest_FallsBackToResultAmountWhenRequestAmountMissing()
    {
        var request = new QueryConcreteReq { Start = 0 };
        var result = ResultFactory.QuerySuccess(request, new[] { "x", "y" }, 0, 2, null, true);
        var next = result.ToNextQueryRequest();
        Assert.Equal(2, next.Start);
    }

    [Fact]
    public void ProjectedQueryRes_ToNextProjectionQueryRequest_AdvancesAndReturnsProjectionType()
    {
        var request = new ProjectionQueryReq {
            Start = 50,
            Amount = 10,
            Select = ["Id", "Name"],
            ComputedFields = [new("Label", "{Name}")]
        };

        var result = ResultFactory.ProjectedQuerySuccess(request, new[] { new Dictionary<string, object?>() }, 50, 10, 200, true);
        var next = result.ToNextProjectionQueryRequest();
        Assert.Equal(60, next.Start);
        Assert.Equal(["Id", "Name"], next.Select);
        Assert.Single(next.ComputedFields);
        Assert.Equal(50, request.Start);
    }

    [Fact]
    public void ProjectedQueryRes_ToNextRootQueryRequest_AdvancesAndReturnsRootType()
    {
        var request = new QueryReq {
            Start = 0,
            Amount = 5,
            From = new() { Alias = "p", EntityType = "PersonEntity" },
            Select = ["p.Id"]
        };

        var result = ResultFactory.ProjectedQuerySuccess(request, new object[5], 0, 5, 20, true);
        var next = result.ToNextRootQueryRequest();
        Assert.Equal(5, next.Start);
        Assert.Equal("p", next.From.Alias);
        Assert.Equal(["p.Id"], next.Select);
    }

    [Fact]
    public void ProjectedQueryRes_ToNextProjectionQueryRequest_ThrowsWhenRootRequest()
    {
        var request = new QueryReq { Amount = 1, From = new() { Alias = "p", EntityType = "PersonEntity" }, Select = ["p.Id"] };
        var result = ResultFactory.ProjectedQuerySuccess(request, new object[1], 0, 1, 1);
        Assert.Throws<InvalidOperationException>(() => result.ToNextProjectionQueryRequest());
    }

    [Fact]
    public void ProjectedQueryRes_WithStart_WorksForProjection()
    {
        var request = new ProjectionQueryReq { Start = 0, Amount = 10, Select = ["Id"] };
        var result = ResultFactory.ProjectedQuerySuccess(request, Array.Empty<object>(), 0, 0, 0);
        var next = result.WithStart(100);
        Assert.IsType<ProjectionQueryReq>(next);
        Assert.Equal(100, next.Start);
        Assert.Equal(10, next.Amount);
    }
}