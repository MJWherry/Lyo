using System.Diagnostics;
using Lyo.Reporting.Models.Controls;

namespace Lyo.Reporting.Models.Builders;

/// <summary>Fluent builder for table rows.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class TableRowBuilder
{
    private readonly TableRow _row = new();

    /// <summary>Appends a cell to the row.</summary>
    public TableRowBuilder AddCell(object? value)
    {
        _row.Cells.Add(value);
        return this;
    }

    /// <summary>Appends several cells to the row.</summary>
    public TableRowBuilder AddCells(params object?[] cells)
    {
        foreach (var cell in cells)
            _row.Cells.Add(cell);

        return this;
    }

    /// <summary>Appends several cells to the row.</summary>
    public TableRowBuilder AddCells(IEnumerable<object?> cells)
    {
        foreach (var cell in cells)
            _row.Cells.Add(cell);

        return this;
    }

    /// <summary>Sets whether this row is emphasized.</summary>
    public TableRowBuilder SetEmphasized(bool emphasized = true)
    {
        _row.Emphasized = emphasized;
        return this;
    }

    /// <summary>Adds a CSS style on the row.</summary>
    public TableRowBuilder AddStyle(string property, string value)
    {
        _row.Styles[property] = value;
        return this;
    }

    /// <summary>Builds and returns the finished row.</summary>
    public TableRow Build() => _row;

    public override string ToString() => $"TableRowBuilder: {_row.Cells.Count} cells";
}
