using Table = Lyo.DataTable.Models.DataTable;

namespace Lyo.Xml.Tests;

public class DataTableRoundTripTests
{
    [Fact]
    public void ExportToXmlStringFromDataTable_RoundTripsHeadersAndCells()
    {
        var parsed = RoundTrip(Grid());
        Assert.Equal("X", parsed.Headers[0].DisplayValue);
        Assert.Equal("Y", parsed.Headers[1].DisplayValue);
        Assert.Equal("1", parsed.Rows[0][0].DisplayValue);
        Assert.Equal("4", parsed.Rows[1][1].DisplayValue);
    }

    [Fact]
    public void ExportToXmlStringFromDataTable_Footer_RoundTripsWhenHasFooterRow()
    {
        var parsed = new XmlService().ParseStringAsDataTable(new XmlService().ExportToXmlStringFromDataTable(WithFooter()), true).ValueOrThrow();
        Assert.Single(parsed.Rows);
        Assert.Equal("1", parsed.Rows[0][0].DisplayValue);
        Assert.Equal("Total", parsed.Footer[0].DisplayValue);
        Assert.Equal("2", parsed.Footer[1].DisplayValue);
    }

    [Fact]
    public void ExportToXmlStringFromDataTable_LeadingZero_StaysText()
    {
        var table = new Table();
        table.SetHeader(0, "X");
        table.AddRow().SetCell(0, "01");
        Assert.Equal("01", RoundTrip(table).Rows[0][0].DisplayValue);
    }

    [Fact]
    public void ExportToXmlStringFromDataTable_SingleRow_StaysOneRow()
    {
        var table = new Table();
        table.SetHeader(0, "X");
        table.AddRow().SetCell(0, "1");
        Assert.Single(RoundTrip(table).Rows);
    }

    [Fact]
    public void ExportToXmlStringFromDataTable_EncodedHeader_RoundTrips()
    {
        var table = new Table();
        table.SetHeader(0, "First Name");
        table.AddRow().SetCell(0, "Ada");
        var xml = new XmlService().ExportToXmlStringFromDataTable(table);
        Assert.Contains("First_x0020_Name", xml, StringComparison.Ordinal);
        var parsed = new XmlService().ParseStringAsDataTable(xml).ValueOrThrow();
        Assert.Equal("First Name", parsed.Headers[0].DisplayValue);
        Assert.Equal("Ada", parsed.Rows[0][0].DisplayValue);
    }

    private static Table RoundTrip(Table table)
    {
        var xml = new XmlService().ExportToXmlStringFromDataTable(table);
        return new XmlService().ParseStringAsDataTable(xml).ValueOrThrow();
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
