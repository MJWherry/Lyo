using Lyo.Common.Core.Enums;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Lyo.Query.Models.Exceptions;
using Lyo.Query.Services.Aggregate;

namespace Lyo.Query.Tests;

public class AggregateQueryTests : WhereClauseServiceTests
{
    [Fact]
    public void Execute_GroupByNameAndAge_ReturnsCountPerGroup()
    {
        var people = People(new() { Name = "F", Age = 30 }, new() { Name = "F", Age = 30 }, new() { Name = "M", Age = 30 });
        var page = AggregateQuery.Execute(people, Request(["Name", "Age"], new AggregateSelect(AggregateFunction.Count, "Count")));
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(2, page.Total);
        var female = Row(page, "Name", "F");
        Assert.Equal(30, female["Age"]);
        Assert.Equal(2, female["Count"]);
        Assert.Equal(1, Row(page, "Name", "M")["Count"]);
    }

    [Fact]
    public void Execute_SumAndAverage_SkipsNulls_AverageOfIntsIsDouble()
    {
        var people = People(
            new() { Name = "F", Age = 10, AgeNullable = 10 }, new() { Name = "F", Age = 30, AgeNullable = null }, new() { Name = "F", Age = 30, AgeNullable = 30 },
            new() { Name = "M", Age = 30, AgeNullable = 30 });

        var page = AggregateQuery.Execute(
            people,
            Request(
                ["Name"], new AggregateSelect(AggregateFunction.Sum, "AgeSum", "Age"), new AggregateSelect(AggregateFunction.Average, "AgeAvg", "Age"),
                new AggregateSelect(AggregateFunction.Sum, "NullableSum", "AgeNullable"), new AggregateSelect(AggregateFunction.Average, "NullableAvg", "AgeNullable")));

        var female = Row(page, "Name", "F");
        Assert.Equal(70, female["AgeSum"]);
        Assert.Equal(70d / 3d, female["AgeAvg"]);
        Assert.Equal(40, female["NullableSum"]);
        Assert.Equal(20d, female["NullableAvg"]);
    }

    [Fact]
    public void Execute_MinAndMax_UsesGroupValues()
    {
        var people = People(new() { Name = "F", Age = 10 }, new() { Name = "F", Age = 30 }, new() { Name = "M", Age = 30 });
        var page = AggregateQuery.Execute(
            people, Request(["Name"], new AggregateSelect(AggregateFunction.Min, "Youngest", "Age"), new AggregateSelect(AggregateFunction.Max, "Oldest", "Age")));

        var female = Row(page, "Name", "F");
        Assert.Equal(10, female["Youngest"]);
        Assert.Equal(30, female["Oldest"]);
        Assert.Equal(30, Row(page, "Name", "M")["Youngest"]);
    }

    [Fact]
    public void Execute_CountAndSum_ReturnsBothColumns()
    {
        var people = People(new() { Name = "F", Age = 10 }, new() { Name = "F", Age = 30 });
        var page = AggregateQuery.Execute(
            people, Request(["Name"], new AggregateSelect(AggregateFunction.Count, "Count"), new AggregateSelect(AggregateFunction.Sum, "AgeSum", "Age")));

        var row = Assert.Single(page.Items);
        Assert.Equal(2, row["Count"]);
        Assert.Equal(40, row["AgeSum"]);
    }

    [Fact]
    public void Execute_WhereClause_DropsRowsBeforeAggregates()
    {
        var people = People(new() { Name = "F", Age = 10 }, new() { Name = "F", Age = 30 }, new() { Name = "M", Age = 30 });
        var filtered = CreateService().ApplyWhereClause(people, WhereClauseBuilder.And(b => b.GreaterThan("Age", 20)));
        var page = AggregateQuery.Execute(
            filtered, Request(["Name"], new AggregateSelect(AggregateFunction.Count, "Count"), new AggregateSelect(AggregateFunction.Sum, "AgeSum", "Age")));

        Assert.Equal(2, page.Items.Count);
        Assert.Equal(1, Row(page, "Name", "F")["Count"]);
        Assert.Equal(30, Row(page, "Name", "F")["AgeSum"]);
        Assert.Equal(1, Row(page, "Name", "M")["Count"]);
    }

    [Fact]
    public void Execute_SortByAggregateDescending_OrdersGroups()
    {
        var people = People(new() { Name = "M", Age = 30 }, new() { Name = "F", Age = 30 }, new() { Name = "F", Age = 31 });
        var request = Request(["Name"], new AggregateSelect(AggregateFunction.Count, "Count"));
        request.SortBy = [new("Count", SortDirection.Desc)];
        var page = AggregateQuery.Execute(people, request);
        Assert.Equal("F", page.Items[0]["Name"]);
        Assert.Equal(2, page.Items[0]["Count"]);
        Assert.Equal("M", page.Items[1]["Name"]);
    }

    [Fact]
    public void Execute_StartAndAmount_PageGroups_TotalIsGroupCount()
    {
        var people = People(new() { Name = "A", Age = 1 }, new() { Name = "B", Age = 1 }, new() { Name = "C", Age = 1 });
        var request = Request(["Name"], new AggregateSelect(AggregateFunction.Count, "Count"));
        request.Start = 1;
        request.Amount = 1;
        request.SortBy = [new("Name", SortDirection.Asc)];
        request.Options.TotalCountMode = QueryTotalCountMode.Exact;
        var page = AggregateQuery.Execute(people, request);
        var row = Assert.Single(page.Items);
        Assert.Equal("B", row["Name"]);
        Assert.Equal(3, page.Total);
    }

    [Fact]
    public void Execute_NullGroupKey_SharesOneGroup()
    {
        var people = People(new Person { Name = null!, Age = 1 }, new Person { Name = null!, Age = 2 }, new Person { Name = "A", Age = 3 });
        var page = AggregateQuery.Execute(people, Request(["Name"], new AggregateSelect(AggregateFunction.Count, "Count")));
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(2, page.Items.Single(r => r["Name"] is null)["Count"]);
        Assert.Equal(1, Row(page, "Name", "A")["Count"]);
    }

    [Fact]
    public void Execute_EmptySelect_OnEmptySource_ReturnsOneSummaryRow()
    {
        var empty = People();
        var count = AggregateQuery.Execute(empty, Request([], new AggregateSelect(AggregateFunction.Count, "Count")));
        var row = Assert.Single(count.Items);
        Assert.Equal(0, row["Count"]);
        Assert.Equal(1, count.Total);
        var sum = AggregateQuery.Execute(empty, Request([], new AggregateSelect(AggregateFunction.Sum, "AgeSum", "Age")));
        Assert.Equal(0, Assert.Single(sum.Items)["AgeSum"]);
        var nullableSum = AggregateQuery.Execute(empty, Request([], new AggregateSelect(AggregateFunction.Sum, "AgeSum", "AgeNullable")));
        Assert.Null(Assert.Single(nullableSum.Items)["AgeSum"]);
        var average = AggregateQuery.Execute(empty, Request([], new AggregateSelect(AggregateFunction.Average, "AgeAvg", "Age")));
        Assert.Null(Assert.Single(average.Items)["AgeAvg"]);
        var min = AggregateQuery.Execute(empty, Request([], new AggregateSelect(AggregateFunction.Min, "Youngest", "Age")));
        Assert.Null(Assert.Single(min.Items)["Youngest"]);
        var max = AggregateQuery.Execute(empty, Request([], new AggregateSelect(AggregateFunction.Max, "Oldest", "Age")));
        Assert.Null(Assert.Single(max.Items)["Oldest"]);
    }

    [Fact]
    public void Execute_GroupedOverNoRows_ReturnsNoItems()
    {
        var people = People(new Person { Name = "A", Age = 1 });
        var filtered = CreateService().ApplyWhereClause(people, WhereClauseBuilder.And(b => b.Equals("Name", "nope")));
        var page = AggregateQuery.Execute(filtered, Request(["Name"], new AggregateSelect(AggregateFunction.Count, "Count")));
        Assert.Empty(page.Items);
        Assert.Equal(0, page.Total);
    }

    [Fact]
    public void Execute_AllNullMinMax_AreNull()
    {
        var people = People(new() { Name = "Z", AgeNullable = null }, new() { Name = "Z", AgeNullable = null });
        var page = AggregateQuery.Execute(
            people, Request(["Name"], new AggregateSelect(AggregateFunction.Min, "Youngest", "AgeNullable"), new AggregateSelect(AggregateFunction.Max, "Oldest", "AgeNullable")));

        var row = Assert.Single(page.Items);
        Assert.Null(row["Youngest"]);
        Assert.Null(row["Oldest"]);
    }

    [Fact]
    public void Validate_RejectsKeysComputedFieldsSubClauseAndMatchedOnly()
    {
        var keys = Request(["Name"], new AggregateSelect(AggregateFunction.Count, "Count"));
        keys.Keys = [[1]];
        AssertRejected(keys, "keys");
        var computed = Request(["Name"], new AggregateSelect(AggregateFunction.Count, "Count"));
        computed.ComputedFields = [new("Label", "{Name}")];
        AssertRejected(computed, "computed");
        var sub = Request(["Name"], new AggregateSelect(AggregateFunction.Count, "Count"));
        sub.WhereClause = new ConditionClause {
            Field = "Name",
            Comparison = ComparisonOperatorEnum.Equals,
            Value = "A",
            SubClause = new ConditionClause()
        };

        AssertRejected(sub, "sub-clause");
        var matched = Request(["Name"], new AggregateSelect(AggregateFunction.Count, "Count"));
        matched.Options.IncludeFilterMode = QueryIncludeFilterMode.MatchedOnly;
        AssertRejected(matched, "matched-only");
    }

    [Fact]
    public void Validate_RejectsCollectionWildcardCountFieldAndBadSum()
    {
        AssertRejected(Request(["Tags"], new AggregateSelect(AggregateFunction.Count, "Count")), "collection");
        AssertRejected(Request(["*"], new AggregateSelect(AggregateFunction.Count, "Count")), "wildcard");
        AssertRejected(Request(["Name"], new AggregateSelect(AggregateFunction.Sum, "AgeSum")), "field");
        AssertRejected(Request(["Name"], new AggregateSelect(AggregateFunction.Sum, "AgeSum", "Name")), "numeric");
        var counted = AggregateQuery.Execute(
            People(new() { Name = "A", Age = 1 }, new() { Name = "A", Age = 2 }),
            Request(["Name"], new AggregateSelect(AggregateFunction.Count, "Rows"), new AggregateSelect(AggregateFunction.Count, "Ages", "Age")));

        var row = Row(counted, "Name", "A");
        Assert.Equal(2, row["Rows"]);
        Assert.Equal(2, row["Ages"]);
    }

    [Fact]
    public void Execute_LastNameIndex_GroupsByFirstCharacter()
    {
        var rows = Rows(new() { LastName = "Smith" }, new() { LastName = "Song" }, new() { LastName = "song" });
        var page = AggregateQuery.Execute(rows, IndexedRequest(["LastName[0]"], new AggregateSelect(AggregateFunction.Count, "Count")));
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(2, Row(page, "LastName[0]", "S")["Count"]);
        Assert.Equal(1, Row(page, "LastName[0]", "s")["Count"]);
    }

    [Fact]
    public void Execute_LastNameRange_GroupsByPrefix()
    {
        var rows = Rows(new() { LastName = "Smith" }, new() { LastName = "Smile" }, new() { LastName = "Sm" });
        var closed = AggregateQuery.Execute(rows, IndexedRequest(["LastName[0:3]"], new AggregateSelect(AggregateFunction.Count, "Count")));
        var open = AggregateQuery.Execute(rows, IndexedRequest(["LastName[:3]"], new AggregateSelect(AggregateFunction.Count, "Count")));
        var one = AggregateQuery.Execute(rows, IndexedRequest(["LastName[0]"], new AggregateSelect(AggregateFunction.Count, "Count")));
        var same = AggregateQuery.Execute(rows, IndexedRequest(["LastName[0:1]"], new AggregateSelect(AggregateFunction.Count, "Count")));
        Assert.Equal(2, Row(closed, "LastName[0:3]", "Smi")["Count"]);
        Assert.Equal(1, Row(closed, "LastName[0:3]", "Sm")["Count"]);
        Assert.Equal(2, Row(open, "LastName[:3]", "Smi")["Count"]);
        Assert.Equal("S", one.Items.Single(r => Equals(r["Count"], 3))["LastName[0]"]);
        Assert.Equal(one.Items.Single(r => Equals(r["Count"], 3))["LastName[0]"], same.Items.Single(r => Equals(r["Count"], 3))["LastName[0:1]"]);
    }

    [Fact]
    public void Execute_LastNameOpenEnd_GroupsBySuffix()
    {
        var rows = Rows(new() { LastName = "Smith" }, new() { LastName = "S" });
        var suffix = AggregateQuery.Execute(rows, IndexedRequest(["LastName[2:]"], new AggregateSelect(AggregateFunction.Count, "Count")));
        var whole = AggregateQuery.Execute(rows, IndexedRequest(["LastName[0:]"], new AggregateSelect(AggregateFunction.Count, "Count")));
        Assert.Equal(1, Row(suffix, "LastName[2:]", "ith")["Count"]);
        Assert.Equal(1, suffix.Items.Count(r => r["LastName[2:]"] is null));
        Assert.Equal(1, Row(whole, "LastName[0:]", "Smith")["Count"]);
        Assert.Equal(1, Row(whole, "LastName[0:]", "S")["Count"]);
    }

    [Fact]
    public void Execute_LastNameIndex_NullAndShortAreNull()
    {
        var rows = Rows(new() { LastName = null }, new() { LastName = "" });
        var page = AggregateQuery.Execute(rows, IndexedRequest(["LastName[0]"], new AggregateSelect(AggregateFunction.Count, "Count")));
        var row = Assert.Single(page.Items);
        Assert.Null(row["LastName[0]"]);
        Assert.Equal(2, row["Count"]);
    }

    [Fact]
    public void Execute_TagsIndex_GroupsByOrderedElement()
    {
        var rows = Rows(new() { Tags = ["b", "a"] }, new() { Tags = ["a", "c"] }, new() { Tags = ["b"] });
        var page = AggregateQuery.Execute(rows, IndexedRequest(["Tags[0]"], new AggregateSelect(AggregateFunction.Count, "Count")));
        Assert.Equal(2, Row(page, "Tags[0]", "a")["Count"]);
        Assert.Equal(1, Row(page, "Tags[0]", "b")["Count"]);
    }

    [Fact]
    public void Execute_AddressIndex_GroupsByCityOfLowestId()
    {
        var rows = Rows(
            new IndexedRow { Addresses = [new() { Id = 2, City = "B" }, new() { Id = 1, City = "A" }] }, new IndexedRow { Addresses = [new() { Id = 5, City = "A" }] },
            new IndexedRow { Addresses = [new() { Id = 1, City = "C" }] });

        var page = AggregateQuery.Execute(rows, IndexedRequest(["Addresses[0].City"], new AggregateSelect(AggregateFunction.Count, "Count")));
        Assert.Equal(2, Row(page, "Addresses[0].City", "A")["Count"]);
        Assert.Equal(1, Row(page, "Addresses[0].City", "C")["Count"]);
    }

    [Fact]
    public void Validate_CollectionRange_IsRejectedAsGroupKey()
    {
        AssertRejected<IndexedRow>(IndexedRequest(["Addresses[0:3].City"], new AggregateSelect(AggregateFunction.Count, "Count")), "cannot be grouped");
        AssertRejected<IndexedRow>(IndexedRequest(["Addresses[:3].City"], new AggregateSelect(AggregateFunction.Count, "Count")), "cannot be grouped");
        AssertRejected<IndexedRow>(IndexedRequest(["Addresses[0:].City"], new AggregateSelect(AggregateFunction.Count, "Count")), "cannot be grouped");
        var rows = Rows(new IndexedRow { Addresses = [new() { Id = 2, City = "B" }, new() { Id = 1, City = "A" }] });
        var page = AggregateQuery.Execute(rows, IndexedRequest(["Addresses[:1].City"], new AggregateSelect(AggregateFunction.Count, "Count")));
        Assert.Equal("A", Assert.Single(page.Items)["Addresses[:1].City"]);
    }

    [Fact]
    public void Validate_IndexOnNumber_IsRejected()
    {
        AssertRejected<IndexedRow>(IndexedRequest(["Age[0]"], new AggregateSelect(AggregateFunction.Count, "Count")), "string or a collection");
        AssertRejected<IndexedRow>(IndexedRequest(["Age[:3]"], new AggregateSelect(AggregateFunction.Count, "Count")), "string or a collection");
        AssertRejected<IndexedRow>(IndexedRequest(["Age[0:]"], new AggregateSelect(AggregateFunction.Count, "Count")), "string or a collection");
    }

    [Fact]
    public void Validate_BadRange_IsRejected()
    {
        AssertRejected<IndexedRow>(IndexedRequest(["LastName[2:2]"], new AggregateSelect(AggregateFunction.Count, "Count")), "range");
        AssertRejected<IndexedRow>(IndexedRequest(["LastName[3:1]"], new AggregateSelect(AggregateFunction.Count, "Count")), "range");
        AssertRejected<IndexedRow>(IndexedRequest(["LastName[-1]"], new AggregateSelect(AggregateFunction.Count, "Count")), "range");
        AssertRejected<IndexedRow>(IndexedRequest(["LastName[]"], new AggregateSelect(AggregateFunction.Count, "Count")), "range");
    }

    [Fact]
    public void Validate_IndexedEntityWithoutScalar_IsRejected()
        => AssertRejected<IndexedRow>(IndexedRequest(["Addresses[0]"], new AggregateSelect(AggregateFunction.Count, "Count")), "not a scalar");

    [Fact]
    public void Validate_CollectionWithoutKey_IsRejected()
        => AssertRejected<IndexedRow>(IndexedRequest(["Loose[0].Name"], new AggregateSelect(AggregateFunction.Count, "Count")), "no key");

    [Fact]
    public void Execute_CollectionCount_GroupsByCountAndSumsIt()
    {
        var rows = Rows(
            new() { LastName = "Angel", Tags = ["a"] }, new() { LastName = "Angel", Tags = ["a"] }, new() { LastName = "Angel", Tags = ["a", "b"] },
            new() { LastName = "Bo", Tags = [] });

        var page = AggregateQuery.Execute(rows, IndexedRequest(["LastName", "Tags.count"], new AggregateSelect(AggregateFunction.Sum, "Count", "Tags.count")));
        Assert.Equal(3, page.Items.Count);
        var one = page.Items.Single(r => Equals(r["LastName"], "Angel") && Equals(r["Tags.count"], 1));
        Assert.Equal(2, one["Count"]);
        var two = page.Items.Single(r => Equals(r["LastName"], "Angel") && Equals(r["Tags.count"], 2));
        Assert.Equal(2, two["Count"]);
        var none = page.Items.Single(r => Equals(r["LastName"], "Bo"));
        Assert.Equal(0, none["Tags.count"]);
        Assert.Equal(0, none["Count"]);
    }

    [Fact]
    public void Validate_CountOnScalar_IsRejected()
        => AssertRejected<IndexedRow>(IndexedRequest(["LastName.count"], new AggregateSelect(AggregateFunction.Count, "Count")), "not found");

    [Fact]
    public void Execute_CountCollection_SumsElementCounts()
    {
        var rows = Rows(
            new() { LastName = "Angel", Tags = ["a"], Addresses = [new() { Id = 1, City = "A" }] },
            new() { LastName = "Angel", Tags = ["a", "b"], Addresses = [new() { Id = 1, City = "A" }, new() { Id = 2, City = "B" }, new() { Id = 3, City = "C" }] },
            new() { LastName = "Bo", Tags = [], Addresses = [] });

        var page = AggregateQuery.Execute(
            rows,
            IndexedRequest(
                ["LastName"], new AggregateSelect(AggregateFunction.Count, "Tags", "Tags"), new AggregateSelect(AggregateFunction.Count, "TagsCount", "Tags.count"),
                new AggregateSelect(AggregateFunction.Count, "Addresses", "Addresses")));

        var angel = Row(page, "LastName", "Angel");
        Assert.Equal(3, angel["Tags"]);
        Assert.Equal(3, angel["TagsCount"]);
        Assert.Equal(4, angel["Addresses"]);
        var bo = Row(page, "LastName", "Bo");
        Assert.Equal(0, bo["Tags"]);
        Assert.Equal(0, bo["TagsCount"]);
        Assert.Equal(0, bo["Addresses"]);
    }

    [Fact]
    public void Execute_CountScalar_SkipsNullsNotDuplicates()
    {
        var people = People(new() { Name = "A", AgeNullable = 10 }, new() { Name = "A", AgeNullable = 10 }, new() { Name = "A", AgeNullable = null });
        var page = AggregateQuery.Execute(
            people,
            Request(
                ["Name"], new AggregateSelect(AggregateFunction.Count, "Ages", "AgeNullable"),
                new AggregateSelect(AggregateFunction.CountDistinct, "DistinctAges", "AgeNullable")));

        var row = Row(page, "Name", "A");
        Assert.Equal(2, row["Ages"]);
        Assert.Equal(1, row["DistinctAges"]);
    }

    [Fact]
    public void Validate_CountOfList_IsRejected()
        => AssertRejected<IndexedRow>(IndexedRequest(["LastName"], new AggregateSelect(AggregateFunction.Count, "Cities", "Addresses.City")), "list");

    [Fact]
    public void Execute_CountDistinct_SkipsNulls()
    {
        var people = People(
            new() { Name = "A", AgeNullable = 10 }, new() { Name = "A", AgeNullable = 10 }, new() { Name = "A", AgeNullable = null }, new() { Name = "B", AgeNullable = null });

        var page = AggregateQuery.Execute(people, Request(["Name"], new AggregateSelect(AggregateFunction.CountDistinct, "Ages", "AgeNullable")));
        Assert.Equal(1, Row(page, "Name", "A")["Ages"]);
        Assert.Equal(0, Row(page, "Name", "B")["Ages"]);
        var empty = AggregateQuery.Execute(People(), Request([], new AggregateSelect(AggregateFunction.CountDistinct, "Ages", "Age")));
        Assert.Equal(0, Assert.Single(empty.Items)["Ages"]);
    }

    [Fact]
    public void Validate_RejectsSortThatIsNotAColumn()
    {
        var request = Request(["Name"], new AggregateSelect(AggregateFunction.Count, "Count"));
        request.SortBy = [new("Missing", SortDirection.Asc)];
        AssertRejected(request, "Missing");
    }

    [Fact]
    public void ProjectionQueryReqBuilder_AddCountAndSum_AppendsAggregates()
    {
        var query = ProjectionQueryReqBuilder.New().AddSelects("Name").AddCount().AddSum("AgeSum", "Age").Build();
        Assert.Equal(["Name"], query.Select);
        Assert.Equal(AggregateFunction.Count, query.Aggregates[0].Function);
        Assert.Equal("Count", query.Aggregates[0].Name);
        Assert.Equal(AggregateFunction.Sum, query.Aggregates[1].Function);
        Assert.Equal("Age", query.Aggregates[1].Field);
    }

    [Fact]
    public void Clone_ProjectionQueryReq_CopiesAggregates()
    {
        var source = new ProjectionQueryReq { Select = ["Name"], Aggregates = [new(AggregateFunction.Count, "Count")] };
        var clone = QueryRequestClone.Clone(source);
        Assert.Equal("Count", clone.Aggregates[0].Name);
        Assert.NotSame(source.Aggregates, clone.Aggregates);
        Assert.NotSame(source.Aggregates[0], clone.Aggregates[0]);
    }

    private static void AssertRejected(ProjectionQueryReq request, string fragment) => AssertRejected<Person>(request, fragment);

    private static void AssertRejected<T>(ProjectionQueryReq request, string fragment)
    {
        var errors = AggregateQuery.Validate<T>(request);
        Assert.Contains(errors, e => e.Contains(fragment, StringComparison.OrdinalIgnoreCase));
        var ex = Assert.Throws<InvalidQueryException>(() => AggregateQuery.Execute(Array.Empty<T>().AsQueryable(), request));
        Assert.Contains(fragment, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static Dictionary<string, object?> Row(AggregateQueryPage page, string key, string value) => page.Items.Single(r => Equals(r[key], value));

    private static IQueryable<Person> People(params Person[] people) => people.AsQueryable();

    private static ProjectionQueryReq Request(IEnumerable<string> select, params AggregateSelect[] aggregates)
        => new() { Select = [.. select], Aggregates = [.. aggregates], Options = new() { TotalCountMode = QueryTotalCountMode.Exact } };

    private static ProjectionQueryReq IndexedRequest(IEnumerable<string> select, params AggregateSelect[] aggregates) => Request(select, aggregates);

    private static IQueryable<IndexedRow> Rows(params IndexedRow[] rows) => rows.AsQueryable();

    private sealed class IndexedRow
    {
        public string? LastName { get; set; }

        public int Age { get; set; }

        public List<string> Tags { get; set; } = [];

        public List<IndexedAddress> Addresses { get; set; } = [];

        public List<UnkeyedChild> Loose { get; set; } = [];
    }

    private sealed class IndexedAddress
    {
        public int Id { get; set; }

        public string? City { get; set; }
    }

    private sealed class UnkeyedChild
    {
        public string? Name { get; set; }
    }
}