namespace Lyo.DataTable.Models.Tests;

public sealed class DataTableBuilderTests
{
    [Fact]
    public void Build_creates_table_with_headers_and_rows()
    {
        var table = new DataTableBuilder().AddHeaders("Name", "Amount").AddRow(r => r.AddCells("Alice", "10")).AddRow(r => r.AddCells("Bob", "20")).Build();
        Assert.Equal(2, table.Headers.Count);
        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("Alice", table.Rows[0].Cells[0].DisplayValue);
        Assert.Equal("10", table.Rows[0].Cells[1].DisplayValue);
    }

    [Fact]
    public void AddSumFooter_computes_sum_at_build()
    {
        var table = new DataTableBuilder().AddColumn(1, c => c.WithSumFooter())
            .AddHeaders("Name", "Amount")
            .AddRow(r => r.AddCells("A", "10"))
            .AddRow(r => r.AddCells("B", "20"))
            .Build();

        Assert.Equal("30", table.Footer[1].DisplayValue);
    }

    [Fact]
    public void AddRow_with_builder_chains_cells()
    {
        var table = new DataTableBuilder().AddHeaders("Col0", "Col1").AddRow().SetCell(0, "x").SetCell(1, 42).BuildAndAdd().Build();
        Assert.Single(table.Rows);
        Assert.Equal("x", table.Rows[0].Cells[0].DisplayValue);
        Assert.Equal("42", table.Rows[0].Cells[1].DisplayValue);
    }

    [Fact]
    public void AddFooter_sets_footer_cells()
    {
        var table = new DataTableBuilder().AddHeaders("H1").AddFooters("Total").Build();
        Assert.Single(table.Footer);
        Assert.Equal("Total", table.Footer[0].DisplayValue);
    }

    [Fact]
    public void FormatWhen_applies_conditional_formatting()
    {
        var table = new DataTableBuilder().AddColumn(1, c => c.FormatWhen<int>(v => v < 0, b => b.WithFontColor("#FF0000")))
            .AddHeaders("Label", "Value")
            .AddRow(r => r.SetCell(0, "Negative").SetCell(1, -5))
            .AddRow(r => r.SetCell(0, "Positive").SetCell(1, 10))
            .Build();

        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("#FF0000", table.Rows[0].Cells[1].FontColor);
        Assert.Null(table.Rows[1].Cells[1].FontColor);
    }
}