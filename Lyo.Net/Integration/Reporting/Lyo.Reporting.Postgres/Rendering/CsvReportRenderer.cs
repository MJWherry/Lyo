using System.Text.Json;
using Lyo.Csv.Models;
using Lyo.Exceptions;
using Lyo.Reporting.Models;
using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Models.Rendering;

namespace Lyo.Reporting.Postgres.Rendering;

/// <summary>Renders the first (or all) report grids to CSV without Blazor.</summary>
public sealed class CsvReportRenderer(ICsvService csvService) : IReportRenderer
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public bool CanRender(ReportFormat format) => format == ReportFormat.Csv;

    public async Task<ReportRenderResult> RenderAsync(ReportRenderRequest request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.ReportDataJson);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.OutputFilePath);
        if (!CanRender(request.Format))
            throw new NotSupportedException($"{nameof(CsvReportRenderer)} cannot render format {request.Format}.");

        ct.ThrowIfCancellationRequested();

        var report = JsonSerializer.Deserialize<Report<object>>(request.ReportDataJson, JsonOptions)
                     ?? throw new ReportValidationException("Failed to deserialize report JSON.");

        var grid = FindFirstGrid(report)
                   ?? throw new ReportValidationException("CSV generation requires at least one grid in the report.");

        var dict = new Dictionary<int, IReadOnlyDictionary<int, string>>();
        var header = new Dictionary<int, string>();
        for (var c = 0; c < grid.Columns.Count; c++)
            header[c] = grid.Columns[c].Header;

        dict[0] = header;
        for (var r = 0; r < grid.Rows.Count; r++) {
            var row = grid.Rows[r];
            var cells = new Dictionary<int, string>();
            for (var c = 0; c < grid.Columns.Count; c++) {
                var value = c < row.Cells.Count ? row.Cells[c] : null;
                var col = grid.Columns[c];
                cells[c] = col.ValueFormatter != null ? col.ValueFormatter(value) : value?.ToString() ?? string.Empty;
            }

            dict[r + 1] = cells;
        }

        await csvService.ExportToCsvFromDictionaryAsync(dict, request.OutputFilePath, hasHeaderRow: true, ct).ConfigureAwait(false);
        var fileName = request.SuggestedFileName ?? "report.csv";
        return new ReportRenderResult {
            FilePath = request.OutputFilePath,
            ContentType = "text/csv; charset=utf-8",
            FileName = fileName,
            ByteLength = new FileInfo(request.OutputFilePath).Length
        };
    }

    private static ReportGrid? FindFirstGrid(Report<object> report)
    {
        foreach (var section in report.Sections.OrderBy(s => s.Order)) {
            if (section.Grids.Count > 0)
                return section.Grids[0];
            foreach (var sub in section.Subsections.OrderBy(s => s.Order)) {
                if (sub.Grids.Count > 0)
                    return sub.Grids[0];
            }
        }

        return null;
    }
}
