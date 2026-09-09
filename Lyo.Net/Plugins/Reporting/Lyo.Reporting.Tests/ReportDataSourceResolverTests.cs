using System.Globalization;
using System.Text.Json;
using Lyo.Api.Services.Crud;
using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;
using Lyo.Parameters;
using Lyo.Query.Models.Parameters;
using Lyo.Reporting.Models;
using Lyo.Reporting.Models.Builders;
using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Controls;
using Lyo.Reporting.Models.Models;
using Lyo.Reporting.Models.Request;
using Lyo.Reporting.Postgres;
using Microsoft.Extensions.DependencyInjection;

namespace Lyo.Reporting.Tests;

public sealed class ReportDataSourceResolverTests
{
    [Fact]
    public async Task ApplyAsync_SprocRows_MapsOntoFromParameterTable()
    {
        var sproc = new FakeSproc {
            Rows = [new Dictionary<string, object?> { ["Name"] = "Ada", ["Total"] = 9 }]
        };
        var resolver = CreateResolver(sproc);
        var json = ReportJsonOf(
            ReportBuilder<object>.New()
                .AddSection(s => s.AddTable("Lines", t => t.AddColumn("Name", "Name").AddColumn("Total", "Total").SetFromParameter("Lines")))
                .Build());
        var merged = new List<ReportGenerationParameterReq> { new("Lines", LyoTypeInfo.JsonArray, "[]") };
        var defs = new List<ReportParameterOptionsRef> {
            new("Lines", LyoTypeInfo.JsonArray.FullName, ParameterOptionsJson.Serialize(new() { Kind = ParameterOptionsKind.Sproc, StoredProcName = "public.report_lines" }))
        };

        var updated = await resolver.ApplyAsync(json, defs, merged, TestContext.Current.CancellationToken);
        var report = ReportJson.Deserialize<object>(updated);
        var table = Assert.Single(SectionBody.CollectTables(report.Sections));
        Assert.Equal("Ada", CellText(table.Rows[0].Cells[0]));
        Assert.Equal("public.report_lines", sproc.LastName);
        Assert.Contains("Ada", merged[0].Value, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ApplyAsync_MissingParam_ClearsFromParameterTable()
    {
        var resolver = CreateResolver(new FakeSproc());
        var json = ReportJsonOf(
            ReportBuilder<object>.New()
                .AddSection(s => s.AddTable("Lines", t => t.AddColumn("Name").SetFromParameter("Missing")))
                .Build());
        var updated = await resolver.ApplyAsync(json, [], [], TestContext.Current.CancellationToken);
        var report = ReportJson.Deserialize<object>(updated);
        Assert.Empty(Assert.Single(SectionBody.CollectTables(report.Sections)).Rows);
    }

    [Fact]
    public async Task ApplyAsync_MissingSprocService_Throws()
    {
        var resolver = CreateResolver(sproc: null);
        var json = ReportJsonOf(
            ReportBuilder<object>.New()
                .AddSection(s => s.AddTable("Lines", t => t.AddColumn("Name").SetFromParameter("Lines")))
                .Build());
        var merged = new List<ReportGenerationParameterReq> { new("Lines", LyoTypeInfo.JsonArray, "[]") };
        var defs = new List<ReportParameterOptionsRef> {
            new("Lines", LyoTypeInfo.JsonArray.FullName, ParameterOptionsJson.Serialize(new() { Kind = ParameterOptionsKind.Sproc, StoredProcName = "public.report_lines" }))
        };

        var ex = await Assert.ThrowsAsync<ReportValidationException>(() => resolver.ApplyAsync(json, defs, merged, TestContext.Current.CancellationToken));
        Assert.Contains("Lines", ex.Message, StringComparison.Ordinal);
        Assert.Contains("ISprocService", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ApplyAsync_SprocPlaceholders_BindsValues()
    {
        var sproc = new FakeSproc { Rows = [] };
        var resolver = CreateResolver(sproc);
        var json = ReportJsonOf(ReportBuilder<object>.New().AddSection(s => s.AddTable("G", t => t.AddColumn("A").SetFromParameter("Lines"))).Build());
        var merged = new List<ReportGenerationParameterReq> {
            new("ClientId", LyoTypeInfo.String, "\"c-1\""),
            new("Lines", LyoTypeInfo.JsonArray, "[]")
        };
        var defs = new List<ReportParameterOptionsRef> {
            new(
                "Lines", LyoTypeInfo.JsonArray.FullName, ParameterOptionsJson.Serialize(
                    new() {
                        Kind = ParameterOptionsKind.Sproc,
                        StoredProcName = "public.report_lines",
                        SprocParameters = { ["client_id"] = "{{ClientId}}" }
                    }))
        };

        await resolver.ApplyAsync(json, defs, merged, TestContext.Current.CancellationToken);
        Assert.Equal("\"c-1\"", sproc.LastArgs?["client_id"]);
    }

    [Fact]
    public async Task ApplyAsync_FromParameterRows_MapsChartSeries()
    {
        var sproc = new FakeSproc {
            Rows = [new Dictionary<string, object?> { ["Name"] = "Ada", ["Total"] = 9 }]
        };
        var resolver = CreateResolver(sproc);
        var report = ReportBuilder<object>.New().AddSection(s => s.AddChart(ChartKind.Bar, "By name", [])).Build();
        var block = report.Sections[0].Controls.OfType<Block>().Single();
        block.DataSourceKind = DataSourceKind.FromParameter;
        block.DataParameterKey = "Lines";
        block.ChartLabelField = "Name";
        block.ChartValueField = "Total";
        var json = ReportJsonOf(report);
        var merged = new List<ReportGenerationParameterReq> { new("Lines", LyoTypeInfo.JsonArray, "[]") };
        var defs = new List<ReportParameterOptionsRef> {
            new("Lines", LyoTypeInfo.JsonArray.FullName, ParameterOptionsJson.Serialize(new() { Kind = ParameterOptionsKind.Sproc, StoredProcName = "public.report_lines" }))
        };

        var updated = await resolver.ApplyAsync(json, defs, merged, TestContext.Current.CancellationToken);
        var bound = ReportJson.Deserialize<object>(updated);
        Assert.Equal(["Ada|9"], SectionBody.WalkControls(bound.Sections[0].Controls).OfType<Block>().Single().ListItems);
    }

    [Fact]
    public async Task ApplyAsync_TableInsideGrid_FillsFromParameter()
    {
        var sproc = new FakeSproc {
            Rows = [new Dictionary<string, object?> { ["Name"] = "Ada" }]
        };
        var resolver = CreateResolver(sproc);
        var json = ReportJsonOf(
            ReportBuilder<object>.New()
                .AddSection(s => s.AddGrid(g => g.AddTable("Inner", t => t.AddColumn("Name", "Name").SetFromParameter("Lines"))))
                .Build());
        var merged = new List<ReportGenerationParameterReq> { new("Lines", LyoTypeInfo.JsonArray, "[]") };
        var defs = new List<ReportParameterOptionsRef> {
            new("Lines", LyoTypeInfo.JsonArray.FullName, ParameterOptionsJson.Serialize(new() { Kind = ParameterOptionsKind.Sproc, StoredProcName = "public.report_lines" }))
        };

        var updated = await resolver.ApplyAsync(json, defs, merged, TestContext.Current.CancellationToken);
        var table = Assert.Single(SectionBody.CollectTables(ReportJson.Deserialize<object>(updated).Sections));
        Assert.Equal("Ada", CellText(table.Rows[0].Cells[0]));
    }

    [Fact]
    public async Task ApplyAsync_TableQueryOptions_FillsRows()
    {
        var sproc = new FakeSproc {
            Rows = [new Dictionary<string, object?> { ["Name"] = "Ada" }]
        };
        var resolver = CreateResolver(sproc);
        var json = ReportJsonOf(
            ReportBuilder<object>.New()
                .AddSection(s => s.AddTable("Lines", t => t.AddColumn("Name", "Name").SetFromSproc("public.report_lines")))
                .Build());

        var updated = await resolver.ApplyAsync(json, [], [], TestContext.Current.CancellationToken);
        var table = Assert.Single(SectionBody.CollectTables(ReportJson.Deserialize<object>(updated).Sections));
        Assert.Equal("Ada", CellText(table.Rows[0].Cells[0]));
        Assert.Equal("public.report_lines", sproc.LastName);
    }

    [Fact]
    public async Task ApplyAsync_TableSprocMissingService_Throws()
    {
        var resolver = CreateResolver(sproc: null);
        var json = ReportJsonOf(
            ReportBuilder<object>.New()
                .AddSection(s => s.AddTable("Lines", t => t.AddColumn("Name").SetFromSproc("public.report_lines")))
                .Build());

        var ex = await Assert.ThrowsAsync<ReportValidationException>(() => resolver.ApplyAsync(json, [], [], TestContext.Current.CancellationToken));
        Assert.Contains("ISprocService", ex.Message, StringComparison.Ordinal);
    }

    private static IReportDataSourceResolver CreateResolver(FakeSproc? sproc)
    {
        var services = new ServiceCollection();
        if (sproc is not null)
            services.AddSingleton<ISprocService>(sproc);

        return new ReportDataSourceResolver(services.BuildServiceProvider());
    }

    private static string ReportJsonOf(Report<object> report) => ReportJson.Serialize(report);

    private static string CellText(object? value)
        => value is JsonElement el
            ? el.ValueKind == JsonValueKind.String ? el.GetString() ?? "" : el.ToString()
            : Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";

    private sealed class FakeSproc : ISprocService
    {
        public List<Dictionary<string, object?>> Rows { get; init; } = [];

        public string? LastName { get; private set; }

        public IReadOnlyDictionary<string, object?>? LastArgs { get; private set; }

        public Task<IReadOnlyList<TResult>> ExecuteStoredProcAsync<TResult>(
            string storedProcName,
            IReadOnlyDictionary<string, object?>? parameters = null,
            string[]? extraCacheTags = null,
            CancellationToken ct = default)
        {
            LastName = storedProcName;
            LastArgs = parameters;
            if (typeof(TResult) != typeof(Dictionary<string, object?>))
                return Task.FromResult<IReadOnlyList<TResult>>([]);

            return Task.FromResult((IReadOnlyList<TResult>)(object)Rows);
        }
    }
}
