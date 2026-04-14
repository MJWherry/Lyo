using Lyo.DataTable.Models;

namespace Lyo.Xlsx.Models;

/// <summary>Converts XlsxCellValue to DataTableCell.</summary>
public static class XlsxCellValueExtensions
{
    public static IDataTableCell ToDataTableCell(this XlsxCellValue cell)
        => new DataTableCell<string>(
            cell.Value, cell.FontSize, cell.FontName, cell.FontBold, cell.FontItalic, cell.FontUnderline, cell.FontStrikethrough, cell.FontColor, cell.BackgroundColor,
            cell.HorizontalAlignment, cell.VerticalAlignment, cell.NumberFormat, cell.TextRotation, cell.WrapText, cell.BorderTop, cell.BorderBottom, cell.BorderLeft,
            cell.BorderRight, cell.BorderColor);
}