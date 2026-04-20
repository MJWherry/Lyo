using System.Diagnostics;
using Lyo.Web.Reporting.Models;

namespace Lyo.Web.Reporting.Builders;

/// <summary>Fluent builder for constructing report columns.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ColumnBuilder
{
    private readonly ReportColumn _column = new();

    /// <summary>Sets the column label.</summary>
    public ColumnBuilder SetLabel(string label)
    {
        _column.Label = label;
        return this;
    }

    /// <summary>Sets the column value.</summary>
    public ColumnBuilder SetValue(object? value)
    {
        _column.Value = value;
        return this;
    }

    /// <summary>Sets the column width (as CSS value, e.g., "50%", "200px").</summary>
    public ColumnBuilder SetWidth(string width)
    {
        _column.Width = width;
        return this;
    }

    /// <summary>Sets the column alignment (left, right, center, justify).</summary>
    public ColumnBuilder SetAlignment(string alignment)
    {
        _column.Alignment = alignment;
        return this;
    }

    /// <summary>Sets whether this column should be emphasized.</summary>
    public ColumnBuilder SetEmphasized(bool emphasized = true)
    {
        _column.Emphasized = emphasized;
        return this;
    }

    /// <summary>Adds a CSS style to the column.</summary>
    public ColumnBuilder AddStyle(string property, string value)
    {
        _column.Styles[property] = value;
        return this;
    }

    /// <summary>Sets a formatter function for the value.</summary>
    public ColumnBuilder SetFormatter(Func<object?, string> formatter)
    {
        _column.ValueFormatter = formatter;
        return this;
    }

    /// <summary>Builds and returns the column.</summary>
    public ReportColumn Build() => _column;

    public override string ToString() => $"ColumnBuilder: {_column.Label ?? "(No Label)"} = {_column.Value}";
}