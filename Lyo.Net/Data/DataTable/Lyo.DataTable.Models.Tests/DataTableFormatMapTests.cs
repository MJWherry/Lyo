namespace Lyo.DataTable.Models.Tests;

public sealed class DataTableFormatMapTests
{
    [Fact]
    public void SetFormat_null_removes_key()
    {
        var table = new DataTable();
        table.SetCell(0, 0, "x", new DataTableCellFormat(FontBold: true));
        Assert.True(table.HasFormats);
        Assert.True(table.GetFormat(0, 0)?.FontBold == true);
        table.SetFormat(0, 0, null);
        Assert.Null(table.GetFormat(0, 0));
        Assert.False(table.HasFormats);
    }

    [Fact]
    public void ClearCell_removes_value_and_format()
    {
        var table = new DataTable();
        table.SetCell(0, 1, "y", new DataTableCellFormat(FontColor: "#FF0000"));
        table.ClearCell(0, 1);
        Assert.Equal("", table[0, 1].DisplayValue);
        Assert.Null(table.GetFormat(0, 1));
    }

    [Fact]
    public void SetCell_valueOnly_leaves_existing_format()
    {
        var table = new DataTable();
        table.SetCell(0, 0, "a", new DataTableCellFormat(FontBold: true));
        table.SetCell(0, 0, "b");
        Assert.Equal("b", table[0, 0].DisplayValue);
        Assert.True(table.GetFormat(0, 0)?.FontBold == true);
    }

    [Fact]
    public void Formats_returns_snapshot()
    {
        var table = new DataTable();
        table.SetFormat(-1, 0, new DataTableCellFormat(FontBold: true));
        var snapshot = table.Formats;
        table.SetFormat(-1, 1, new DataTableCellFormat(FontItalic: true));
        Assert.Single(snapshot);
        Assert.Equal(2, table.Formats.Count);
    }

    [Fact]
    public void ValueInterner_shares_equal_strings_above_threshold()
    {
        var options = new DataTablePoolingOptions { PoolValues = true, PoolFormats = false, PoolingCellThreshold = 0 };
        var interner = new DataTableValueInterner(options, estimatedCellCount: 1);
        var a = interner.Intern(new string('x', 3));
        var b = interner.Intern(new string('x', 3));
        Assert.Same(a, b);
    }

    [Fact]
    public void ValueInterner_disabled_below_threshold()
    {
        var options = new DataTablePoolingOptions { PoolValues = true, PoolingCellThreshold = 1000 };
        var interner = new DataTableValueInterner(options, estimatedCellCount: 10);
        Assert.False(interner.PoolsValues);
        Assert.False(interner.PoolsFormats);
    }

    [Fact]
    public void Concurrent_SetFormat_GetFormat_smoke()
    {
        var table = new DataTable();
        Parallel.For(0, 200, i => {
            var col = i % 20;
            table.SetFormat(0, col, new DataTableCellFormat(FontBold: true));
            _ = table.GetFormat(0, col);
            if (i % 3 == 0)
                table.ClearFormat(0, col);
        });

        Assert.True(table.Formats.Count <= 20);
    }

    [Fact]
    public void ValueInterner_shares_equal_formats_when_enabled()
    {
        var options = new DataTablePoolingOptions { PoolValues = false, PoolFormats = true, PoolingCellThreshold = 0 };
        var interner = new DataTableValueInterner(options, estimatedCellCount: 1);
        var a = interner.Intern(new DataTableCellFormat(FontBold: true));
        var b = interner.Intern(new DataTableCellFormat(FontBold: true));
        Assert.Same(a, b);
    }
}
