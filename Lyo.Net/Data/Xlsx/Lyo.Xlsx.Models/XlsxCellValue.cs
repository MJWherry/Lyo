namespace Lyo.Xlsx.Models;

/// <summary>A cell value with optional formatting extracted from XLSX.</summary>
/// <param name="Value">The textual/display value of the cell.</param>
/// <param name="FontSize">Font size in points, if set.</param>
/// <param name="FontName">Font family name, if set.</param>
/// <param name="FontBold">Whether font is bold, if set.</param>
/// <param name="FontItalic">Whether font is italic, if set.</param>
/// <param name="FontUnderline">Whether font is underlined, if set.</param>
/// <param name="FontStrikethrough">Whether font has strikethrough, if set.</param>
/// <param name="FontColor">Font color as hex (e.g. #FF0000), if set.</param>
/// <param name="BackgroundColor">Background/fill color as hex, if set.</param>
/// <param name="HorizontalAlignment">Horizontal alignment (Left, Center, Right, etc.), if set.</param>
/// <param name="VerticalAlignment">Vertical alignment (Top, Center, Bottom, etc.), if set.</param>
/// <param name="NumberFormat">Number format code (e.g. 0.00, m/d/yy), if set.</param>
/// <param name="TextRotation">Text rotation angle (-90 to 90, or 255 for vertical), if set.</param>
/// <param name="WrapText">Whether text wraps, if set.</param>
/// <param name="BorderTop">Top border style, if set.</param>
/// <param name="BorderBottom">Bottom border style, if set.</param>
/// <param name="BorderLeft">Left border style, if set.</param>
/// <param name="BorderRight">Right border style, if set.</param>
/// <param name="BorderColor">Border color as hex (when borders are set), if set.</param>
public sealed record XlsxCellValue(
    string Value,
    double? FontSize = null,
    string? FontName = null,
    bool? FontBold = null,
    bool? FontItalic = null,
    bool? FontUnderline = null,
    bool? FontStrikethrough = null,
    string? FontColor = null,
    string? BackgroundColor = null,
    string? HorizontalAlignment = null,
    string? VerticalAlignment = null,
    string? NumberFormat = null,
    int? TextRotation = null,
    bool? WrapText = null,
    string? BorderTop = null,
    string? BorderBottom = null,
    string? BorderLeft = null,
    string? BorderRight = null,
    string? BorderColor = null)
{
    /// <summary>Creates a cell with only a value (no formatting).</summary>
    public static XlsxCellValue FromValue(string value) => new(value);
}