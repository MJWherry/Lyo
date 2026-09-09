using Lyo.DataTable.Models;
using LyoDataTable = Lyo.DataTable.Models.DataTable;
using Lyo.Exceptions;

namespace Lyo.TestGateway.Services;

/// <summary>Merges <see cref="LyoDataTable" /> instances for CSV/XLSX workbench combine flows.</summary>
public static class TabularDataMergeHelper
{
    /// <summary>Adds rows from <paramref name="second" /> beneath <paramref name="first" />, keeping the first table's headers and row order.</summary>
    /// <param name="first">Lead table; its headers and rows are copied first.</param>
    /// <param name="second">Follow-on table; its rows are appended after the first.</param>
    /// <param name="skipFirstRowOfSecond">If true, the second table's first data row is omitted (for example a repeated header).</param>
    public static LyoDataTable AppendRows(LyoDataTable first, LyoDataTable second, bool skipFirstRowOfSecond)
    {
        ArgumentHelpers.ThrowIfNull(first);
        ArgumentHelpers.ThrowIfNull(second);
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