using Lyo.DataTable.Models;
using LyoDataTable = Lyo.DataTable.Models.DataTable;

namespace Lyo.Gateway.Services;

/// <summary>Combines <see cref="LyoDataTable" /> instances for CSV/XLSX workbench merge operations.</summary>
public static class TabularDataMergeHelper
{
    /// <summary>Appends data rows from <paramref name="second" /> under <paramref name="first" />, using the first table's headers and row order.</summary>
    /// <param name="first">Primary table (headers and rows are copied first).</param>
    /// <param name="second">Secondary table; its rows are appended after the first.</param>
    /// <param name="skipFirstRowOfSecond">When true, the first data row of the second table is skipped (e.g. duplicate header row).</param>
    public static LyoDataTable AppendRows(LyoDataTable first, LyoDataTable second, bool skipFirstRowOfSecond)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        var result = CloneTable(first);
        var maxCol = Math.Max(result.MaxColumn, second.MaxColumn);
        if (second.MaxColumn > first.MaxColumn) {
            for (var c = first.MaxColumn + 1; c <= maxCol; c++) {
                if (second.Headers.TryGetValue(c, out var h))
                    result.SetHeader(c, h.DisplayValue);
            }
        }

        var rows = second.Rows;
        var start = skipFirstRowOfSecond && rows.Count > 0 ? 1 : 0;
        for (var i = start; i < rows.Count; i++) {
            var row = rows[i];
            var nr = result.AddRow();
            foreach (var col in row.Cells.Keys.OrderBy(x => x))
                nr.SetCell(col, DataTableCell.FromValue(row[col].DisplayValue));
        }

        return result;
    }

    private static LyoDataTable CloneTable(LyoDataTable source)
    {
        var dt = new LyoDataTable();
        foreach (var kv in source.Headers.OrderBy(x => x.Key))
            dt.SetHeader(kv.Key, kv.Value);

        foreach (var row in source.Rows) {
            var nr = dt.AddRow();
            foreach (var col in row.Cells.Keys.OrderBy(x => x))
                nr.SetCell(col, DataTableCell.FromValue(row[col].DisplayValue));
        }

        return dt;
    }
}