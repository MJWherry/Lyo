using System.Diagnostics;
using System.Text.Json.Serialization;
using Lyo.Exceptions;

namespace Lyo.Reporting.Models.Controls;

/// <summary>A data table (headers and cells). Not a layout <see cref="Grid" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class Table : Control
{
    /// <summary>Table title.</summary>
    public string? Title { get; set; }

    /// <summary>Table caption.</summary>
    public string? Caption { get; set; }

    /// <summary>Column definitions.</summary>
    public List<TableColumn> Columns { get; set; } = [];

    /// <summary>Rows in the table. Ignored when the data source is not <see cref="DataSourceKind.Static" /> after bind.</summary>
    public List<TableRow> Rows { get; set; } = [];

    /// <summary>Whether column headers are shown.</summary>
    public bool ShowHeaders { get; set; } = true;

    /// <summary>Whether row numbers are shown.</summary>
    public bool ShowRowNumbers { get; set; }

    /// <summary>Whether the table uses striped rows.</summary>
    public bool Striped { get; set; } = true;

    /// <summary>Whether the table draws borders.</summary>
    public bool Bordered { get; set; } = true;

    /// <summary>Whether rows come from authored cells, a parameter, a query, or a stored procedure.</summary>
    public DataSourceKind DataSourceKind { get; set; }

    /// <summary>Parameter key holding row JSON when <see cref="DataSourceKind" /> is <see cref="Controls.DataSourceKind.FromParameter" />.</summary>
    public string? DataParameterKey { get; set; }

    /// <summary>
    /// Query or sproc <c>ParameterOptions</c> JSON when <see cref="DataSourceKind" /> is <see cref="Controls.DataSourceKind.Query" /> or
    /// <see cref="Controls.DataSourceKind.Sproc" />.
    /// </summary>
    public string? Options { get; set; }

    /// <summary>Maps table column <see cref="TableColumn.Field" /> (or header) onto a dataset column name.</summary>
    public Dictionary<string, string> FieldMap { get; set; } = [];

    /// <summary>Grows or shrinks <see cref="Columns" /> (and lockstep row cells). Minimum 1.</summary>
    public void SetColumnCount(int count)
    {
        ArgumentHelpers.ThrowIfLessThan(count, 1);
        while (Columns.Count < count)
            Columns.Add(new() { Header = $"Column {Columns.Count + 1}" });

        if (Columns.Count > count)
            Columns.RemoveRange(count, Columns.Count - count);

        foreach (var row in Rows) {
            while (row.Cells.Count < Columns.Count)
                row.Cells.Add(null);

            if (row.Cells.Count > Columns.Count)
                row.Cells.RemoveRange(Columns.Count, row.Cells.Count - Columns.Count);
        }
    }

    /// <summary>Grows or shrinks <see cref="Rows" />. Zero is allowed for parameter/query tables.</summary>
    public void SetRowCount(int count)
    {
        ArgumentHelpers.ThrowIfLessThan(count, 0);
        var width = Math.Max(Columns.Count, 1);
        while (Rows.Count < count)
            Rows.Add(new() { Cells = Enumerable.Repeat<object?>(null, width).ToList() });

        if (Rows.Count > count)
            Rows.RemoveRange(count, Rows.Count - count);
    }

    public override string ToString() => $"Table: {Title ?? "(Untitled)"} ({Columns.Count} columns, {Rows.Count} rows)";
}

/// <summary>A column definition in a table.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class TableColumn
{
    /// <summary>Column header text.</summary>
    public string Header { get; set; } = string.Empty;

    /// <summary>Column data field or property name.</summary>
    public string? Field { get; set; }

    /// <summary>Column width as a CSS value.</summary>
    public string? Width { get; set; }

    /// <summary>Column alignment (left, right, center, or justify).</summary>
    public string? Alignment { get; set; }

    /// <summary>Formatter for cell values. Not persisted in JSON.</summary>
    [JsonIgnore]
    public Func<object?, string>? ValueFormatter { get; set; }

    /// <summary>Custom CSS styles for this column.</summary>
    public Dictionary<string, string> Styles { get; set; } = [];

    public override string ToString() => $"TableColumn: {Header} ({Field ?? "no field"})";
}

/// <summary>A row in a table.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class TableRow
{
    /// <summary>Cell values in this row.</summary>
    public List<object?> Cells { get; set; } = [];

    /// <summary>Whether this row is emphasized.</summary>
    public bool Emphasized { get; set; }

    /// <summary>Custom CSS styles for this row.</summary>
    public Dictionary<string, string> Styles { get; set; } = [];

    public override string ToString() => $"TableRow: {Cells.Count} cells";
}
