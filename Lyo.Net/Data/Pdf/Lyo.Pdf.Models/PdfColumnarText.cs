namespace Lyo.Pdf.Models;

/// <summary>Plain text partitioned into vertical columns (newspaper or form layouts).</summary>
/// <param name="Columns">One string per column, left to right. Lines inside a column are separated by <c>\n</c>.</param>
public sealed record PdfColumnarText(IReadOnlyList<string> Columns)
{
    /// <summary>Concatenates columns, inserting a blank line between each.</summary>
    public string ToCombinedString(string columnSeparator = "\n\n") => string.Join(columnSeparator, Columns);
}