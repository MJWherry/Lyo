using System.Diagnostics;

namespace Lyo.DataTable.Models;

/// <summary>A row of cells in a data table.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class DataTableRow
{
    private readonly Dictionary<int, IDataTableCell> _cells = new();

    /// <summary>Column index to cell mapping.</summary>
    public IReadOnlyDictionary<int, IDataTableCell> Cells => _cells;

    /// <summary>Gets or sets the cell at the given column.</summary>
    public IDataTableCell this[int col] {
        get => _cells.TryGetValue(col, out var c) ? c : DataTableCell.Empty;
        set => _cells[col] = value ?? DataTableCell.Empty;
    }

    /// <summary>Sets the cell at the given column.</summary>
    public DataTableRow SetCell(int col, IDataTableCell cell)
    {
        _cells[col] = cell ?? DataTableCell.Empty;
        return this;
    }

    /// <summary>Sets the cell at the given column with a value-only cell.</summary>
    public DataTableRow SetCell(int col, string value) => SetCell(col, DataTableCell.FromValue(value));

    public override string ToString() => $"DataTableRow(Cells: {string.Join(", ", _cells.Select(kv => $"[{kv.Key}: {kv.Value.DisplayValue}]"))})";
}