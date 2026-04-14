using System.Diagnostics;

namespace Lyo.DataTable.Models;

/// <summary>Cell value with optional formatting for data tables.</summary>
/// <param name="Value">The typed value of the cell.</param>
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
[DebuggerDisplay("{ToString(),nq}")]
public sealed record DataTableCell<T>(
    T? Value,
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
    string? BorderColor = null) : IDataTableCell
{
    /// <summary>Empty cell placeholder. Immutable; safe to reuse.</summary>
    public static DataTableCell<string> Empty { get; } = new("");

    /// <summary>The display string for the cell (Value?.ToString() ?? "").</summary>
    public string DisplayValue => Value?.ToString() ?? "";

    /// <summary>Creates a cell with only a value (no formatting).</summary>
    public static DataTableCell<T> FromValue(T? value) => new(value);

    public override string ToString() => $"({typeof(T).Name}) {DisplayValue})";
}

/// <summary>Static helpers for DataTableCell.</summary>
public static class DataTableCell
{
    /// <summary>Empty cell placeholder.</summary>
    public static IDataTableCell Empty => DataTableCell<string>.Empty;

    /// <summary>Creates a string cell with no formatting.</summary>
    public static IDataTableCell FromValue(string value) => DataTableCell<string>.FromValue(value ?? "");
}