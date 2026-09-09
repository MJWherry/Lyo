using Lyo.Reporting.Models.Builders;
using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Controls;

namespace Lyo.Reporting.Tests;

public sealed class ReportSectionJsonTests
{
    [Fact]
    public void Serialize_Invoice_RoundTripsTablesAndKpiCards()
    {
        var invoice = ReportDesignTemplates.Invoice();
        var back = ReportJson.Deserialize<object>(ReportJson.Serialize(invoice));
        var controls = back.Sections.SelectMany(s => SectionBody.WalkControls(s.Controls)).ToList();
        Assert.Contains(controls, c => c is Table table && table.DataSourceKind == DataSourceKind.FromParameter && table.DataParameterKey == "LineItems");
        Assert.Contains(controls, c => c is Card card && card.Label == "Invoice");
        Assert.Contains(controls, c => c is Grid grid && grid.TemplateColumns == "repeat(auto-fit, minmax(220px, 1fr))");
    }

    [Fact]
    public void Serialize_ControlsGallery_RoundTripsTablesAndKpiCards()
    {
        var gallery = ReportDesignTemplates.ControlsGallery();
        var back = ReportJson.Deserialize<object>(ReportJson.Serialize(gallery));
        var controls = back.Sections.SelectMany(s => SectionBody.WalkControls(s.Controls)).ToList();
        Assert.Contains(controls.OfType<Table>(), t => t.Title == "Milestones");
        Assert.Contains(controls.OfType<Card>(), c => c.Label == "Period");
        Assert.Contains(controls.OfType<Block>(), b => b.ContentType == ContentType.Callout);
        Assert.Contains(controls.OfType<Grid>(), g => g.TemplateColumns == "repeat(auto-fit, minmax(220px, 1fr))");
    }

    [Fact]
    public void Serialize_GridOfMixedChildren_RoundTrips()
    {
        var report = ReportBuilder<object>.New()
            .SetTitle("Mixed")
            .AddSection(
                s => s.AddGrid(
                    g => g.SetTemplateColumns("1fr 2fr")
                        .AddCard("KPI", 9)
                        .AddText("hello")
                        .AddTable(t => t.SetTitle("Lines").AddColumn("Name").AddRow("Ada"))))
            .Build();
        var back = ReportJson.Deserialize<object>(ReportJson.Serialize(report));
        var grid = Assert.Single(back.Sections[0].Controls.OfType<Grid>());
        Assert.Equal("1fr 2fr", grid.TemplateColumns);
        Assert.Equal(3, grid.Controls.Count);
        var card = Assert.IsType<Card>(grid.Controls[0]);
        Assert.Equal("KPI", card.Label);
        var block = Assert.IsType<Block>(grid.Controls[1]);
        Assert.Equal("hello", block.Content);
        var table = Assert.IsType<Table>(grid.Controls[2]);
        Assert.Equal("Lines", table.Title);
        Assert.Equal("Ada", table.Rows[0].Cells[0]);
    }

    [Fact]
    public void Deserialize_LegacyLayoutGrid_StaysGrid()
    {
        const string json = """
            {
              "title": "Layout",
              "sections": [
                {
                  "title": "S",
                  "grids": [{ "title": "kpi", "columnCount": 3, "templateColumns": "1fr 1fr 1fr" }]
                }
              ]
            }
            """;
        var report = ReportJson.Deserialize<object>(json);
        var grid = Assert.Single(report.Sections[0].Controls.OfType<Grid>());
        Assert.Equal("kpi", grid.Title);
        Assert.Equal(3, grid.ColumnCount);
        Assert.Empty(SectionBody.CollectTables(report.Sections));
    }
}
