using System.Collections.Concurrent;
using System.Diagnostics;

namespace Lyo.DataTable.Models;

/// <summary>
/// Mutable data table with headers, rows, and footer. Cell values are thin (value + spans); optional formatting is stored in a sparse map keyed by (row, col). Row indices:
/// <c>-1</c> header, <c>-2</c> footer, <c>≥0</c> body. The format map and its accessors are thread-safe; concurrent mutation of cell structure still requires external
/// synchronization.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class DataTable
{
    private readonly Dictionary<int, IDataTableCell> _footer = new();
    private readonly Dictionary<int, IDataTableCell> _headers = new();
    private readonly List<DataTableRow> _rows = new();
    private ConcurrentDictionary<(int Row, int Col), DataTableCellFormat>? _formats;

    /// <summary>Column index to header cell.</summary>
    public IReadOnlyDictionary<int, IDataTableCell> Headers => _headers;

    /// <summary>Data rows. Each row contains column-to-cell mapping.</summary>
    public IReadOnlyList<DataTableRow> Rows => _rows;

    /// <summary>Column index to footer cell.</summary>
    public IReadOnlyDictionary<int, IDataTableCell> Footer => _footer;

    /// <summary>True when at least one format entry exists.</summary>
    public bool HasFormats {
        get {
            var formats = Volatile.Read(ref _formats);
            return formats is { Count: > 0 };
        }
    }

    /// <summary>Snapshot of the sparse format map. Absent key means no format for that cell. Enumeration is a copy so concurrent <see cref="SetFormat" /> does not tear the view.</summary>
    public IReadOnlyDictionary<(int Row, int Col), DataTableCellFormat> Formats {
        get {
            var formats = Volatile.Read(ref _formats);
            if (formats == null || formats.Count == 0)
                return new Dictionary<(int Row, int Col), DataTableCellFormat>();

            return new Dictionary<(int Row, int Col), DataTableCellFormat>(formats);
        }
    }

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
                _headers[col] = value;
                return;
            }

            if (row == -2) {
                _footer[col] = value;
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

    /// <summary>Gets the format for the cell at (row, col), or null when absent from the sparse map.</summary>
    public DataTableCellFormat? GetFormat(int row, int col)
    {
        var formats = Volatile.Read(ref _formats);
        if (formats == null)
            return null;

        return formats.TryGetValue((row, col), out var format) ? format : null;
    }

    /// <summary>Sets or clears format at (row, col). Null removes the key (no format entry is stored). Thread-safe with other format map operations.</summary>
    public DataTable SetFormat(int row, int col, DataTableCellFormat? format)
    {
        if (format == null) {
            ClearFormat(row, col);
            return this;
        }

        EnsureFormats()[(row, col)] = format;
        return this;
    }

    /// <summary>Removes the format entry at (row, col) if present.</summary>
    public DataTable ClearFormat(int row, int col)
    {
        var formats = Volatile.Read(ref _formats);
        formats?.TryRemove((row, col), out var _);
        return this;
    }

    /// <summary>Removes all format entries.</summary>
    public DataTable ClearFormats()
    {
        var formats = Volatile.Read(ref _formats);
        formats?.Clear();
        return this;
    }

    /// <summary>Sets the header at the given column index.</summary>
    public DataTable SetHeader(int col, IDataTableCell cell)
    {
        _headers[col] = cell;
        return this;
    }

    /// <summary>Sets the header cell and optional format. Null format clears any existing format at that header cell.</summary>
    public DataTable SetHeader(int col, IDataTableCell cell, DataTableCellFormat? format)
    {
        SetHeader(col, cell);
        SetFormat(-1, col, format);
        return this;
    }

    /// <summary>Sets the header at the given column index with a value-only cell.</summary>
    public DataTable SetHeader(int col, string value) => SetHeader(col, DataTableCell.FromValue(value));

    /// <summary>Sets the header value and optional format.</summary>
    public DataTable SetHeader(int col, string value, DataTableCellFormat? format) => SetHeader(col, DataTableCell.FromValue(value), format);

    /// <summary>Sets the footer at the given column index.</summary>
    public DataTable SetFooter(int col, IDataTableCell cell)
    {
        _footer[col] = cell;
        return this;
    }

    /// <summary>Sets the footer cell and optional format. Null format clears any existing format at that footer cell.</summary>
    public DataTable SetFooter(int col, IDataTableCell cell, DataTableCellFormat? format)
    {
        SetFooter(col, cell);
        SetFormat(-2, col, format);
        return this;
    }

    /// <summary>Sets the footer at the given column index with a value-only cell.</summary>
    public DataTable SetFooter(int col, string value) => SetFooter(col, DataTableCell.FromValue(value));

    /// <summary>Sets the footer value and optional format.</summary>
    public DataTable SetFooter(int col, string value, DataTableCellFormat? format) => SetFooter(col, DataTableCell.FromValue(value), format);

    /// <summary>Sets the cell at the given row and column.</summary>
    public DataTable SetCell(int row, int col, IDataTableCell cell)
    {
        EnsureRowCount(row + 1);
        _rows[row].SetCell(col, cell);
        return this;
    }

    /// <summary>Sets the cell and optional format. Null format clears any existing format at that coordinate.</summary>
    public DataTable SetCell(int row, int col, IDataTableCell cell, DataTableCellFormat? format)
    {
        SetCell(row, col, cell);
        SetFormat(row, col, format);
        return this;
    }

    /// <summary>Sets the cell at the given row and column with a value-only cell.</summary>
    public DataTable SetCell(int row, int col, string value) => SetCell(row, col, DataTableCell.FromValue(value));

    /// <summary>Sets the cell value and optional format.</summary>
    public DataTable SetCell(int row, int col, string value, DataTableCellFormat? format) => SetCell(row, col, DataTableCell.FromValue(value), format);

    /// <summary>Removes the body cell and its format at (row, col). Header/footer use row -1 / -2.</summary>
    public DataTable ClearCell(int row, int col)
    {
        if (row == -1)
            _headers.Remove(col);
        else if (row == -2)
            _footer.Remove(col);
        else if (row >= 0 && row < _rows.Count)
            _rows[row].ClearCell(col);

        ClearFormat(row, col);
        return this;
    }

    /// <summary>Adds a row and returns it for chaining.</summary>
    public DataTableRow AddRow()
    {
        var row = new DataTableRow();
        _rows.Add(row);
        return row;
    }

#if !NETSTANDARD2_0
    /// <summary>Enumerates body rows asynchronously for <c>await foreach</c> ergonomics. The table remains fully materialised in memory.</summary>
    public async IAsyncEnumerable<DataTableRow> EnumerateRowsAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var row in _rows) {
            ct.ThrowIfCancellationRequested();
            yield return row;
        }

        await Task.CompletedTask;
    }
#endif

    public override string ToString()
        => $"DataTable(Headers: {string.Join(", ", _headers.Select(kv => $"[{kv.Key}: {kv.Value.DisplayValue}]"))}, Rows: {_rows.Count}, Footer: {string.Join(", ", _footer.Select(kv => $"[{kv.Key}: {kv.Value.DisplayValue}]"))}, Formats: {(HasFormats ? Formats.Count : 0)})";

    private ConcurrentDictionary<(int Row, int Col), DataTableCellFormat> EnsureFormats()
    {
        var existing = Volatile.Read(ref _formats);
        if (existing != null)
            return existing;

        var created = new ConcurrentDictionary<(int Row, int Col), DataTableCellFormat>();
        var prior = Interlocked.CompareExchange(ref _formats, created, null);
        return prior ?? created;
    }

    private void EnsureRowCount(int count)
    {
        while (_rows.Count < count)
            _rows.Add(new());
    }
}