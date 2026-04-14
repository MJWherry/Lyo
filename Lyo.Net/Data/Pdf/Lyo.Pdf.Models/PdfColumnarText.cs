namespace Lyo.Pdf.Models;

/// <summary>Plain text split into vertical columns (e.g. newspaper or form layouts).</summary>
/// <param name="Columns">One entry per column, left to right. Lines within a column use newline (<c>\n</c>) separators.</param>
public sealed record PdfColumnarText(IReadOnlyList<string> Columns)
{
    /// <summary>Joins all columns with a blank line between them.</summary>
    public string ToCombinedString(string columnSeparator = "\n\n") => string.Join(columnSeparator, Columns);
}