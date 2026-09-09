namespace Lyo.Pdf.Models;

/// <summary>Column heading used when extracting a table.</summary>
/// <param name="Label">Header text exactly as it appears in the PDF.</param>
/// <param name="IsKey">
/// When true, this column starts a new row. A parsed line with no key columns is treated as a continuation and appended to the previous row.
/// </param>
public record ColumnHeader(string Label, bool IsKey = false);