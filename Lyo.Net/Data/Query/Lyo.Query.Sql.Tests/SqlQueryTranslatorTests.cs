using Lyo.Common.Core.Enums;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;

namespace Lyo.Query.Sql.Tests;

public class SqlQueryTranslatorTests
{
    private readonly SqlQueryTranslator _translator = new();

    [Fact]
    public void Translate_EqualsString_EmitsLoweredComparison()
    {
        var request = new QueryConcreteReq { WhereClause = new ConditionClause("Status", ComparisonOperatorEnum.Equals, "Active") };
        var sql = _translator.Translate(request, Map(("Status", "status")));
        Assert.Equal("""SELECT "status" FROM "people"."person" WHERE lower("status") = lower($1)""", sql.Text);
        Assert.Equal([new("$1", "Active")], sql.Parameters);
    }

    [Fact]
    public void Translate_EqualsNull_EmitsIsNull()
    {
        var request = new QueryConcreteReq { WhereClause = new ConditionClause("Status", ComparisonOperatorEnum.Equals, null) };
        var sql = _translator.Translate(request, Map(("Status", "status")));
        Assert.Equal("""SELECT "status" FROM "people"."person" WHERE "status" IS NULL""", sql.Text);
        Assert.Empty(sql.Parameters);
    }

    [Fact]
    public void Translate_AndOr_ParenthesizesGroups()
    {
        var where = new GroupClause(
            GroupOperatorEnum.Or,
            [
                new GroupClause(
                    GroupOperatorEnum.And,
                    [new ConditionClause("Status", ComparisonOperatorEnum.Equals, "Active"), new ConditionClause("Age", ComparisonOperatorEnum.GreaterThan, 18)]),
                new ConditionClause("Name", ComparisonOperatorEnum.Equals, "Bob")
            ]);

        var request = new QueryConcreteReq { WhereClause = where };
        var sql = _translator.Translate(request, Map(("Status", "status"), ("Age", "age"), ("Name", "name")));
        Assert.Equal("""SELECT "status", "age", "name" FROM "people"."person" WHERE ((lower("status") = lower($1)) AND ("age" > $2)) OR (lower("name") = lower($3))""", sql.Text);
        Assert.Equal([new("$1", "Active"), new("$2", 18), new("$3", "Bob")], sql.Parameters);
    }

    [Fact]
    public void Translate_Contains_EscapesLikeMetacharacters()
    {
        var request = new QueryConcreteReq { WhereClause = new ConditionClause("Name", ComparisonOperatorEnum.Contains, "100%") };
        var sql = _translator.Translate(request, Map(("Name", "name")));
        Assert.Equal("SELECT \"name\" FROM \"people\".\"person\" WHERE lower(\"name\") LIKE lower($1) ESCAPE '\\'", sql.Text);
        Assert.Equal("%100\\%%", sql.Parameters[0].Value);
        Assert.DoesNotContain("100%", sql.Text);
    }

    [Fact]
    public void Translate_In_EmitsPositionalParameters()
    {
        var request = new QueryConcreteReq { WhereClause = new ConditionClause("Age", ComparisonOperatorEnum.In, new[] { 1, 2 }) };
        var sql = _translator.Translate(request, Map(("Age", "age")));
        Assert.Equal("""SELECT "age" FROM "people"."person" WHERE "age" IN ($1, $2)""", sql.Text);
        Assert.Equal([new("$1", 1), new("$2", 2)], sql.Parameters);
    }

    [Fact]
    public void Translate_Fuzzy_EmitsSimilarity()
    {
        var request = new QueryConcreteReq { WhereClause = new ConditionClause("Name", ComparisonOperatorEnum.Fuzzy, "jon") };
        var sql = _translator.Translate(request, Map(("Name", "name")));
        Assert.Equal("""SELECT "name" FROM "people"."person" WHERE similarity(lower("name"), lower($1)) >= $2""", sql.Text);
        Assert.Equal("jon", sql.Parameters[0].Value);
        Assert.Equal(ConditionClause.DefaultFuzzyThreshold, sql.Parameters[1].Value);
    }

    [Fact]
    public void Translate_SortAndPage_EmitsOrderByOffsetLimit()
    {
        var request = new QueryConcreteReq { SortBy = [new("Name", SortDirection.Asc), new("Id", SortDirection.Desc)], Start = 20, Amount = 10 };
        var sql = _translator.Translate(request, Map(("Id", "id"), ("Name", "name")));
        Assert.Equal("""SELECT "id", "name" FROM "people"."person" ORDER BY "name" ASC, "id" DESC OFFSET 20 LIMIT 10""", sql.Text);
        Assert.Empty(sql.Parameters);
    }

    [Fact]
    public void Translate_UnknownColumn_Throws()
    {
        var request = new QueryConcreteReq { WhereClause = new ConditionClause("Missing", ComparisonOperatorEnum.Equals, "x") };
        var ex = Assert.Throws<SqlTranslationException>(() => _translator.Translate(request, Map(("Status", "status"))));
        Assert.Contains("Missing", ex.Message);
    }

    [Fact]
    public void Translate_Regex_Throws()
    {
        var request = new QueryConcreteReq { WhereClause = new ConditionClause("Name", ComparisonOperatorEnum.Regex, "a.*") };
        var ex = Assert.Throws<SqlTranslationException>(() => _translator.Translate(request, Map(("Name", "name"))));
        Assert.Contains("Regex", ex.Message);
    }

    [Fact]
    public void Translate_CollectionPath_Throws()
    {
        var request = new QueryConcreteReq { WhereClause = new ConditionClause("Lines.Quantity", ComparisonOperatorEnum.Equals, 1) };
        var ex = Assert.Throws<SqlTranslationException>(() => _translator.Translate(request, Map(("Lines", "lines"))));
        Assert.Contains("Lines.Quantity", ex.Message);
    }

    [Fact]
    public void Translate_Projection_SelectsListedColumns()
    {
        var request = new ProjectionQueryReq { Select = ["Status", "Name"] };
        var sql = _translator.Translate(request, Map(("Status", "status"), ("Name", "name"), ("Amount", "amount")));
        Assert.Equal("SELECT \"status\", \"name\" FROM \"people\".\"person\"", sql.Text);
    }

    [Fact]
    public void Translate_Projection_Aggregates_EmitsGroupBy()
    {
        var count = _translator.Translate(new ProjectionQueryReq { Aggregates = [new(AggregateFunction.Count, "count")] }, Map(("Status", "status")));
        Assert.Equal("SELECT COUNT(*) AS \"count\" FROM \"people\".\"person\"", count.Text);
        Assert.DoesNotContain("GROUP BY", count.Text);
        var sum = _translator.Translate(
            new ProjectionQueryReq { Select = ["Status"], Aggregates = [new(AggregateFunction.Sum, "total", "Amount")] }, Map(("Status", "status"), ("Amount", "amount")));

        Assert.Equal("SELECT \"status\", SUM(\"amount\") AS \"total\" FROM \"people\".\"person\" GROUP BY \"status\"", sum.Text);
    }

    [Fact]
    public void Translate_Projection_ComputedField_Throws()
    {
        var request = new ProjectionQueryReq { Select = ["Name"], ComputedFields = [new("label", "{Name}")] };
        var ex = Assert.Throws<SqlTranslationException>(() => _translator.Translate(request, Map(("Name", "name"))));
        Assert.Contains("Computed", ex.Message);
    }

    [Fact]
    public void Translate_Root_EmitsJoin()
    {
        var request = new QueryReq {
            From = new() { Alias = "o", EntityType = "OrderEntity" },
            Joins = [
                new() {
                    Alias = "p",
                    EntityType = "PersonEntity",
                    Type = JoinType.Left,
                    On = [new() { From = "o.PersonId", To = "p.Id" }]
                }
            ],
            Select = ["o.Id", "p.Name"]
        };

        var sql = _translator.Translate(request, Catalog());
        Assert.Equal(
            "SELECT \"o\".\"id\", \"p\".\"name\" FROM \"sales\".\"order\" AS \"o\" LEFT JOIN \"people\".\"person\" AS \"p\" ON \"o\".\"person_id\" = \"p\".\"id\"", sql.Text);
    }

    [Fact]
    public void Translate_Root_UnknownEntity_Throws()
    {
        var request = new QueryReq { From = new() { Alias = "o", EntityType = "Missing" }, Select = ["o.Id"] };
        var ex = Assert.Throws<SqlTranslationException>(() => _translator.Translate(request, Catalog()));
        Assert.Contains("Missing", ex.Message);
    }

    private static SqlColumnMap Map(params (string Property, string Column)[] columns)
    {
        var dict = new Dictionary<string, string>();
        foreach (var (property, column) in columns)
            dict[property] = column;

        return new("people", "person", dict);
    }

    private static Dictionary<string, SqlColumnMap> Catalog()
        => new() {
            ["OrderEntity"] = new("sales", "order", new Dictionary<string, string> { ["Id"] = "id", ["PersonId"] = "person_id" }),
            ["PersonEntity"] = new("people", "person", new Dictionary<string, string> { ["Id"] = "id", ["Name"] = "name" })
        };
}