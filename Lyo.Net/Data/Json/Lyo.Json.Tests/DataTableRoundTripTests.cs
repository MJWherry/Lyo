using Table = Lyo.DataTable.Models.DataTable;

namespace Lyo.Json.Tests;

public class DataTableRoundTripTests
{
    [Fact]
    public void ExportToJsonStringFromDataTable_RoundTripsHeadersAndCells()
    {
        var parsed = RoundTrip(Grid());
        Assert.Equal("X", parsed.Headers[0].DisplayValue);
        Assert.Equal("Y", parsed.Headers[1].DisplayValue);
        Assert.Equal("1", parsed.Rows[0][0].DisplayValue);
        Assert.Equal("2", parsed.Rows[0][1].DisplayValue);
        Assert.Equal("3", parsed.Rows[1][0].DisplayValue);
        Assert.Equal("4", parsed.Rows[1][1].DisplayValue);
    }

    [Fact]
    public void ExportToJsonStringFromDataTable_Footer_RoundTripsWhenHasFooterRow()
    {
        var parsed = new JsonService().ParseStringAsDataTable(new JsonService().ExportToJsonStringFromDataTable(WithFooter()), true).ValueOrThrow();
        Assert.Single(parsed.Rows);
        Assert.Equal("1", parsed.Rows[0][0].DisplayValue);
        Assert.Equal("Total", parsed.Footer[0].DisplayValue);
        Assert.Equal("2", parsed.Footer[1].DisplayValue);
    }

    [Fact]
    public void ParseStringAsDataTable_Default_KeepsLastRowAsBody()
    {
        var parsed = new JsonService().ParseStringAsDataTable(new JsonService().ExportToJsonStringFromDataTable(WithFooter())).ValueOrThrow();
        Assert.Equal(2, parsed.Rows.Count);
        Assert.Equal("Total", parsed.Rows[1][0].DisplayValue);
        Assert.Empty(parsed.Footer);
    }

    [Fact]
    public void ExportToJsonStringFromDataTable_BlankCell_StaysEmptyString()
    {
        var table = new Table();
        table.SetHeader(0, "X");
        table.AddRow().SetCell(0, "");
        var parsed = new JsonService(o => o.IgnoreEmptyStrings = true).ParseStringAsDataTable(
                new JsonService(o => o.IgnoreEmptyStrings = true).ExportToJsonStringFromDataTable(table))
            .ValueOrThrow();

        Assert.Equal("", parsed.Rows[0][0].DisplayValue);
    }

    [Fact]
    public void ExportToJsonStringFromDataTable_LeadingZero_StaysText()
    {
        var table = new Table();
        table.SetHeader(0, "X");
        table.AddRow().SetCell(0, "01");
        var parsed = RoundTrip(table);
        Assert.Equal("01", parsed.Rows[0][0].DisplayValue);
    }

    [Fact]
    public void ExportToJsonStringFromDataTable_BlankHeader_UsesColumnIndex()
    {
        var table = new Table();
        table.SetHeader(0, "");
        table.AddRow().SetCell(0, "a");
        var parsed = RoundTrip(table);
        Assert.Equal("Column0", parsed.Headers[0].DisplayValue);
        Assert.Equal("a", parsed.Rows[0][0].DisplayValue);
    }

    [Fact]
    public void ExportToJsonStringFromDataTable_DuplicateHeader_Throws()
    {
        var table = new Table();
        table.SetHeader(0, "X").SetHeader(1, "X");
        Assert.Throws<ArgumentException>(() => new JsonService().ExportToJsonStringFromDataTable(table));
    }

    [Fact]
    public void ParseStringAsDataTable_NestedValue_Throws()
    {
        var result = new JsonService().ParseStringAsDataTable("""[{"X":{"a":1}}]""");
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ParseStringAsDataTable_ObjectRoot_Throws()
    {
        var result = new JsonService().ParseStringAsDataTable("""{"X":"1"}""");
        Assert.False(result.IsSuccess);
    }

    private static Table RoundTrip(Table table)
    {
        var json = new JsonService().ExportToJsonStringFromDataTable(table);
        return new JsonService().ParseStringAsDataTable(json).ValueOrThrow();
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