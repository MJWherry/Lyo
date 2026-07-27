using System.Text.Json;
using Lyo.Exceptions;
using Lyo.Reporting.Models;
using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Models.Rendering;
using Lyo.Xlsx.Models;

namespace Lyo.Reporting.Postgres.Rendering;

/// <summary>Renders all report grids to an XLSX workbook (one worksheet per grid) without Blazor.</summary>
public sealed class XlsxReportRenderer(IXlsxService xlsxService) : IReportRenderer
{
    /// <summary>Excel's hard worksheet-name limit.</summary>
    private const int MaxSheetNameLength = 31;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public bool CanRender(ReportFormat format) => format == ReportFormat.Xlsx;

    public Task<ReportRenderResult> RenderAsync(ReportRenderRequest request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.ReportDataJson);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.OutputFilePath);
        if (!CanRender(request.Format))
            throw new NotSupportedException($"{nameof(XlsxReportRenderer)} cannot render format {request.Format}.");

        var report = JsonSerializer.Deserialize<Report<object>>(request.ReportDataJson, JsonOptions)
                     ?? throw new ReportValidationException("Failed to deserialize report JSON.");

        var grids = CollectGrids(report);
        if (grids.Count == 0)
            throw new ReportValidationException("XLSX generation requires at least one grid in the report.");

        using (var writer = xlsxService.CreateDocumentWriter(request.OutputFilePath)) {
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;
            foreach (var grid in grids) {
                index++;
                var sheetName = BuildSheetName(grid.Title, index, usedNames);
                writer.AddSheetFromDictionary(sheetName, GridToDictionary(grid), useHeaderRow: true, ct);
            }
        }

        var fileName = request.SuggestedFileName ?? "report.xlsx";
        return Task.FromResult(
            new ReportRenderResult {
                FilePath = request.OutputFilePath,
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileName = fileName,
                ByteLength = new FileInfo(request.OutputFilePath).Length
            });
    }

    private static IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> GridToDictionary(ReportGrid grid)
    {
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

        return dict;
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

    private static string BuildSheetName(string? title, int index, HashSet<string> usedNames)
    {
        var baseName = string.IsNullOrWhiteSpace(title) ? $"Grid {index}" : title!.Trim();
        // Excel forbids these characters in worksheet names.
        baseName = new string(baseName.Where(c => c is not ('\\' or '/' or '*' or '[' or ']' or ':' or '?')).ToArray()).Trim();
        if (baseName.Length == 0)
            baseName = $"Grid {index}";
        if (baseName.Length > MaxSheetNameLength)
            baseName = baseName[..MaxSheetNameLength];

        var name = baseName;
        var suffix = 2;
        while (!usedNames.Add(name)) {
            var tag = $" ({suffix++})";
            name = baseName.Length + tag.Length > MaxSheetNameLength
                ? baseName[..(MaxSheetNameLength - tag.Length)] + tag
                : baseName + tag;
        }

        return name;
    }
}
