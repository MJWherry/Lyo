using System.Diagnostics;

namespace Lyo.DataTable.Models;

/// <summary>Mutable data table with headers, rows, and footer. Supports per-cell formatting and programmatic construction.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class DataTable
{
    private readonly Dictionary<int, IDataTableCell> _footer = new();
    private readonly Dictionary<int, IDataTableCell> _headers = new();
    private readonly List<DataTableRow> _rows = new();

    /// <summary>Column index to header cell.</summary>
    public IReadOnlyDictionary<int, IDataTableCell> Headers => _headers;

    /// <summary>Data rows. Each row contains column-to-cell mapping.</summary>
    public IReadOnlyList<DataTableRow> Rows => _rows;

    /// <summary>Column index to footer cell.</summary>
    public IReadOnlyDictionary<int, IDataTableCell> Footer => _footer;

    /// <summary>Gets or sets the cell at the given row and column. Use row=-1 for header, row=-2 for footer.</summary>
    public IDataTableCell this[int row, int col] {
        get {
            if (row == -1)
                return _headers.TryGetValue(col, out var h) ? h : DataTableCell.Empty;

            if (row == -2)
                return _footer.TryGetValue(col, out var f) ? f : DataTableCell.Empty;

            if (row < 0 || row >= _rows.Count)
                return DataTableCell.Empty;

            return _rows[row][col];
        }
        set {
            if (row == -1) {
                _headers[col] = value ?? DataTableCell.Empty;
                return;
            }

            if (row == -2) {
                _footer[col] = value ?? DataTableCell.Empty;
                return;
            }

            EnsureRowCount(row + 1);
            _rows[row].SetCell(col, value);
        }
    }

    /// <summary>Gets the maximum column index from headers, rows, and footer.</summary>
    public int MaxColumn
        => Math.Max(
            Math.Max(_headers.Count > 0 ? _headers.Keys.Max() : -1, _rows.Count > 0 ? _rows.Select(r => r.Cells.Count > 0 ? r.Cells.Keys.Max() : -1).Max() : -1),
            _footer.Count > 0 ? _footer.Keys.Max() : -1);

    /// <summary>Sets the header at the given column index.</summary>
    public DataTable SetHeader(int col, IDataTableCell cell)
    {
        _headers[col] = cell ?? DataTableCell.Empty;
        return this;
    }

    /// <summary>Sets the header at the given column index with a value-only cell.</summary>
    public DataTable SetHeader(int col, string value) => SetHeader(col, DataTableCell.FromValue(value));

    /// <summary>Sets the footer at the given column index.</summary>
    public DataTable SetFooter(int col, IDataTableCell cell)
    {
        _footer[col] = cell ?? DataTableCell.Empty;
        return this;
    }

    /// <summary>Sets the footer at the given column index with a value-only cell.</summary>
    public DataTable SetFooter(int col, string value) => SetFooter(col, DataTableCell.FromValue(value));

    /// <summary>Sets the cell at the given row and column.</summary>
    public DataTable SetCell(int row, int col, IDataTableCell cell)
    {
        EnsureRowCount(row + 1);
        _rows[row].SetCell(col, cell);
        return this;
    }

    /// <summary>Sets the cell at the given row and column with a value-only cell.</summary>
    public DataTable SetCell(int row, int col, string value) => SetCell(row, col, DataTableCell.FromValue(value));

    /// <summary>Adds a row and returns it for chaining.</summary>
    public DataTableRow AddRow()
    {
        var row = new DataTableRow();
        _rows.Add(row);
        return row;
    }

    public override string ToString()
        => $"DataTable(Headers: {string.Join(", ", _headers.Select(kv => $"[{kv.Key}: {kv.Value.DisplayValue}]"))}, Rows: {_rows.Count}, Footer: {string.Join(", ", _footer.Select(kv => $"[{kv.Key}: {kv.Value.DisplayValue}]"))})";

    private void EnsureRowCount(int count)
    {
        while (_rows.Count < count)
            _rows.Add(new());
    }
}