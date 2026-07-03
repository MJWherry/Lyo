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

    [Fact]
    public void ToHtmlDocument_emits_colspan_and_skips_covered_cells()
    {
        var table = new DataTable();
        table.SetHeader(0, "A");
        table.SetHeader(1, "B");
        table.SetHeader(2, "C");
        table.SetCell(0, 0, new DataTableCell<string>("Merged", ColSpan: 2));
        table.SetCell(0, 2, "End");
        var html = DataTableToHtml.ToHtmlDocument(table);
        Assert.Contains("<td colspan=\"2\">Merged</td>", html);
        // Row 0 renders exactly two <td> elements (the covered column is skipped).
        var bodyStart = html.IndexOf("<tbody>", StringComparison.Ordinal);
        var bodyEnd = html.IndexOf("</tbody>", StringComparison.Ordinal);
        var body = html.Substring(bodyStart, bodyEnd - bodyStart);
        Assert.Equal(2, body.Split(["<td"], StringSplitOptions.None).Length - 1);
    }

    [Fact]
    public void ToHtmlDocument_emits_rowspan_and_skips_covered_rows()
    {
        var table = new DataTable();
        table.SetHeader(0, "A");
        table.SetHeader(1, "B");
        table.SetCell(0, 0, new DataTableCell<string>("Tall", RowSpan: 2));
        table.SetCell(0, 1, "r0c1");
        table.SetCell(1, 1, "r1c1");
        var html = DataTableToHtml.ToHtmlDocument(table);
        Assert.Contains("<td rowspan=\"2\">Tall</td>", html);
        // Second row only renders one cell (column 0 is covered from above).
        Assert.Contains("<tr><td>r1c1</td></tr>", html);
    }

    [Fact]
    public void ToHtmlDocument_emits_colspan_in_header()
    {
        var table = new DataTable();
        table.SetHeader(0, new DataTableCell<string>("Wide", ColSpan: 2));
        table.SetHeader(2, "C");
        table.SetCell(0, 0, "a");
        table.SetCell(0, 1, "b");
        table.SetCell(0, 2, "c");
        var html = DataTableToHtml.ToHtmlDocument(table);
        Assert.Contains("<th colspan=\"2\">Wide</th>", html);
        Assert.Contains("<th>C</th>", html);
    }
}