using System.Net;
using System.Text;
using Lyo.Exceptions;

namespace Lyo.DataTable.Models;

/// <summary>Builds an HTML document with a table from a DataTable.</summary>
public static class DataTableToHtml
{
    /// <summary>Renders a DataTable to a complete HTML document with a table.</summary>
    /// <param name="data">The data table to render.</param>
    /// <returns>Complete HTML document string with table.</returns>
    public static string ToHtmlDocument(DataTable data)
    {
        ArgumentHelpers.ThrowIfNull(data);
        return BuildHtml(data.Headers, data.Rows, data.Footer);
    }

    internal static string BuildHtml(IReadOnlyDictionary<int, IDataTableCell> headers, IReadOnlyList<DataTableRow> rows, IReadOnlyDictionary<int, IDataTableCell> footer)
    {
        if (headers.Count == 0 && rows.Count == 0 && footer.Count == 0)
            return WrapInHtmlDocument("<p>No data</p>");

        var maxCol = Math.Max(
            Math.Max(headers.Count > 0 ? headers.Keys.Max() : -1, rows.Count > 0 ? rows.Select(r => r.Cells.Count > 0 ? r.Cells.Keys.Max() : -1).DefaultIfEmpty(-1).Max() : -1),
            footer.Count > 0 ? footer.Keys.Max() : -1);

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\"><style>");
        sb.Append("table{border-collapse:collapse;font-family:sans-serif}th,td{border:1px solid #ccc;padding:6px 10px;text-align:left}");
        sb.Append("th{background:#eee}tfoot td{background:#f5f5f5;font-weight:bold}</style></head><body><table>");
        if (headers.Count > 0) {
            sb.Append("<thead><tr>");
            AppendSingleRow(sb, headers, maxCol, "th");
            sb.Append("</tr></thead>");
        }

        if (rows.Count > 0) {
            sb.Append("<tbody>");
            // pendingRows[col] > 0 means the column is covered by a rowspan from a previous row.
            var pendingRows = new int[maxCol + 1];
            foreach (var row in rows) {
                sb.Append("<tr>");
                var coveredInRow = new HashSet<int>();
                for (var col = 0; col <= maxCol; col++) {
                    if (pendingRows[col] > 0) {
                        pendingRows[col]--;
                        continue;
                    }

                    if (coveredInRow.Contains(col))
                        continue;

                    var cell = row.Cells.TryGetValue(col, out var v) ? v : DataTableCell.Empty;
                    var colSpan = ClampSpan(cell.ColSpan, maxCol - col + 1);
                    var rowSpan = cell.RowSpan < 1 ? 1 : cell.RowSpan;
                    sb.Append($"<td{GetSpanAttr(colSpan, rowSpan)}{GetCellStyleAttr(cell)}>{WebUtility.HtmlEncode(cell.DisplayValue)}</td>");
                    for (var k = col; k < col + colSpan; k++) {
                        if (k > col)
                            coveredInRow.Add(k);

                        if (rowSpan > 1)
                            pendingRows[k] += rowSpan - 1;
                    }
                }

                sb.Append("</tr>");
            }

            sb.Append("</tbody>");
        }

        if (footer.Count > 0) {
            sb.Append("<tfoot><tr>");
            AppendSingleRow(sb, footer, maxCol, "td");
            sb.Append("</tr></tfoot>");
        }

        sb.Append("</table></body></html>");
        return sb.ToString();
    }

    /// <summary>Renders a single header/footer row honoring ColSpan (RowSpan is ignored: the section is one row tall).</summary>
    private static void AppendSingleRow(StringBuilder sb, IReadOnlyDictionary<int, IDataTableCell> cells, int maxCol, string tag)
    {
        var col = 0;
        while (col <= maxCol) {
            var cell = cells.TryGetValue(col, out var c) ? c : DataTableCell.Empty;
            var colSpan = ClampSpan(cell.ColSpan, maxCol - col + 1);
            sb.Append($"<{tag}{GetSpanAttr(colSpan, 1)}{GetCellStyleAttr(cell)}>{WebUtility.HtmlEncode(cell.DisplayValue)}</{tag}>");
            col += colSpan;
        }
    }

    private static int ClampSpan(int span, int max)
    {
        if (span < 1)
            return 1;

        return span > max ? max : span;
    }

    private static string GetSpanAttr(int colSpan, int rowSpan)
    {
        if (colSpan <= 1 && rowSpan <= 1)
            return "";

        var sb = new StringBuilder();
        if (colSpan > 1)
            sb.Append($" colspan=\"{colSpan}\"");

        if (rowSpan > 1)
            sb.Append($" rowspan=\"{rowSpan}\"");

        return sb.ToString();
    }

    private static string GetCellStyleAttr(IDataTableCell cell)
    {
        var parts = new List<string>();
        if (cell.FontSize.HasValue)
            parts.Add($"font-size:{cell.FontSize}pt");

        if (!string.IsNullOrEmpty(cell.FontName))
            parts.Add($"font-family:{cell.FontName}");

        if (cell.FontBold == true)
            parts.Add("font-weight:bold");

        if (cell.FontItalic == true)
            parts.Add("font-style:italic");

        if (cell.FontUnderline == true || cell.FontStrikethrough == true) {
            var deco = new List<string>();
            if (cell.FontUnderline == true)
                deco.Add("underline");

            if (cell.FontStrikethrough == true)
                deco.Add("line-through");

            parts.Add($"text-decoration:{string.Join(" ", deco)}");
        }

        if (!string.IsNullOrEmpty(cell.FontColor))
            parts.Add($"color:{cell.FontColor}");

        if (!string.IsNullOrEmpty(cell.BackgroundColor))
            parts.Add($"background-color:{cell.BackgroundColor}");

        if (!string.IsNullOrEmpty(cell.HorizontalAlignment))
            parts.Add($"text-align:{cell.HorizontalAlignment!.ToLowerInvariant()}");

        if (cell.WrapText == true)
            parts.Add("white-space:normal");

        if (parts.Count == 0)
            return "";

        return " style=\"" + string.Join(";", parts) + "\"";
    }

    private static string WrapInHtmlDocument(string body) => $"<!DOCTYPE html><html><head><meta charset=\"utf-8\"></head><body>{body}</body></html>";
}