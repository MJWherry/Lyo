using Lyo.DataTable.Models;

namespace Lyo.Xlsx.Models;

/// <summary>Maps an <see cref="XlsxCellValue" /> to a Lyo <see cref="IDataTableCell" /> for HTML/table pipelines.</summary>
public static class XlsxCellValueExtensions
{
    /// <summary>Builds a string-based data table cell, copying formatting when present.</summary>
    public static IDataTableCell ToDataTableCell(this XlsxCellValue cell)
        => new DataTableCell<string>(
            cell.Value, cell.FontSize, cell.FontName, cell.FontBold, cell.FontItalic, cell.FontUnderline, cell.FontStrikethrough, cell.FontColor, cell.BackgroundColor,
            cell.HorizontalAlignment, cell.VerticalAlignment, cell.NumberFormat, cell.TextRotation, cell.WrapText, cell.BorderTop, cell.BorderBottom, cell.BorderLeft,
            cell.BorderRight, cell.BorderColor);
}