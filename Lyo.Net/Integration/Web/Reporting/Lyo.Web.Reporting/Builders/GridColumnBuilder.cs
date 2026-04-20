using System.Diagnostics;
using Lyo.Web.Reporting.Models;

namespace Lyo.Web.Reporting.Builders;

/// <summary>Fluent builder for constructing grid column definitions.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class GridColumnBuilder
{
    private readonly ReportGridColumn _column = new();

    /// <summary>Sets the column header text.</summary>
    public GridColumnBuilder SetHeader(string header)
    {
        _column.Header = header;
        return this;
    }

    /// <summary>Sets the column data field/property name.</summary>
    public GridColumnBuilder SetField(string field)
    {
        _column.Field = field;
        return this;
    }

    /// <summary>Sets the column width (as CSS value).</summary>
    public GridColumnBuilder SetWidth(string width)
    {
        _column.Width = width;
        return this;
    }

    /// <summary>Sets the column alignment (left, right, center, justify).</summary>
    public GridColumnBuilder SetAlignment(string alignment)
    {
        _column.Alignment = alignment;
        return this;
    }

    /// <summary>Sets a formatter function for cell values.</summary>
    public GridColumnBuilder SetFormatter(Func<object?, string> formatter)
    {
        _column.ValueFormatter = formatter;
        return this;
    }

    /// <summary>Adds a CSS style to the column.</summary>
    public GridColumnBuilder AddStyle(string property, string value)
    {
        _column.Styles[property] = value;
        return this;
    }

    /// <summary>Builds and returns the grid column.</summary>
    public ReportGridColumn Build() => _column;

    public override string ToString() => $"GridColumnBuilder: {_column.Header} ({_column.Field ?? "no field"})";
}