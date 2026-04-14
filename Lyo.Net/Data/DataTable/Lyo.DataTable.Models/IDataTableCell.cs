namespace Lyo.DataTable.Models;

/// <summary>Common interface for data table cells of any type.</summary>
public interface IDataTableCell
{
    /// <summary>The display string for the cell.</summary>
    string DisplayValue { get; }

    double? FontSize { get; }

    string? FontName { get; }

    bool? FontBold { get; }

    bool? FontItalic { get; }

    bool? FontUnderline { get; }

    bool? FontStrikethrough { get; }

    string? FontColor { get; }

    string? BackgroundColor { get; }

    string? HorizontalAlignment { get; }

    string? VerticalAlignment { get; }

    string? NumberFormat { get; }

    int? TextRotation { get; }

    bool? WrapText { get; }

    string? BorderTop { get; }

    string? BorderBottom { get; }

    string? BorderLeft { get; }

    string? BorderRight { get; }

    string? BorderColor { get; }
}