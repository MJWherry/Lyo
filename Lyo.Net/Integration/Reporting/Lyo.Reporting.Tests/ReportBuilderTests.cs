using Lyo.Reporting.Builders;

namespace Lyo.Reporting.Tests;

public sealed class ReportBuilderTests
{
    [Fact]
    public void Build_sets_title_and_section()
    {
        var report = ReportBuilder<string>.New("opts").SetTitle("Sales").AddSection("Summary", s => s.AddColumn("Total", 10)).Build();
        Assert.Equal("Sales", report.Title);
        Assert.Equal("opts", report.Parameters);
        Assert.Single(report.Sections);
        Assert.Equal("Summary", report.Sections[0].Title);
        Assert.Equal(10, report.Sections[0].Columns[0].Value);
    }

    [Fact]
    public void Build_with_grid_rows()
    {
        var report = ReportBuilder<object>.New().SetTitle("Grid").AddSection(s => s.AddGrid("People", g => g.AddColumn("Name").AddColumn("Age").AddRow("Ada", 36))).Build();
        var grid = report.Sections[0].Grids[0];
        Assert.Equal(2, grid.Columns.Count);
        Assert.Single(grid.Rows);
        Assert.Equal("Ada", grid.Rows[0].Cells[0]);
    }
}