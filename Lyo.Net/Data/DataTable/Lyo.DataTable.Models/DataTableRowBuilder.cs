using Lyo.Exceptions;

namespace Lyo.DataTable.Models;

/// <summary>Fluent builder for DataTableRow. Optional formats are collected for application on the parent <see cref="DataTable" /> map.</summary>
public sealed class DataTableRowBuilder
{
    private readonly Dictionary<int, IDataTableCell> _cells = new();
    private readonly IReadOnlyDictionary<int, DataTableColumnBuilder> _columnDefs;
    private readonly Dictionary<int, DataTableCellFormat> _formats = new();
    private readonly DataTableBuilder? _parentBuilder;

    /// <summary>Creates a row builder with optional column definitions for FormatWhen.</summary>
    public DataTableRowBuilder(IReadOnlyDictionary<int, DataTableColumnBuilder>? columnDefs = null, DataTableBuilder? parentBuilder = null)
    {
        _columnDefs = columnDefs ?? new Dictionary<int, DataTableColumnBuilder>();
        _parentBuilder = parentBuilder;
    }

    /// <summary>Formats collected for this row (column → format). Applied to the table when the row is added.</summary>
    internal IReadOnlyDictionary<int, DataTableCellFormat> Formats => _formats;

    /// <summary>Sets a cell with a typed value. Column FormatWhen rules are applied when defined for this column. Use this (not AddCell) for typed values when FormatWhen is configured.</summary>
    public DataTableRowBuilder SetCell<T>(int col, T value)
    {
        if (_columnDefs.TryGetValue(col, out var colDef) && colDef.Rules.Count > 0) {
            var builder = new DataTableCellBuilder(value);
            foreach (var (predicate, apply) in colDef.Rules) {
                if (predicate(value))
                    apply(builder);
            }

            _cells[col] = builder.Build<T>();
            var format = builder.BuildFormat();
            if (format != null)
                _formats[col] = format;
            else
                _formats.Remove(col);
        }
        else {
            _cells[col] = DataTableCell<T>.FromValue(value);
            _formats.Remove(col);
        }

        return this;
    }

    /// <summary>Adds a cell with a string value. FormatWhen does not apply. Use SetCell&lt;T&gt; for typed values when FormatWhen is configured.</summary>
    public DataTableRowBuilder AddCell(int col, string value)
    {
        _cells[col] = DataTableCell.FromValue(value);
        _formats.Remove(col);
        return this;
    }

    /// <summary>Adds a cell at the given column.</summary>
    public DataTableRowBuilder AddCell(int col, IDataTableCell cell)
    {
        _cells[col] = cell;
        return this;
    }

    /// <summary>Adds a cell at the given column using a builder. Format is stored for the parent table map.</summary>
    public DataTableRowBuilder AddCell(int col, DataTableCellBuilder builder)
    {
        _cells[col] = builder.Build();
        var format = builder.BuildFormat();
        if (format != null)
            _formats[col] = format;
        else
            _formats.Remove(col);

        return this;
    }

    /// <summary>Adds cells for columns 0, 1, 2, ... from the given values.</summary>
    public DataTableRowBuilder AddCells(params string[] values)
    {
        for (var i = 0; i < values.Length; i++) {
            _cells[i] = DataTableCell.FromValue(values[i]);
            _formats.Remove(i);
        }

        return this;
    }

    /// <summary>Adds cells for columns 0, 1, 2, ... from the given cells.</summary>
    public DataTableRowBuilder AddCells(params IDataTableCell[] cells)
    {
        for (var i = 0; i < cells.Length; i++)
            _cells[i] = cells[i];

        return this;
    }

    /// <summary>Builds the DataTableRow (cells only; formats are applied by DataTableBuilder when adding the row).</summary>
    public DataTableRow Build()
    {
        var row = new DataTableRow();
        foreach (var kv in _cells)
            row.SetCell(kv.Key, kv.Value);

        return row;
    }

    /// <summary>Builds the row, adds it to the table (including formats), and returns the DataTableBuilder for further chaining.</summary>
    public DataTableBuilder BuildAndAdd()
    {
        OperationHelpers.ThrowIfNull(_parentBuilder, "BuildAndAdd requires this builder to be created from DataTableBuilder.AddRow().");
        _parentBuilder.AddRow(this);
        return _parentBuilder;
    }
}
