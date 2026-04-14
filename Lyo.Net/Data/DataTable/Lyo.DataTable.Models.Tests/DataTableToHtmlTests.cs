namespace Lyo.DataTable.Models.Tests;

public sealed class DataTableToHtmlTests
{
    [Fact]
    public void ToHtmlDocument_renders_table_with_headers_and_rows()
    {
        var table = new DataTableBuilder().AddHeaders("A", "B").AddRow(r => r.AddCells("1", "2")).Build();
        var html = DataTableToHtml.ToHtmlDocument(table);
        Assert.Contains("<!DOCTYPE html>", html);
        Assert.Contains("<th>", html);
        Assert.Contains("A", html);
        Assert.Contains("B", html);
        Assert.Contains("<td>", html);
        Assert.Contains("1", html);
        Assert.Contains("2", html);
    }

    [Fact]
    public void ToHtmlDocument_empty_table_returns_no_data_message()
    {
        var table = new DataTableBuilder().Build();
        var html = DataTableToHtml.ToHtmlDocument(table);
        Assert.Contains("No data", html);
    }

    [Fact]
    public void ToHtmlDocument_with_footer_includes_tfoot()
    {
        var table = new DataTableBuilder().AddHeaders("H").AddFooters("Total").Build();
        var html = DataTableToHtml.ToHtmlDocument(table);
        Assert.Contains("<tfoot>", html);
        Assert.Contains("Total", html);
    }

    [Fact]
    public void ToHtmlDocument_throws_on_null() => Assert.Throws<ArgumentNullException>(() => DataTableToHtml.ToHtmlDocument(null!));
}