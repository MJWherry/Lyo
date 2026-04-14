namespace Lyo.DataTable.Models;

/// <summary>Fluent builder for DataTableCell.</summary>
public sealed class DataTableCellBuilder
{
    private string? _backgroundColor;
    private string? _borderBottom;
    private string? _borderColor;
    private string? _borderLeft;
    private string? _borderRight;
    private string? _borderTop;
    private bool? _fontBold;
    private string? _fontColor;
    private bool? _fontItalic;
    private string? _fontName;
    private double? _fontSize;
    private bool? _fontStrikethrough;
    private bool? _fontUnderline;
    private string? _horizontalAlignment;
    private string? _numberFormat;
    private int? _textRotation;
    private object? _value;
    private string? _verticalAlignment;
    private bool? _wrapText;

    /// <summary>Creates a cell builder with the given value.</summary>
    public DataTableCellBuilder(object? value) => _value = value;

    /// <summary>Sets the value.</summary>
    public DataTableCellBuilder WithValue(object? value)
    {
        _value = value;
        return this;
    }

    /// <summary>Sets font size in points.</summary>
    public DataTableCellBuilder WithFontSize(double size)
    {
        _fontSize = size;
        return this;
    }

    /// <summary>Sets font family name.</summary>
    public DataTableCellBuilder WithFontName(string? name)
    {
        _fontName = name;
        return this;
    }

    /// <summary>Sets bold.</summary>
    public DataTableCellBuilder WithBold(bool bold = true)
    {
        _fontBold = bold;
        return this;
    }

    /// <summary>Sets italic.</summary>
    public DataTableCellBuilder WithItalic(bool italic = true)
    {
        _fontItalic = italic;
        return this;
    }

    /// <summary>Sets underline.</summary>
    public DataTableCellBuilder WithUnderline(bool underline = true)
    {
        _fontUnderline = underline;
        return this;
    }

    /// <summary>Sets strikethrough.</summary>
    public DataTableCellBuilder WithStrikethrough(bool strikethrough = true)
    {
        _fontStrikethrough = strikethrough;
        return this;
    }

    /// <summary>Sets font color (e.g. #FF0000).</summary>
    public DataTableCellBuilder WithFontColor(string? color)
    {
        _fontColor = color;
        return this;
    }

    /// <summary>Sets background/fill color (e.g. #EEEEEE).</summary>
    public DataTableCellBuilder WithBackgroundColor(string? color)
    {
        _backgroundColor = color;
        return this;
    }

    /// <summary>Sets horizontal alignment (Left, Center, Right, etc.).</summary>
    public DataTableCellBuilder WithHorizontalAlignment(string? alignment)
    {
        _horizontalAlignment = alignment;
        return this;
    }

    /// <summary>Sets vertical alignment (Top, Center, Bottom, etc.).</summary>
    public DataTableCellBuilder WithVerticalAlignment(string? alignment)
    {
        _verticalAlignment = alignment;
        return this;
    }

    /// <summary>Sets number format (e.g. 0.00, m/d/yy).</summary>
    public DataTableCellBuilder WithNumberFormat(string? format)
    {
        _numberFormat = format;
        return this;
    }

    /// <summary>Sets text rotation angle (-90 to 90, or 255 for vertical).</summary>
    public DataTableCellBuilder WithTextRotation(int? rotation)
    {
        _textRotation = rotation;
        return this;
    }

    /// <summary>Sets text wrap.</summary>
    public DataTableCellBuilder WithWrapText(bool wrap = true)
    {
        _wrapText = wrap;
        return this;
    }

    /// <summary>Sets top border style.</summary>
    public DataTableCellBuilder WithBorderTop(string? style)
    {
        _borderTop = style;
        return this;
    }

    /// <summary>Sets bottom border style.</summary>
    public DataTableCellBuilder WithBorderBottom(string? style)
    {
        _borderBottom = style;
        return this;
    }

    /// <summary>Sets left border style.</summary>
    public DataTableCellBuilder WithBorderLeft(string? style)
    {
        _borderLeft = style;
        return this;
    }

    /// <summary>Sets right border style.</summary>
    public DataTableCellBuilder WithBorderRight(string? style)
    {
        _borderRight = style;
        return this;
    }

    /// <summary>Sets all border styles.</summary>
    public DataTableCellBuilder WithBorders(string? style)
    {
        _borderTop = _borderBottom = _borderLeft = _borderRight = style;
        return this;
    }

    /// <summary>Sets border color (e.g. #000000).</summary>
    public DataTableCellBuilder WithBorderColor(string? color)
    {
        _borderColor = color;
        return this;
    }

    /// <summary>Builds the DataTableCell.</summary>
    public DataTableCell<T> Build<T>()
        => new(
            (T?)(object?)_value, _fontSize, _fontName, _fontBold, _fontItalic, _fontUnderline, _fontStrikethrough, _fontColor, _backgroundColor, _horizontalAlignment,
            _verticalAlignment, _numberFormat, _textRotation, _wrapText, _borderTop, _borderBottom, _borderLeft, _borderRight, _borderColor);

    /// <summary>Builds the DataTableCell as IDataTableCell. For value types this boxes to DataTableCell&lt;object?&gt;.</summary>
    public IDataTableCell Build() => Build<object?>();
}