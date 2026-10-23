using Table = Lyo.DataTable.Models.DataTable;

namespace Lyo.Yaml.Tests;

public class DataTableRoundTripTests
{
    [Fact]
    public void ExportToYamlStringFromDataTable_RoundTripsHeadersAndCells()
    {
        var parsed = RoundTrip(Grid());
        Assert.Equal("X", parsed.Headers[0].DisplayValue);
        Assert.Equal("Y", parsed.Headers[1].DisplayValue);
        Assert.Equal("1", parsed.Rows[0][0].DisplayValue);
        Assert.Equal("4", parsed.Rows[1][1].DisplayValue);
    }

    [Fact]
    public void ExportToYamlStringFromDataTable_Footer_RoundTripsWhenHasFooterRow()
    {
        var parsed = new YamlService().ParseStringAsDataTable(new YamlService().ExportToYamlStringFromDataTable(WithFooter()), true).ValueOrThrow();
        Assert.Single(parsed.Rows);
        Assert.Equal("1", parsed.Rows[0][0].DisplayValue);
        Assert.Equal("Total", parsed.Footer[0].DisplayValue);
        Assert.Equal("2", parsed.Footer[1].DisplayValue);
    }

    [Fact]
    public void ExportToYamlStringFromDataTable_LeadingZero_StaysText()
    {
        var table = new Table();
        table.SetHeader(0, "X");
        table.AddRow().SetCell(0, "01");
        Assert.Equal("01", RoundTrip(table).Rows[0][0].DisplayValue);
    }

    private static Table RoundTrip(Table table)
    {
        var yaml = new YamlService().ExportToYamlStringFromDataTable(table);
        return new YamlService().ParseStringAsDataTable(yaml).ValueOrThrow();
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