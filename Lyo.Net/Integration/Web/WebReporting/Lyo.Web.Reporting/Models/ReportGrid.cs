using System.Diagnostics;

namespace Lyo.Web.Reporting.Models;

/// <summary>Represents a grid/table within a report section.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ReportGrid
{
    /// <summary>Gets or sets the grid title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the grid caption.</summary>
    public string? Caption { get; set; }

    /// <summary>Gets the list of column definitions.</summary>
    public List<ReportGridColumn> Columns { get; set; } = [];

    /// <summary>Gets the list of rows in the grid.</summary>
    public List<ReportGridRow> Rows { get; set; } = [];

    /// <summary>Gets or sets whether to show column headers.</summary>
    public bool ShowHeaders { get; set; } = true;

    /// <summary>Gets or sets whether to show row numbers.</summary>
    public bool ShowRowNumbers { get; set; }

    /// <summary>Gets or sets whether the grid should be striped.</summary>
    public bool Striped { get; set; } = true;

    /// <summary>Gets or sets whether the grid should have borders.</summary>
    public bool Bordered { get; set; } = true;

    /// <summary>Gets or sets custom CSS styles for the grid.</summary>
    public Dictionary<string, string> Styles { get; set; } = [];

    public override string ToString() => $"Grid: {Title ?? "(Untitled)"} ({Columns.Count} columns, {Rows.Count} rows)";
}

/// <summary>Represents a column definition in a report grid.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ReportGridColumn
{
    /// <summary>Gets or sets the column header text.</summary>
    public string Header { get; set; } = string.Empty;

    /// <summary>Gets or sets the column data field/property name.</summary>
    public string? Field { get; set; }

    /// <summary>Gets or sets the column width (as CSS value).</summary>
    public string? Width { get; set; }

    /// <summary>Gets or sets the column alignment (left, right, center, justify).</summary>
    public string? Alignment { get; set; }

    /// <summary>Gets or sets a formatter function for cell values.</summary>
    public Func<object?, string>? ValueFormatter { get; set; }

    /// <summary>Gets or sets custom CSS styles for this column.</summary>
    public Dictionary<string, string> Styles { get; set; } = [];

    public override string ToString() => $"GridColumn: {Header} ({Field ?? "no field"})";
}

/// <summary>Represents a row in a report grid.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ReportGridRow
{
    /// <summary>Gets the list of cell values in this row.</summary>
    public List<object?> Cells { get; set; } = [];

    /// <summary>Gets or sets whether this row should be emphasized.</summary>
    public bool Emphasized { get; set; }

    /// <summary>Gets or sets custom CSS styles for this row.</summary>
    public Dictionary<string, string> Styles { get; set; } = [];

    public override string ToString() => $"GridRow: {Cells.Count} cells";
}