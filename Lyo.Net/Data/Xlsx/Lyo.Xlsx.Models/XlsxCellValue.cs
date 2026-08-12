namespace Lyo.Xlsx.Models;

/// <summary>
/// A cell value with optional merge spans extracted from XLSX. Formatting is applied to <see cref="DataTable.Models.DataTable" /> via
/// <see cref="DataTable.Models.DataTableCellFormat" />, not stored on this type.
/// </summary>
/// <param name="Value">The textual/display value of the cell.</param>
/// <param name="ColSpan">Number of columns this cell spans (1 = no spanning; anchor cell of a merged range).</param>
/// <param name="RowSpan">Number of rows this cell spans (1 = no spanning; anchor cell of a merged range).</param>
public sealed record XlsxCellValue(string Value, int ColSpan = 1, int RowSpan = 1)
{
    /// <summary>Creates a cell with only a value (no spans).</summary>
    public static XlsxCellValue FromValue(string value) => new(value);
}