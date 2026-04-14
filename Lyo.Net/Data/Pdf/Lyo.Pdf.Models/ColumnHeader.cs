namespace Lyo.Pdf.Models;

/// <summary>Header descriptor for table extraction.</summary>
/// <param name="Label">Exact text of the column header as it appears in the PDF.</param>
/// <param name="IsKey">
/// When true this column anchors a new row. If no key columns are present on a parsed line, that line is treated as a continuation and appended to the previous
/// row values.
/// </param>
public record ColumnHeader(string Label, bool IsKey = false);