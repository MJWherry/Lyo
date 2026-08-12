using System.Text.Json;
using Lyo.DataTable.Models;
using Lyo.Exceptions;
using Lyo.Reporting.Models;
using LyoDataTable = Lyo.DataTable.Models.DataTable;

namespace Lyo.Reporting.Web.Components;

/// <summary>Maps report composition grids into <see cref="LyoDataTable" /> snapshots for in-page preview.</summary>
public static class ReportGridDataTableMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>Deserializes report JSON and returns every grid as a titled <see cref="LyoDataTable" /> (section order).</summary>
    public static IReadOnlyList<(string Title, LyoDataTable Table)> FromReportDataJson(string? reportDataJson)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(reportDataJson);
        var report = JsonSerializer.Deserialize<Report<object>>(reportDataJson, JsonOptions)
                     ?? throw new InvalidOperationException("Failed to deserialize report JSON.");
        return FromReport(report);
    }

    /// <summary>Maps all grids in the report (ordered by section / subsection).</summary>
    public static IReadOnlyList<(string Title, LyoDataTable Table)> FromReport(Report<object> report)
    {
        ArgumentHelpers.ThrowIfNull(report);
        var grids = CollectGrids(report);
        var result = new List<(string Title, LyoDataTable Table)>(grids.Count);
        for (var i = 0; i < grids.Count; i++) {
            var grid = grids[i];
            var title = string.IsNullOrWhiteSpace(grid.Title) ? $"Grid {i + 1}" : grid.Title!.Trim();
            result.Add((title, FromGrid(grid)));
        }

        return result;
    }

    /// <summary>Maps a single <see cref="ReportGrid" /> to a <see cref="LyoDataTable" /> (headers + body rows).</summary>
    public static LyoDataTable FromGrid(ReportGrid grid)
    {
        ArgumentHelpers.ThrowIfNull(grid);
        var builder = new DataTableBuilder();
        for (var c = 0; c < grid.Columns.Count; c++)
            builder.AddHeader(c, grid.Columns[c].Header ?? string.Empty);

        foreach (var row in grid.Rows) {
            builder.AddRow(rb => {
                for (var c = 0; c < grid.Columns.Count; c++) {
                    var value = c < row.Cells.Count ? row.Cells[c] : null;
                    var col = grid.Columns[c];
                    var text = col.ValueFormatter != null ? col.ValueFormatter(value) : FormatValue(value);
                    rb.SetCell(c, text);
                }
            });
        }

        return builder.Build();
    }

    /// <summary>First grid only (matches CSV renderer behavior).</summary>
    public static LyoDataTable? FirstGridFromReportDataJson(string? reportDataJson)
    {
        var sheets = FromReportDataJson(reportDataJson);
        return sheets.Count > 0 ? sheets[0].Table : null;
    }

    private static List<ReportGrid> CollectGrids(Report<object> report)
    {
        var grids = new List<ReportGrid>();
        foreach (var section in report.Sections.OrderBy(s => s.Order)) {
            grids.AddRange(section.Grids);
            foreach (var sub in section.Subsections.OrderBy(s => s.Order))
                grids.AddRange(sub.Grids);
        }

        return grids;
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
