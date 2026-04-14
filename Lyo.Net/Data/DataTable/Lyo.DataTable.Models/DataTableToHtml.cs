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
        ArgumentHelpers.ThrowIfNull(data, nameof(data));
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
            for (var col = 0; col <= maxCol; col++) {
                var cell = headers.TryGetValue(col, out var h) ? h : DataTableCell.Empty;
                sb.Append($"<th{GetCellStyleAttr(cell)}>{WebUtility.HtmlEncode(cell.DisplayValue)}</th>");
            }

            sb.Append("</tr></thead>");
        }

        if (rows.Count > 0) {
            sb.Append("<tbody>");
            foreach (var row in rows) {
                sb.Append("<tr>");
                for (var col = 0; col <= maxCol; col++) {
                    var cell = row.Cells.TryGetValue(col, out var v) ? v : DataTableCell.Empty;
                    sb.Append($"<td{GetCellStyleAttr(cell)}>{WebUtility.HtmlEncode(cell.DisplayValue)}</td>");
                }

                sb.Append("</tr>");
            }

            sb.Append("</tbody>");
        }

        if (footer.Count > 0) {
            sb.Append("<tfoot><tr>");
            for (var col = 0; col <= maxCol; col++) {
                var cell = footer.TryGetValue(col, out var f) ? f : DataTableCell.Empty;
                sb.Append($"<td{GetCellStyleAttr(cell)}>{WebUtility.HtmlEncode(cell.DisplayValue)}</td>");
            }

            sb.Append("</tr></tfoot>");
        }

        sb.Append("</table></body></html>");
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