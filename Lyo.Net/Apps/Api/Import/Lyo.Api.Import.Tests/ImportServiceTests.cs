using Lyo.Api.Import.Csv;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Enums;
using Lyo.Json;

namespace Lyo.Api.Import.Tests;

public class ImportServiceTests
{
    private sealed class Person
    {
        public string? FirstName { get; set; }

        public int Age { get; set; }
    }

    private sealed class RecordingCommitter : IImportCommitter
    {
        public int SaveCount { get; private set; }

        public void Save<T>(IReadOnlyList<T> rows) => SaveCount++;
    }

    private static ImportService Service(int maxRows = 100) => new(
        [new JsonImportFormatHandler(), new CsvImportFormatHandler()],
        new ImportOptions { MaxImportRows = maxRows });

    [Fact]
    public void Import_JsonArray_EmptyMap_RoundTripsExportShape()
    {
        var json = new JsonService().ExportToJsonString(new[] { new Person { FirstName = "Ada", Age = 10 } });
        var result = Service().Import<Person>(new() { Format = ImportFormat.Json, Content = json });
        Assert.True(result.IsSuccess);
        Assert.Equal("Ada", result.Rows[0].FirstName);
        Assert.Equal(10, result.Rows[0].Age);
    }

    [Fact]
    public void Import_Json_WrappedRowsPath_ReadsItems()
    {
        const string json = """{"items":[{"firstName":"Ada","age":4}]}""";
        var result = Service().Import<Person>(new() { Format = ImportFormat.Json, JsonMode = ImportJsonMode.Wrapped, RowsPath = "items", Content = json });
        Assert.True(result.IsSuccess);
        Assert.Equal("Ada", result.Rows[0].FirstName);
        Assert.Equal(4, result.Rows[0].Age);
    }

    [Fact]
    public void Import_Json_Ndjson_ReturnsRowsAndLineIssues()
    {
        const string ndjson = """
            {"firstName":"Ada","age":1}
            not json
            {"firstName":"Bea","age":2}
            """;
        var result = Service().Import<Person>(new() { Format = ImportFormat.Json, JsonMode = ImportJsonMode.Ndjson, Content = ndjson });
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Rows.Count);
        Assert.Equal(1, result.Issues.Single().Index);
    }

    [Fact]
    public void Import_Csv_UnmappedHeaders_BindProperties()
    {
        var result = Service().Import<Person>(new() { Format = ImportFormat.Csv, Content = "FirstName,Age\nAda,10" });
        Assert.True(result.IsSuccess);
        Assert.Equal("Ada", result.Rows[0].FirstName);
        Assert.Equal(10, result.Rows[0].Age);
    }

    [Fact]
    public void Import_Map_RenamesProperty()
    {
        var json = Service().Import<Person>(new() {
            Format = ImportFormat.Json,
            Content = """[{"first":"Ada"}]""",
            Fields = [new ImportFieldMap { Source = "first", Target = "FirstName" }]
        });
        Assert.Equal("Ada", json.Rows[0].FirstName);

        var csv = Service().Import<Person>(new() {
            Format = ImportFormat.Csv,
            Content = "First\nAda",
            Fields = [new ImportFieldMap { Source = "First", Target = "FirstName" }]
        });
        Assert.Equal("Ada", csv.Rows[0].FirstName);
    }

    [Fact]
    public void Import_DuplicateTarget_IsDocumentError()
    {
        var result = Service().Import<Person>(new() {
            Format = ImportFormat.Json,
            Content = """[{"first":"Ada"}]""",
            Fields = [
                new ImportFieldMap { Source = "first", Target = "FirstName" },
                new ImportFieldMap { Source = "given", Target = "FirstName" }
            ]
        });
        Assert.False(result.IsSuccess);
        Assert.Empty(result.Rows);
    }

    [Fact]
    public void Import_OverRowCap_IsDocumentError()
    {
        const string json = """[{"firstName":"A","age":1},{"firstName":"B","age":2},{"firstName":"C","age":3}]""";
        var result = Service(maxRows: 2).Import<Person>(new() { Format = ImportFormat.Json, Content = json });
        Assert.False(result.IsSuccess);
        Assert.Empty(result.Rows);
    }

    [Fact]
    public void Import_UnknownFormat_IsDocumentError()
    {
        var result = Service().Import<Person>(new() { Format = ImportFormat.Xlsx, ContentBytes = [1, 2, 3] });
        Assert.False(result.IsSuccess);
        Assert.Empty(result.Rows);
    }

    [Fact]
    public void Import_ErrorReport_OneCsvLinePerIssue()
    {
        const string ndjson = """
            {"firstName":"Ada","age":1}
            not json
            {"firstName":"Bea","age":2}
            """;
        var service = Service();
        var result = service.Import<Person>(new() { Format = ImportFormat.Json, JsonMode = ImportJsonMode.Ndjson, Content = ndjson });
        var report = service.WriteErrorReport(result.Issues);
        var lines = report.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal("Index,Field,Message,Raw", lines[0]);
        Assert.Equal(result.Issues.Count, lines.Length - 1);
    }

    [Fact]
    public void Import_DoesNotWriteStore()
    {
        var committer = new RecordingCommitter();
        var service = new ImportService([new JsonImportFormatHandler()], new ImportOptions(), committer);
        var json = new JsonService().ExportToJsonString(new[] { new Person { FirstName = "Ada", Age = 1 } });
        var result = service.Import<Person>(new() { Format = ImportFormat.Json, Content = json });
        Assert.True(result.IsSuccess);
        Assert.Equal(0, committer.SaveCount);
    }
}
