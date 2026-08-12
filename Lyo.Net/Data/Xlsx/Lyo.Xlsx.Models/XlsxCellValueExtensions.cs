using Lyo.DataTable.Models;

namespace Lyo.Xlsx.Models;

/// <summary>Maps an <see cref="XlsxCellValue" /> to a Lyo <see cref="IDataTableCell" /> for table pipelines.</summary>
public static class XlsxCellValueExtensions
{
    /// <summary>Builds a string-based data table cell (value + spans only).</summary>
    public static IDataTableCell ToDataTableCell(this XlsxCellValue cell) => DataTableCell.FromValue(cell.Value, cell.ColSpan, cell.RowSpan);
}