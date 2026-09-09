using Lyo.Reporting.Models.Builders;
using Lyo.Reporting.Models.Controls;
using Lyo.Reporting.Models.Enums;

namespace Lyo.Reporting.Tests;

public sealed class ReportBuilderTests
{
    [Fact]
    public void Build_TitleAndKpiGrid_SetsBoth()
    {
        var report = ReportBuilder<string>.New("opts")
            .SetTitle("Sales")
            .AddSection(
                "Summary",
                s => s.AddGrid(g => g.SetTemplateColumns("repeat(auto-fit, minmax(220px, 1fr))").AddCard("Total", 10)))
            .Build();
        Assert.Equal("Sales", report.Title);
        Assert.Equal("opts", report.Parameters);
        Assert.Single(report.Sections);
        Assert.Equal("Summary", report.Sections[0].Title);
        var grid = Assert.Single(report.Sections[0].Controls.OfType<Grid>());
        Assert.Equal("repeat(auto-fit, minmax(220px, 1fr))", grid.TemplateColumns);
        Assert.Equal(10, Assert.Single(grid.Controls.OfType<Card>()).Value);
    }

    [Fact]
    public void Build_TableRows_AddsCells()
    {
        var report = ReportBuilder<object>.New()
            .SetTitle("Table")
            .AddSection(s => s.AddTable("People", t => t.AddColumn("Name").AddColumn("Age").AddRow("Ada", 36)))
            .Build();
        var table = Assert.Single(report.Sections[0].Controls.OfType<Table>());
        Assert.Equal(2, table.Columns.Count);
        Assert.Single(table.Rows);
        Assert.Equal("Ada", table.Rows[0].Cells[0]);
    }

    [Fact]
    public void Build_LayoutParametersAndControls_SetsFields()
    {
        var report = ReportBuilder<object>.New()
            .SetTitle("{Title}")
            .SetLayout(l => l.SetTheme("Formal").SetWatermark("DRAFT").ShowPageNumbers())
            .AddParameter("Title", example: "\"Demo\"", required: true)
            .AddSection(
                "Body",
                s => s.SetVisibleWhen("Title")
                    .AddHeading("Hi", 3)
                    .AddCallout("Note", "info")
                    .AddProgress(40, "Done")
                    .AddKeyValue("Owner", "Ada")
                    .AddDivider())
            .Build();

        Assert.Equal("Formal", report.Layout.Theme);
        Assert.True(report.Layout.ShowPageNumbers);
        Assert.Single(report.ParameterSpecs);
        var blocks = report.Sections[0].Controls.OfType<Block>().ToList();
        Assert.Equal(ContentType.Heading, blocks[0].ContentType);
        Assert.Equal(ContentType.KeyValue, blocks[3].ContentType);
    }

    [Fact]
    public void ToDefinitionReq_CompositionAndParameterSpecs_Serializes()
    {
        var req = ReportBuilder<object>.New()
            .SetTitle("Q1")
            .AddParameter("ClientId", Lyo.Common.Metadata.Records.LyoTypeInfo.Guid.FullName, required: true)
            .AddSection("Body", s => s.AddText("Hello"))
            .ToDefinitionReq("Quarterly", "desc", ReportFormat.Html);

        Assert.Equal("Quarterly", req.Name);
        Assert.Equal("desc", req.Description);
        Assert.Equal(ReportFormat.Html, req.DefaultFormat);
        Assert.Contains("Q1", req.ReportDataJson, StringComparison.Ordinal);
        Assert.Single(req.CreateParameters);
        Assert.Equal("ClientId", req.CreateParameters[0].Key);
        Assert.True(req.CreateParameters[0].Required);
    }

    [Fact]
    public void ToDefinitionReq_MissingDefault_CopiesExampleValue()
    {
        var req = ReportBuilder<object>.New()
            .AddParameter("Period", example: "\"Q4 2024\"")
            .ToDefinitionReq("Demo");

        Assert.Equal("\"Q4 2024\"", req.CreateParameters[0].Value);
    }

    [Fact]
    public void AddChartFromParameter_FromParameterFields_AreSet()
    {
        var report = ReportBuilder<object>.New()
            .AddSection("Body", s => s.AddChartFromParameter(ChartKind.Bar, "RegionSeries", "By region", "label", "value", 240))
            .Build();

        var block = Assert.Single(report.Sections[0].Controls.OfType<Block>());
        Assert.Equal(ContentType.Chart, block.ContentType);
        Assert.Equal(ChartKind.Bar, block.ChartKind);
        Assert.Equal(DataSourceKind.FromParameter, block.DataSourceKind);
        Assert.Equal("RegionSeries", block.DataParameterKey);
        Assert.Equal("label", block.ChartLabelField);
        Assert.Equal("value", block.ChartValueField);
        Assert.Equal(240, block.Level);
        Assert.Equal("By region", block.Caption);
    }

    [Fact]
    public void AddCardAndAddTableOnGrid_BuildsMixedChildren()
    {
        var report = ReportBuilder<object>.New()
            .AddSection(
                s => s.AddGrid(
                    "Cluster",
                    g => g.AddCard("A", 1).AddTable(t => t.AddColumn("H").AddRow("v")).AddText("note")))
            .Build();
        var grid = Assert.Single(report.Sections[0].Controls.OfType<Grid>());
        Assert.Equal("Cluster", grid.Title);
        Assert.Equal(3, grid.Controls.Count);
        Assert.IsType<Card>(grid.Controls[0]);
        Assert.IsType<Table>(grid.Controls[1]);
        Assert.IsType<Block>(grid.Controls[2]);
    }
}
