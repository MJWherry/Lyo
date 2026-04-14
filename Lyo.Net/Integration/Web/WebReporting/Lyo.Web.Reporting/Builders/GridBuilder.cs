using System.Diagnostics;
using Lyo.Web.Reporting.Models;

namespace Lyo.Web.Reporting.Builders;

/// <summary>Fluent builder for constructing report grids/tables.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class GridBuilder
{
    private readonly ReportGrid _grid = new();

    /// <summary>Sets the grid title.</summary>
    public GridBuilder SetTitle(string title)
    {
        _grid.Title = title;
        return this;
    }

    /// <summary>Sets the grid caption.</summary>
    public GridBuilder SetCaption(string caption)
    {
        _grid.Caption = caption;
        return this;
    }

    /// <summary>Sets whether to show column headers.</summary>
    public GridBuilder SetShowHeaders(bool showHeaders = true)
    {
        _grid.ShowHeaders = showHeaders;
        return this;
    }

    /// <summary>Sets whether to show row numbers.</summary>
    public GridBuilder SetShowRowNumbers(bool showRowNumbers = true)
    {
        _grid.ShowRowNumbers = showRowNumbers;
        return this;
    }

    /// <summary>Sets whether the grid should be striped.</summary>
    public GridBuilder SetStriped(bool striped = true)
    {
        _grid.Striped = striped;
        return this;
    }

    /// <summary>Sets whether the grid should have borders.</summary>
    public GridBuilder SetBordered(bool bordered = true)
    {
        _grid.Bordered = bordered;
        return this;
    }

    /// <summary>Adds a CSS style to the grid.</summary>
    public GridBuilder AddStyle(string property, string value)
    {
        _grid.Styles[property] = value;
        return this;
    }

    /// <summary>Adds a column definition to the grid.</summary>
    public GridBuilder AddColumn(string header, string? field = null, string? width = null, string? alignment = null)
    {
        _grid.Columns.Add(
            new() {
                Header = header,
                Field = field,
                Width = width,
                Alignment = alignment
            });

        return this;
    }

    /// <summary>Adds a column definition using a grid column builder.</summary>
    public GridBuilder AddColumn(Action<GridColumnBuilder> configure)
    {
        var builder = new GridColumnBuilder();
        configure(builder);
        _grid.Columns.Add(builder.Build());
        return this;
    }

    /// <summary>Adds a column definition using a grid column builder instance.</summary>
    public GridBuilder AddColumn(GridColumnBuilder columnBuilder)
    {
        _grid.Columns.Add(columnBuilder.Build());
        return this;
    }

    /// <summary>Adds multiple column definitions from headers.</summary>
    public GridBuilder AddColumns(params string[] headers)
    {
        foreach (var header in headers)
            AddColumn(header, header);

        return this;
    }

    /// <summary>Adds a row to the grid.</summary>
    public GridBuilder AddRow(params object?[] cells)
    {
        _grid.Rows.Add(new() { Cells = cells.ToList() });
        return this;
    }

    /// <summary>Adds a row to the grid.</summary>
    public GridBuilder AddRow(IEnumerable<object?> cells)
    {
        _grid.Rows.Add(new() { Cells = cells.ToList() });
        return this;
    }

    /// <summary>Adds a row using a row builder.</summary>
    public GridBuilder AddRow(Action<GridRowBuilder> configure)
    {
        var builder = new GridRowBuilder();
        configure(builder);
        _grid.Rows.Add(builder.Build());
        return this;
    }

    /// <summary>Adds a row using a row builder instance.</summary>
    public GridBuilder AddRow(GridRowBuilder rowBuilder)
    {
        _grid.Rows.Add(rowBuilder.Build());
        return this;
    }

    /// <summary>Adds multiple rows from a collection of objects, mapping properties to columns.</summary>
    public GridBuilder AddRowsFromObjects<TItem>(IEnumerable<TItem> items, Func<TItem, object?[]>? mapper = null)
    {
        foreach (var item in items) {
            if (mapper != null)
                AddRow(mapper(item));
            else {
                // Auto-map properties if columns are defined with Field names
                var cells = new List<object?>();
                foreach (var col in _grid.Columns) {
                    if (!string.IsNullOrEmpty(col.Field)) {
                        var prop = typeof(TItem).GetProperty(col.Field);
                        cells.Add(prop?.GetValue(item));
                    }
                    else
                        cells.Add(null);
                }

                AddRow(cells);
            }
        }

        return this;
    }

    /// <summary>Builds and returns the grid.</summary>
    public ReportGrid Build() => _grid;

    public override string ToString() => $"GridBuilder: {_grid.Title ?? "(Untitled)"} ({_grid.Columns.Count} columns, {_grid.Rows.Count} rows)";
}