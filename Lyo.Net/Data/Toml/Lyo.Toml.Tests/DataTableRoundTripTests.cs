using Table = Lyo.DataTable.Models.DataTable;

namespace Lyo.Toml.Tests;

public class DataTableRoundTripTests
{
    [Fact]
    public void ExportToTomlStringFromDataTable_RoundTripsHeadersAndCells()
    {
        var parsed = RoundTrip(Grid());
        Assert.Equal("X", parsed.Headers[0].DisplayValue);
        Assert.Equal("Y", parsed.Headers[1].DisplayValue);
        Assert.Equal("1", parsed.Rows[0][0].DisplayValue);
        Assert.Equal("4", parsed.Rows[1][1].DisplayValue);
    }

    [Fact]
    public void ExportToTomlStringFromDataTable_Footer_RoundTripsWhenHasFooterRow()
    {
        var parsed = new TomlService().ParseStringAsDataTable(new TomlService().ExportToTomlStringFromDataTable(WithFooter()), true).ValueOrThrow();
        Assert.Single(parsed.Rows);
        Assert.Equal("1", parsed.Rows[0][0].DisplayValue);
        Assert.Equal("Total", parsed.Footer[0].DisplayValue);
        Assert.Equal("2", parsed.Footer[1].DisplayValue);
    }

    [Fact]
    public void ExportToTomlStringFromDataTable_LeadingZero_StaysText()
    {
        var table = new Table();
        table.SetHeader(0, "X");
        table.AddRow().SetCell(0, "01");
        Assert.Equal("01", RoundTrip(table).Rows[0][0].DisplayValue);
    }

    [Fact]
    public void ExportToTomlStringFromDataTable_CustomItemsKey_WritesThatArray()
    {
        var service = new TomlService(o => o.ItemsKey = "people");
        var toml = service.ExportToTomlStringFromDataTable(Grid());
        Assert.Contains("[[people]]", toml, StringComparison.Ordinal);
        Assert.Equal("1", service.ParseStringAsDataTable(toml).ValueOrThrow().Rows[0][0].DisplayValue);
    }

    private static Table RoundTrip(Table table)
    {
        var toml = new TomlService().ExportToTomlStringFromDataTable(table);
        return new TomlService().ParseStringAsDataTable(toml).ValueOrThrow();
    }

    private static Table Grid()
    {
        var table = new Table();
        table.SetHeader(0, "X").SetHeader(1, "Y");
        table.AddRow().SetCell(0, "1").SetCell(1, "2");
        table.AddRow().SetCell(0, "3").SetCell(1, "4");
        return table;
    }

    private static Table WithFooter()
    {
        var table = new Table();
        table.SetHeader(0, "X").SetHeader(1, "Y");
        table.AddRow().SetCell(0, "1").SetCell(1, "2");
        table.SetFooter(0, "Total").SetFooter(1, "2");
        return table;
    }
}
