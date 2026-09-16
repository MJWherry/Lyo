namespace Lyo.Json.Tests;

public class JsonImportResultTests
{
    private sealed class WhenRow
    {
        public DateTime When { get; set; }
    }

    [Fact]
    public void Parse_JsonArray_BadElement_SkipsThatElement()
    {
        const string json = """[{"when":"2020-01-02T00:00:00Z"},"nope",{"when":"2020-01-03T00:00:00Z"}]""";
        var result = JsonService.Default.ParseArrayAsResult<WhenRow>(json);
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Rows.Count);
        Assert.Equal(1, result.Issues.Single().Index);
    }

    [Fact]
    public void Parse_JsonArray_BadProperty_SkipsThatElement()
    {
        const string json = """[{"when":1},{"when":"2020-01-03T00:00:00Z"}]""";
        var result = JsonService.Default.ParseArrayAsResult<WhenRow>(json);
        Assert.True(result.IsSuccess);
        Assert.Single(result.Rows);
        Assert.Equal(new DateTime(2020, 1, 3, 0, 0, 0, DateTimeKind.Utc), result.Rows[0].When.ToUniversalTime());
        Assert.Equal(0, result.Issues.Single().Index);
    }

    [Fact]
    public void Parse_Ndjson_BadLine_SkipsThatLine()
    {
        const string ndjson = """
            {"when":"2020-01-02T00:00:00Z"}
            not json
            {"when":"2020-01-03T00:00:00Z"}
            """;
        var result = JsonService.Default.ParseNdjsonAsResult<WhenRow>(ndjson);
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Rows.Count);
        Assert.Equal(1, result.Issues.Single().Index);
    }

    [Fact]
    public void Parse_Json_RootObjectWithoutRowsPath_IsDocumentError()
    {
        var result = JsonService.Default.ParseArrayAsResult<WhenRow>("""{"when":"2020-01-02T00:00:00Z"}""");
        Assert.False(result.IsSuccess);
        Assert.Empty(result.Rows);
    }

    [Fact]
    public void Parse_Json_Null_IsDocumentError()
    {
        var result = JsonService.Default.ParseArrayAsResult<WhenRow>("null");
        Assert.False(result.IsSuccess);
        Assert.Empty(result.Rows);
    }
}
