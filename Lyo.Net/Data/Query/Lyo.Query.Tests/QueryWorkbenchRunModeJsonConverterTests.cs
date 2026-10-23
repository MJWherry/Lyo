using System.Text.Json;
using Lyo.Query.Web.Components;

namespace Lyo.Query.Tests;

public sealed class QueryWorkbenchRunModeJsonConverterTests
{
    [Theory]
    [InlineData("QuerySelect")]
    [InlineData("QueryConcrete")]
    [InlineData("QueryRoot")]
    public void Read_OldNames_Throw(string name)
    {
        var json = "{\"RunMode\":\"" + name + "\"}";
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<QueryWorkbenchRunConfiguration>(json));
    }

    [Fact]
    public void Read_NumericOrdinal_Throws()
    {
        const string json = """{"RunMode":1}""";
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<QueryWorkbenchRunConfiguration>(json));
    }

    [Fact]
    public void Read_QueryProject_Maps()
    {
        const string json = """{"RunMode":"QueryProject"}""";
        var config = JsonSerializer.Deserialize<QueryWorkbenchRunConfiguration>(json);
        Assert.NotNull(config);
        Assert.Equal(QueryWorkbenchRunMode.QueryProject, config.RunMode);
    }
}