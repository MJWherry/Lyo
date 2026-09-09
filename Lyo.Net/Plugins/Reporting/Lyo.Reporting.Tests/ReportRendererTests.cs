using System.Text;
using System.Text.Json;
using Lyo.Csv;
using Lyo.Csv.Models;
using Lyo.Reporting.Models;
using Lyo.Reporting.Models.Builders;
using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Models.Response;
using Lyo.Reporting.Postgres.Rendering;
using Lyo.Xlsx;
using Lyo.Xlsx.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Reporting.Tests;

public sealed class ReportRendererTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly string _tempDir;

    public ReportRendererTests()
    {
        // ExcelDataReader (Xlsx read-back verification) requires legacy code pages.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddXlsxService();
        services.AddCsvService();
        _provider = services.BuildServiceProvider();
        _tempDir = Path.Combine(Path.GetTempPath(), $"lyo-report-render-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        _provider.Dispose();
        Directory.Delete(_tempDir, true);
    }

    private static string MultiTableReportJson()
        => ReportJson.Serialize(
            ReportBuilder<object>.New()
                .SetTitle("Multi")
                .AddSection("S1", s => s.AddTable("People", t => t.AddColumn("Name").AddColumn("Age").AddRow("Ada", 36).AddRow("Grace", 40)))
                .AddSection("S2", s => s.AddTable("Totals", t => t.AddColumn("Count").AddRow("2")))
                .Build());

    [Fact]
    public async Task RenderAsync_Xlsx_WritesOneWorksheetPerTable()
    {
        var xlsx = _provider.GetRequiredService<IXlsxService>();
        var renderer = new XlsxReportRenderer(xlsx);
        var outputPath = Path.Combine(_tempDir, "multi.xlsx");
        Assert.True(renderer.CanRender(ReportFormat.Xlsx));
        Assert.False(renderer.CanRender(ReportFormat.Csv));
        var result = await renderer.RenderAsync(
            new() {
                ReportDataJson = MultiTableReportJson(),
                Format = ReportFormat.Xlsx,
                OutputFilePath = outputPath,
                SuggestedFileName = "multi.xlsx"
            }, TestContext.Current.CancellationToken);

        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.ContentType);
        Assert.True(new FileInfo(outputPath).Length > 0);
        var sheetNames = xlsx.Reader.ListSheetNames(outputPath);
        Assert.Equal(["People", "Totals"], sheetNames);

        // Reader consumes the header row by default, so index 0 is the first data row.
        var people = xlsx.Reader.ParseXlsxFileAsDictionary(outputPath, "People");
        Assert.Equal("Ada", people[0][0]);
        Assert.Equal("40", people[1][1]);
        var table = xlsx.Reader.ParseXlsxFileAsDataTable(outputPath, "People", true);
        Assert.True(table.IsSuccess);
        Assert.Equal("Name", table.Data!.Headers[0].DisplayValue);
        Assert.Equal("Age", table.Data.Headers[1].DisplayValue);
    }

    [Fact]
    public async Task RenderAsync_Xlsx_DeduplicatesAndTruncatesSheetNames()
    {
        var report = ReportBuilder<object>.New()
            .SetTitle("Dupes")
            .AddSection(s => s.AddTable("Same", t => t.AddColumn("A").AddRow("1"))
                .AddTable("Same", t => t.AddColumn("A").AddRow("2"))
                .AddTable(new('L', 60), t => t.AddColumn("A").AddRow("3")))
            .Build();

        var xlsx = _provider.GetRequiredService<IXlsxService>();
        var renderer = new XlsxReportRenderer(xlsx);
        var outputPath = Path.Combine(_tempDir, "dupes.xlsx");
        await renderer.RenderAsync(
            new() { ReportDataJson = ReportJson.Serialize(report), Format = ReportFormat.Xlsx, OutputFilePath = outputPath }, TestContext.Current.CancellationToken);

        var sheetNames = xlsx.Reader.ListSheetNames(outputPath);
        Assert.Equal(3, sheetNames.Count);
        Assert.Equal(sheetNames.Count, sheetNames.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(sheetNames, n => Assert.True(n.Length <= 31));
    }

    [Fact]
    public async Task RenderAsync_Csv_ExportsEveryTable()
    {
        var renderer = new CsvReportRenderer(_provider.GetRequiredService<ICsvService>());
        var outputPath = Path.Combine(_tempDir, "multi.csv");
        var result = await renderer.RenderAsync(
            new() {
                ReportDataJson = MultiTableReportJson(),
                Format = ReportFormat.Csv,
                OutputFilePath = outputPath,
                SuggestedFileName = "multi.csv"
            }, TestContext.Current.CancellationToken);

        Assert.Equal(ReportFormat.Csv.ContentType, result.ContentType);
        var csv = await File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
        Assert.Contains("Ada", csv, StringComparison.Ordinal);
        Assert.Contains("Grace", csv, StringComparison.Ordinal);

        // The second grid once was dropped entirely.
        Assert.Contains("Totals", csv, StringComparison.Ordinal);
        Assert.Contains("Count", csv, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RenderAsync_Csv_WritesTableLargerThanFlushChunk()
    {
        // The renderer flushes every 2,000 rows; a table spanning several chunks must come out as one continuous CSV with no repeated header or stray encoding preamble.
        const int rowCount = 5_000;
        var builder = ReportBuilder<object>.New().SetTitle("Big");
        var json = ReportJson.Serialize(
            builder.AddSection(
                    "S", s => s.AddTable(
                        "Rows", t => {
                            t.AddColumn("Index");
                            for (var i = 0; i < rowCount; i++)
                                t.AddRow(i.ToString());
                        }))
                .Build());

        var renderer = new CsvReportRenderer(_provider.GetRequiredService<ICsvService>());
        var outputPath = Path.Combine(_tempDir, "big.csv");
        await renderer.RenderAsync(
            new() { ReportDataJson = json, Format = ReportFormat.Csv, OutputFilePath = outputPath }, TestContext.Current.CancellationToken);

        var lines = await File.ReadAllLinesAsync(outputPath, TestContext.Current.CancellationToken);
        var dataLines = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
        Assert.Equal(rowCount + 1, dataLines.Count);
        Assert.Equal("Index", dataLines[0]);
        Assert.Equal("0", dataLines[1]);
        Assert.Equal((rowCount - 1).ToString(), dataLines[^1]);
        Assert.Single(dataLines, l => l == "Index");
    }

    [Fact]
    public async Task RenderAsync_CsvWithoutTable_FailsValidation()
    {
        var renderer = new CsvReportRenderer(_provider.GetRequiredService<ICsvService>());
        var json = ReportJson.Serialize(ReportBuilder<object>.New().SetTitle("No tables").AddSection("S", s => s.AddText("nothing here")).Build());
        await Assert.ThrowsAsync<ReportValidationException>(() => renderer.RenderAsync(
            new() { ReportDataJson = json, Format = ReportFormat.Csv, OutputFilePath = Path.Combine(_tempDir, "empty.csv") }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RenderAsync_Xlsx_HonorsCancellation()
    {
        var renderer = new XlsxReportRenderer(_provider.GetRequiredService<IXlsxService>());
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => renderer.RenderAsync(
            new() { ReportDataJson = MultiTableReportJson(), Format = ReportFormat.Xlsx, OutputFilePath = Path.Combine(_tempDir, "cancelled.xlsx") }, cts.Token));
    }

    [Fact]
    public async Task RenderAsync_Json_WritesCompositionVerbatim()
    {
        var renderer = new JsonReportRenderer();
        var outputPath = Path.Combine(_tempDir, "report.json");
        var json = MultiTableReportJson();
        Assert.True(renderer.CanRender(ReportFormat.Json));
        Assert.False(renderer.CanRender(ReportFormat.Pdf));
        var result = await renderer.RenderAsync(
            new() {
                ReportDataJson = json,
                Format = ReportFormat.Json,
                OutputFilePath = outputPath,
                SuggestedFileName = "report.json"
            }, TestContext.Current.CancellationToken);

        Assert.Equal("application/json; charset=utf-8", result.ContentType);
        var written = await File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(written);
        Assert.Equal("Multi", doc.RootElement.GetProperty("Title").GetString());
    }

    [Fact]
    public async Task RenderAsync_Json_InterpolatesParameters()
    {
        var json = ReportJson.Serialize(ReportBuilder<object>.New().SetTitle("{Period}").AddSection("S", s => s.AddTable("G", t => t.AddColumn("A").AddRow("{Period}"))).Build());
        var renderer = new JsonReportRenderer();
        var outputPath = Path.Combine(_tempDir, "bound.json");
        await renderer.RenderAsync(
            new() {
                ReportDataJson = json,
                Format = ReportFormat.Json,
                OutputFilePath = outputPath,
                Parameters = [new(Guid.Empty, Guid.Empty, "Period", "System.String", "\"Q4\"", null, null)]
            }, TestContext.Current.CancellationToken);

        var written = await File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(written);
        Assert.Equal("Q4", doc.RootElement.GetProperty("Title").GetString());
    }
}