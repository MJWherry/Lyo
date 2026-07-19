using Lyo.Common.Enums;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;

namespace Lyo.Query.Tests;

public class QueryRequestCloneTests
{
    [Fact]
    public void Clone_QueryConcreteReq_CopiesFields_AndSharesWhereClause()
    {
        var where = WhereClauseBuilder.And(b => b.Equals("FirstName", "Ada"));
        var source = new QueryConcreteReq {
            Start = 10,
            Amount = 25,
            Options = new() { TotalCountMode = QueryTotalCountMode.HasMore, IncludeFilterMode = QueryIncludeFilterMode.MatchedOnly },
            WhereClause = where,
            Include = ["contactaddresses.address"],
            Keys = [[Guid.NewGuid()]],
            SortBy = [new SortBy("CreatedTimestamp", SortDirection.Desc, 0)]
        };

        var clone = QueryRequestClone.Clone(source);

        Assert.NotSame(source, clone);
        Assert.Equal(10, clone.Start);
        Assert.Equal(25, clone.Amount);
        Assert.Equal(QueryTotalCountMode.HasMore, clone.Options.TotalCountMode);
        Assert.Equal(QueryIncludeFilterMode.MatchedOnly, clone.Options.IncludeFilterMode);
        Assert.Same(where, clone.WhereClause);
        Assert.Equal(source.Include, clone.Include);
        Assert.NotSame(source.Include, clone.Include);
        Assert.Equal(source.Keys[0][0], clone.Keys[0][0]);
        Assert.NotSame(source.Keys, clone.Keys);
        Assert.NotSame(source.Keys[0], clone.Keys[0]);
        Assert.Equal("CreatedTimestamp", clone.SortBy[0].PropertyName);
        Assert.NotSame(source.SortBy, clone.SortBy);

        clone.Start = 99;
        clone.Include.Add("extra");
        Assert.Equal(10, source.Start);
        Assert.Single(source.Include);
    }

    [Fact]
    public void Clone_ProjectionQueryReq_CopiesSelectAndComputedFields()
    {
        var source = new ProjectionQueryReq {
            Start = 0,
            Amount = 50,
            Options = new() { ZipSiblingCollectionSelections = false },
            Select = ["Id", "FirstName"],
            ComputedFields = [new ComputedField("FullName", "{FirstName}")],
            SortBy = [new SortBy("Id", SortDirection.Asc)]
        };

        var clone = QueryRequestClone.Clone(source);

        Assert.Equal(["Id", "FirstName"], clone.Select);
        Assert.NotSame(source.Select, clone.Select);
        Assert.False(clone.Options.ZipSiblingCollectionSelections);
        Assert.Single(clone.ComputedFields);
        Assert.Equal("FullName", clone.ComputedFields[0].Name);
        Assert.Equal("{FirstName}", clone.ComputedFields[0].Template);
        Assert.NotSame(source.ComputedFields[0], clone.ComputedFields[0]);
    }

    [Fact]
    public void Clone_QueryReq_CopiesFromAndJoins()
    {
        var source = new QueryReq {
            Start = 5,
            Amount = 10,
            From = new FromClause { Alias = "p", EntityType = "PersonEntity" },
            Joins = [
                new JoinClause {
                    Alias = "a",
                    EntityType = "AddressEntity",
                    Type = JoinType.Left,
                    As = "addr",
                    On = [new JoinOn { From = "p.Id", To = "a.PersonId" }]
                }
            ],
            Select = ["p.Id", "a.City"]
        };

        var clone = QueryRequestClone.Clone(source);

        Assert.Equal(5, clone.Start);
        Assert.Equal("p", clone.From.Alias);
        Assert.Equal("PersonEntity", clone.From.EntityType);
        Assert.NotSame(source.From, clone.From);
        Assert.Single(clone.Joins);
        Assert.NotSame(source.Joins, clone.Joins);
        Assert.NotSame(source.Joins[0], clone.Joins[0]);
        Assert.Equal(JoinType.Left, clone.Joins[0].Type);
        Assert.Equal("addr", clone.Joins[0].As);
        Assert.Equal("p.Id", clone.Joins[0].On[0].From);
        Assert.NotSame(source.Joins[0].On, clone.Joins[0].On);
        Assert.Equal(["p.Id", "a.City"], clone.Select);
    }

    [Fact]
    public void Clone_QueryRequestBase_DispatchesByRuntimeType()
    {
        QueryRequestBase concrete = new QueryConcreteReq { Amount = 1 };
        QueryRequestBase projection = new ProjectionQueryReq { Amount = 2, Select = ["Id"] };
        QueryRequestBase root = new QueryReq { Amount = 3, From = new() { Alias = "x", EntityType = "X" }, Select = ["x.Id"] };

        Assert.IsType<QueryConcreteReq>(QueryRequestClone.Clone(concrete));
        Assert.IsType<ProjectionQueryReq>(QueryRequestClone.Clone(projection));
        Assert.IsType<QueryReq>(QueryRequestClone.Clone(root));
    }
}
