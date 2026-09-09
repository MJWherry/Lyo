using System.Diagnostics;
using Lyo.Reporting.Models.Controls;

namespace Lyo.Reporting.Models.Builders;

/// <summary>Fluent builder for table column definitions.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class TableColumnBuilder
{
    private readonly TableColumn _column = new();

    /// <summary>Sets the column's header text.</summary>
    public TableColumnBuilder SetHeader(string header)
    {
        _column.Header = header;
        return this;
    }

    /// <summary>Sets the column data field or property name.</summary>
    public TableColumnBuilder SetField(string field)
    {
        _column.Field = field;
        return this;
    }

    /// <summary>Sets the column width as a CSS value.</summary>
    public TableColumnBuilder SetWidth(string width)
    {
        _column.Width = width;
        return this;
    }

    /// <summary>Sets column alignment (left, right, center, or justify).</summary>
    public TableColumnBuilder SetAlignment(string alignment)
    {
        _column.Alignment = alignment;
        return this;
    }

    /// <summary>Sets a formatter for cell values.</summary>
    public TableColumnBuilder SetFormatter(Func<object?, string> formatter)
    {
        _column.ValueFormatter = formatter;
        return this;
    }

    /// <summary>Adds a CSS style on the column.</summary>
    public TableColumnBuilder AddStyle(string property, string value)
    {
        _column.Styles[property] = value;
        return this;
    }

    /// <summary>Builds and returns the finished table column.</summary>
    public TableColumn Build() => _column;

    public override string ToString() => $"TableColumnBuilder: {_column.Header} ({_column.Field ?? "no field"})";
}
