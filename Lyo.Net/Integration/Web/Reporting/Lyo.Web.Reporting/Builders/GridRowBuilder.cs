using System.Diagnostics;
using Lyo.Web.Reporting.Models;

namespace Lyo.Web.Reporting.Builders;

/// <summary>Fluent builder for constructing grid rows.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class GridRowBuilder
{
    private readonly ReportGridRow _row = new();

    /// <summary>Adds a cell to the row.</summary>
    public GridRowBuilder AddCell(object? value)
    {
        _row.Cells.Add(value);
        return this;
    }

    /// <summary>Adds multiple cells to the row.</summary>
    public GridRowBuilder AddCells(params object?[] cells)
    {
        foreach (var cell in cells)
            _row.Cells.Add(cell);

        return this;
    }

    /// <summary>Adds multiple cells to the row.</summary>
    public GridRowBuilder AddCells(IEnumerable<object?> cells)
    {
        foreach (var cell in cells)
            _row.Cells.Add(cell);

        return this;
    }

    /// <summary>Sets whether this row should be emphasized.</summary>
    public GridRowBuilder SetEmphasized(bool emphasized = true)
    {
        _row.Emphasized = emphasized;
        return this;
    }

    /// <summary>Adds a CSS style to the row.</summary>
    public GridRowBuilder AddStyle(string property, string value)
    {
        _row.Styles[property] = value;
        return this;
    }

    /// <summary>Builds and returns the row.</summary>
    public ReportGridRow Build() => _row;

    public override string ToString() => $"GridRowBuilder: {_row.Cells.Count} cells";
}