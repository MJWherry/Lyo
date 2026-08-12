using System.Text;
using System.Text.Json;
using Lyo.Reporting.Models.Builders;
using Lyo.Reporting.Models.Enums;
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
        _provider = services.BuildServiceProvider();
        _tempDir = Path.Combine(Path.GetTempPath(), $"lyo-report-render-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        _provider.Dispose();
        Directory.Delete(_tempDir, true);
    }

    private static string MultiGridReportJson()
        => JsonSerializer.Serialize(
            ReportBuilder<object>.New()
                .SetTitle("Multi")
                .AddSection("S1", s => s.AddGrid("People", g => g.AddColumn("Name").AddColumn("Age").AddRow("Ada", 36).AddRow("Grace", 40)))
                .AddSection("S2", s => s.AddGrid("Totals", g => g.AddColumn("Count").AddRow("2")))
                .Build());

    [Fact]
    public async Task Xlsx_renderer_writes_one_worksheet_per_grid()
    {
        var xlsx = _provider.GetRequiredService<IXlsxService>();
        var renderer = new XlsxReportRenderer(xlsx);
        var outputPath = Path.Combine(_tempDir, "multi.xlsx");
        Assert.True(renderer.CanRender(ReportFormat.Xlsx));
        Assert.False(renderer.CanRender(ReportFormat.Csv));
        var result = await renderer.RenderAsync(
            new() {
                ReportDataJson = MultiGridReportJson(),
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
    public async Task Xlsx_renderer_deduplicates_and_truncates_sheet_names()
    {
        var report = ReportBuilder<object>.New()
            .SetTitle("Dupes")
            .AddSection(s => s.AddGrid("Same", g => g.AddColumn("A").AddRow("1"))
                .AddGrid("Same", g => g.AddColumn("A").AddRow("2"))
                .AddGrid(new('L', 60), g => g.AddColumn("A").AddRow("3")))
            .Build();

        var xlsx = _provider.GetRequiredService<IXlsxService>();
        var renderer = new XlsxReportRenderer(xlsx);
        var outputPath = Path.Combine(_tempDir, "dupes.xlsx");
        await renderer.RenderAsync(
            new() { ReportDataJson = JsonSerializer.Serialize(report), Format = ReportFormat.Xlsx, OutputFilePath = outputPath }, TestContext.Current.CancellationToken);

        var sheetNames = xlsx.Reader.ListSheetNames(outputPath);
        Assert.Equal(3, sheetNames.Count);
        Assert.Equal(sheetNames.Count, sheetNames.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(sheetNames, n => Assert.True(n.Length <= 31));
    }

    [Fact]
    public async Task Json_renderer_writes_composition_json_verbatim()
    {
        var renderer = new JsonReportRenderer();
        var outputPath = Path.Combine(_tempDir, "report.json");
        var json = MultiGridReportJson();
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
        Assert.Equal(json, written);
        using var doc = JsonDocument.Parse(written);
        Assert.Equal("Multi", doc.RootElement.GetProperty("Title").GetString());
    }
}