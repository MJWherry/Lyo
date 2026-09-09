using System.Text.Json;
using Lyo.DataTable.Models;
using Lyo.Exceptions;
using Lyo.Reporting.Models;
using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Controls;
using Lyo.Reporting.Models.Models;
using LyoDataTable = Lyo.DataTable.Models.DataTable;

namespace Lyo.Reporting.Web.Components;

/// <summary>Maps report composition tables to <see cref="LyoDataTable" /> snapshots for in-page preview.</summary>
public static class ReportGridDataTableMapper
{
    /// <summary>Deserializes report JSON and returns every table as a titled <see cref="LyoDataTable" /> (in section order).</summary>
    public static IReadOnlyList<(string Title, LyoDataTable Table)> FromReportDataJson(string? reportDataJson)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(reportDataJson);
        return FromReport(ReportJson.Deserialize<object>(reportDataJson));
    }

    /// <summary>Maps every table in the report (including tables nested in grids and subsections).</summary>
    public static IReadOnlyList<(string Title, LyoDataTable Table)> FromReport(Report<object> report)
    {
        ArgumentHelpers.ThrowIfNull(report);
        var tables = SectionBody.CollectTables(report.Sections);
        var result = new List<(string Title, LyoDataTable Table)>(tables.Count);
        for (var i = 0; i < tables.Count; i++) {
            var table = tables[i];
            var title = string.IsNullOrWhiteSpace(table.Title) ? $"Table {i + 1}" : table.Title!.Trim();
            result.Add((title, FromTable(table)));
        }

        return result;
    }

    /// <summary>Maps one <see cref="Table" /> to a <see cref="LyoDataTable" /> (headers + body rows).</summary>
    public static LyoDataTable FromTable(Table table)
    {
        ArgumentHelpers.ThrowIfNull(table);
        var builder = new DataTableBuilder();
        for (var c = 0; c < table.Columns.Count; c++)
            builder.AddHeader(c, table.Columns[c].Header ?? string.Empty);

        foreach (var row in table.Rows) {
            builder.AddRow(rb => {
                for (var c = 0; c < table.Columns.Count; c++) {
                    var value = c < row.Cells.Count ? row.Cells[c] : null;
                    var col = table.Columns[c];
                    var text = col.ValueFormatter != null ? col.ValueFormatter(value) : FormatValue(value);
                    rb.SetCell(c, text);
                }
            });
        }

        return builder.Build();
    }

    /// <summary>Flattens a table into header labels and row cell strings for a MudBlazor preview.</summary>
    public static (IReadOnlyList<string> Headers, IReadOnlyList<string[]> Rows) ToPreviewRows(LyoDataTable table)
    {
        ArgumentHelpers.ThrowIfNull(table);
        var maxCol = table.MaxColumn;
        if (maxCol < 0)
            return ([], []);

        var headers = new string[maxCol + 1];
        for (var c = 0; c <= maxCol; c++)
            headers[c] = table.Headers.TryGetValue(c, out var h) && !string.IsNullOrWhiteSpace(h.DisplayValue) ? h.DisplayValue : $"Col {c + 1}";

        var rows = new List<string[]>(table.Rows.Count);
        foreach (var row in table.Rows) {
            var cells = new string[maxCol + 1];
            for (var c = 0; c <= maxCol; c++)
                cells[c] = row[c].DisplayValue;

            rows.Add(cells);
        }

        return (headers, rows);
    }

    /// <summary>First table only.</summary>
    public static LyoDataTable? FirstTableFromReportDataJson(string? reportDataJson)
    {
        var sheets = FromReportDataJson(reportDataJson);
        return sheets.Count > 0 ? sheets[0].Table : null;
    }

    private static string FormatValue(object? value)
        => value switch {
            null => string.Empty,
            JsonElement { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined } => string.Empty,
            JsonElement { ValueKind: JsonValueKind.String } je => je.GetString() ?? string.Empty,
            JsonElement { ValueKind: JsonValueKind.True } => "True",
            JsonElement { ValueKind: JsonValueKind.False } => "False",
            JsonElement je => je.ToString(),
            _ => value.ToString() ?? string.Empty
        };
}
