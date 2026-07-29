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
        return BuildHtml(data);
    }

    internal static string BuildHtml(DataTable data)
    {
        var headers = data.Headers;
        var rows = data.Rows;
        var footer = data.Footer;
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
            AppendSingleRow(sb, data, headers, maxCol, "th", -1);
            sb.Append("</tr></thead>");
        }

        if (rows.Count > 0) {
            sb.Append("<tbody>");
            // pendingRows[col] > 0 means the column is covered by a rowspan from a previous row.
            var pendingRows = new int[maxCol + 1];
            for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++) {
                var row = rows[rowIndex];
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
                    sb.Append($"<td{GetSpanAttr(colSpan, rowSpan)}{GetCellStyleAttr(data.GetFormat(rowIndex, col))}>{WebUtility.HtmlEncode(cell.DisplayValue)}</td>");
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
            AppendSingleRow(sb, data, footer, maxCol, "td", -2);
            sb.Append("</tr></tfoot>");
        }

        sb.Append("</table></body></html>");
        return sb.ToString();
    }

    /// <summary>Renders a single header/footer row honoring ColSpan (RowSpan is ignored: the section is one row tall).</summary>
    private static void AppendSingleRow(
        StringBuilder sb, DataTable data, IReadOnlyDictionary<int, IDataTableCell> cells, int maxCol, string tag, int formatRow)
    {
        var col = 0;
        while (col <= maxCol) {
            var cell = cells.TryGetValue(col, out var c) ? c : DataTableCell.Empty;
            var colSpan = ClampSpan(cell.ColSpan, maxCol - col + 1);
            sb.Append($"<{tag}{GetSpanAttr(colSpan, 1)}{GetCellStyleAttr(data.GetFormat(formatRow, col))}>{WebUtility.HtmlEncode(cell.DisplayValue)}</{tag}>");
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

    private static string GetCellStyleAttr(DataTableCellFormat? format)
    {
        if (format == null)
            return "";

        var parts = new List<string>();
        if (format.FontSize.HasValue)
            parts.Add($"font-size:{format.FontSize}pt");

        if (!string.IsNullOrEmpty(format.FontName))
            parts.Add($"font-family:{format.FontName}");

        if (format.FontBold == true)
            parts.Add("font-weight:bold");

        if (format.FontItalic == true)
            parts.Add("font-style:italic");

        if (format.FontUnderline == true || format.FontStrikethrough == true) {
            var deco = new List<string>();
            if (format.FontUnderline == true)
                deco.Add("underline");

            if (format.FontStrikethrough == true)
                deco.Add("line-through");

            parts.Add($"text-decoration:{string.Join(" ", deco)}");
        }

        if (!string.IsNullOrEmpty(format.FontColor))
            parts.Add($"color:{format.FontColor}");

        if (!string.IsNullOrEmpty(format.BackgroundColor))
            parts.Add($"background-color:{format.BackgroundColor}");

        if (!string.IsNullOrEmpty(format.HorizontalAlignment))
            parts.Add($"text-align:{format.HorizontalAlignment!.ToLowerInvariant()}");

        if (format.WrapText == true)
            parts.Add("white-space:normal");

        if (parts.Count == 0)
            return "";

        return " style=\"" + string.Join(";", parts) + "\"";
    }

    private static string WrapInHtmlDocument(string body) => $"<!DOCTYPE html><html><head><meta charset=\"utf-8\"></head><body>{body}</body></html>";
}
