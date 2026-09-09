using System.Text.Json;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Lyo.Parameters;
using Lyo.Query.Models.Parameters;

namespace Lyo.Query.Tests;

public sealed class ParameterOptionsBinderTests
{
    [Fact]
    [Trait("Category", "Fast")]
    public void TryBind_ReplacesPlaceholders_AndReportsMissing()
    {
        var template = new QueryReq {
            From = new() { Alias = "c", EntityType = "Client" },
            Select = ["c.Id", "c.Name"],
            WhereClause = new ConditionClause("c.TenantId", ComparisonOperatorEnum.Equals, "{{TenantId}}"),
            Amount = 50
        };

        Assert.False(ParameterOptionsBinder.TryBind(template, new Dictionary<string, string?>(), out var _, out var missing));
        Assert.Equal(["TenantId"], missing);
        Assert.True(ParameterOptionsBinder.TryBind(template, new Dictionary<string, string?> { ["TenantId"] = "t-1" }, out var bound, out var none));
        Assert.Empty(none);
        Assert.NotNull(bound);
        var condition = Assert.IsType<ConditionClause>(bound!.WhereClause);
        Assert.Equal("t-1", condition.Value);
        Assert.Equal("{{TenantId}}", Assert.IsType<ConditionClause>(template.WhereClause).Value);
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void TryReadKeyValue_PrefersComputedKeyValue_ThenSelectFallback()
    {
        var row = new Dictionary<string, object?> {
            ["Key"] = "id-1",
            ["Value"] = "Acme",
            ["Id"] = "ignored",
            ["Name"] = "ignored"
        };

        Assert.True(ParameterOptionsBinder.TryReadKeyValue(row, ["c.Id", "c.Name"], out var key, out var label));
        Assert.Equal("id-1", key);
        Assert.Equal("Acme", label);
        var row2 = new Dictionary<string, object?> { ["Id"] = "id-2", ["Name"] = "Beta" };
        Assert.True(ParameterOptionsBinder.TryReadKeyValue(row2, ["c.Id", "c.Name"], out key, out label));
        Assert.Equal("id-2", key);
        Assert.Equal("Beta", label);
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void FromAllowedValues_ParsesJsonArray()
    {
        var items = ParameterOptionsBinder.FromAllowedValues("""["A","B","C"]""");
        Assert.Equal(3, items.Count);
        Assert.Equal("B", items[1].Key);
        Assert.Equal("B", items[1].Label);
        Assert.Empty(ParameterOptionsBinder.FromAllowedValues("A|B|C"));
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void ParameterOptionsJson_RoundTripsStaticAndQuery()
    {
        var staticOpts = new ParameterOptions { Kind = ParameterOptionsKind.Static, Items = [new("full", "Full sync"), new("delta", "Delta")] };
        var staticJson = ParameterOptionsJson.Serialize(staticOpts);
        var staticBack = ParameterOptionsJson.Deserialize(staticJson);
        Assert.NotNull(staticBack);
        Assert.Equal(ParameterOptionsKind.Static, staticBack!.Kind);
        Assert.Equal(2, staticBack.Items.Count);
        var queryOpts = new ParameterOptions {
            Kind = ParameterOptionsKind.Query,
            QueryRoute = "api/People/Query",
            Query = new() {
                From = new() { Alias = "c", EntityType = "Client" },
                Select = ["c.Id", "c.Name"],
                ComputedFields = [new("Key", "{c.Id}"), new("Value", "{c.Name}")],
                Amount = 200
            }
        };

        var queryJson = ParameterOptionsJson.Serialize(queryOpts);
        Assert.Contains("\"kind\":\"query\"", queryJson, StringComparison.OrdinalIgnoreCase);
        var queryBack = ParameterOptionsJson.Deserialize(queryJson);
        Assert.NotNull(queryBack);
        Assert.Equal(ParameterOptionsKind.Query, queryBack!.Kind);
        Assert.Equal("api/People/Query", queryBack.QueryRoute);
        Assert.Equal("Client", queryBack.Query!.From.EntityType);
        Assert.Equal("Query", new ParameterOptions { Kind = ParameterOptionsKind.Query }.EffectiveQueryRoute);
        using var _ = JsonDocument.Parse(queryJson!);
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void ParameterOptionsJson_RoundTripsSproc_AndRejectsBadName()
    {
        var opts = new ParameterOptions {
            Kind = ParameterOptionsKind.Sproc,
            StoredProcName = "public.report_lines",
            KeyField = "Id",
            LabelField = "Name",
            SprocParameters = { ["client_id"] = "{{ClientId}}" }
        };

        var json = ParameterOptionsJson.Serialize(opts);
        Assert.Contains("\"kind\":\"sproc\"", json, StringComparison.OrdinalIgnoreCase);
        var back = ParameterOptionsJson.Deserialize(json);
        Assert.Equal(ParameterOptionsKind.Sproc, back!.Kind);
        Assert.Equal("public.report_lines", back.StoredProcName);
        Assert.Equal("{{ClientId}}", back.SprocParameters["client_id"]);
        ParameterOptionsJson.Validate(back);
        var bad = new ParameterOptions { Kind = ParameterOptionsKind.Sproc, StoredProcName = "drop table" };
        Assert.Throws<ArgumentException>(() => ParameterOptionsJson.Validate(bad));
        Assert.False(ParameterOptionsJson.IsValidStoredProcName("report_lines"));
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void TryBindSprocParameters_ReplacesPlaceholders()
    {
        Assert.False(ParameterOptionsBinder.TryBindSprocParameters(new Dictionary<string, string> { ["a"] = "{{TenantId}}" }, new Dictionary<string, string?>(), out _, out var missing));
        Assert.Equal(["TenantId"], missing);
        Assert.True(
            ParameterOptionsBinder.TryBindSprocParameters(
                new Dictionary<string, string> { ["a"] = "{{TenantId}}" }, new Dictionary<string, string?> { ["TenantId"] = "t-1" }, out var bound, out var none));
        Assert.Empty(none);
        Assert.Equal("t-1", bound["a"]);
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void GetInputParameterKeys_FindsNestedPlaceholders()
    {
        var query = new QueryReq {
            From = new() { Alias = "c", EntityType = "Client", Query = new() { WhereClause = new ConditionClause("TenantId", ComparisonOperatorEnum.Equals, "{{TenantId}}") } },
            Select = ["c.Id"],
            WhereClause = new GroupClause(GroupOperatorEnum.And, [new ConditionClause("c.Region", ComparisonOperatorEnum.Equals, "{{Region}}")])
        };

        var keys = ParameterOptionsBinder.GetInputParameterKeys(query);
        Assert.Contains("TenantId", keys);
        Assert.Contains("Region", keys);
    }

    [Fact]
    [Trait("Category", "Fast")]
    public void FromAllowedValues_AndToAllowedValues_RoundTripStaticKeys()
    {
        var options = ParameterOptionsJson.FromAllowedValues("""["AB","CD"]""");
        Assert.Equal(ParameterOptionsKind.Static, ParameterOptionsJson.TryGetKind(options));
        Assert.Equal("""["AB","CD"]""", ParameterOptionsJson.ToAllowedValues(options));
        Assert.Null(ParameterOptionsJson.FromAllowedValues(null));
        Assert.Null(ParameterOptionsJson.ToAllowedValues(ParameterOptionsJson.CreateDefaultForKind(ParameterOptionsKind.Query)));
    }
}