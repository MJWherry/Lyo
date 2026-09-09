using Lyo.Reporting.Models.Builders;
using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Controls;

namespace Lyo.Reporting.Tests;

public sealed class ReportChartMarkupTests
{
    [Fact]
    public void Serialize_StructuredChart_RoundTrips()
    {
        var report = ReportBuilder<object>.New()
            .AddSection(s => s.AddChart(ChartKind.Bar, "Sales", ["Q1|10", "Q2|20"]))
            .Build();
        var json = ReportJson.Serialize(report);
        var back = ReportJson.Deserialize<object>(json);
        var block = Assert.Single(back.Sections[0].Controls.OfType<Block>());
        Assert.Equal(ContentType.Chart, block.ContentType);
        Assert.Equal(ChartKind.Bar, block.ChartKind);
        Assert.Equal("Sales", block.Caption);
        Assert.Equal(["Q1|10", "Q2|20"], block.ListItems);
    }

    [Fact]
    public void Deserialize_LegacyHtmlChart_HasNoKind()
    {
        const string json = """{"title":"Old","sections":[{"contentBlocks":[{"contentType":6,"content":"<canvas data-lyo-chart=\"{}\"></canvas>"}]}]}""";
        var report = ReportJson.Deserialize<object>(json);
        var block = Assert.Single(report.Sections[0].Controls.OfType<Block>());
        Assert.Equal(ContentType.Chart, block.ContentType);
        Assert.Null(block.ChartKind);
        Assert.Contains("data-lyo-chart", block.Content, StringComparison.Ordinal);
        Assert.Null(ReportChartMarkup.TryBuild(block));
    }

    [Fact]
    public void TryBuild_Bar_EmitsCanvas()
    {
        var html = ReportChartMarkup.TryBuild(new() { ChartKind = ChartKind.Bar, Caption = "T", ListItems = ["A|1", "B|2"] });
        Assert.NotNull(html);
        Assert.Contains("data-lyo-chart", html, StringComparison.Ordinal);
        Assert.Contains("bar", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("T", html, StringComparison.Ordinal);
    }

    [Fact]
    public void TryBuild_MissingKind_ReturnsNull()
        => Assert.Null(ReportChartMarkup.TryBuild(new() { ContentType = ContentType.Chart, ListItems = ["A|1"] }));

    [Fact]
    public void ApplyFromRows_LabelAndValue_MapsSeries()
    {
        var block = new Block { ChartKind = ChartKind.Line, ChartLabelField = "Name", ChartValueField = "Total" };
        ReportChartMarkup.ApplyFromRows(block, [new Dictionary<string, object?> { ["Name"] = "Ada", ["Total"] = 9 }]);
        Assert.Equal(["Ada|9"], block.ListItems);
        Assert.NotNull(ReportChartMarkup.TryBuild(block));
    }

    [Theory]
    [InlineData(ChartKind.Sparkline, "line")]
    [InlineData(ChartKind.Gauge, "doughnut")]
    public void TryBuild_SparklineAndGauge_EmitsExpectedChartType(ChartKind kind, string expectedFragment)
        => Assert.Contains(expectedFragment, ReportChartMarkup.TryBuild(new() { ChartKind = kind, ListItems = ["A|1", "B|2"] })!, StringComparison.Ordinal);

    [Fact]
    public void SeriesFromRows_SourceBlock_IsUnchanged()
    {
        var block = new Block { ListItems = ["keep"] };
        var series = ReportChartMarkup.SeriesFromRows(block, [new Dictionary<string, object?> { ["label"] = "A", ["value"] = 1 }]);
        Assert.Equal(["keep"], block.ListItems);
        Assert.Equal(["A|1"], series);
    }

    [Fact]
    public void StableCanvasId_SameInstance_IsStable()
    {
        var block = new Block { ChartKind = ChartKind.Bar, ListItems = ["A|1"] };
        Assert.Equal(ReportChartMarkup.StableCanvasId(block), ReportChartMarkup.StableCanvasId(block));
        var copy = new Block { ChartKind = ChartKind.Bar, ListItems = ["A|1"] };
        Assert.NotEqual(ReportChartMarkup.StableCanvasId(block), ReportChartMarkup.StableCanvasId(copy));
    }
}
