using Lyo.Exceptions;
using Lyo.Reporting.Models;
using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Models.Controls;
using Lyo.Reporting.Models.Models;
using Lyo.Reporting.Models.Rendering;
using Lyo.Xlsx.Models;

namespace Lyo.Reporting.Postgres.Rendering;

/// <summary>Renders every report table to an XLSX workbook (one worksheet per table) without Blazor.</summary>
public sealed class XlsxReportRenderer(IXlsxService xlsxService) : IReportRenderer
{
    /// <summary>Excel's hard limit on worksheet names.</summary>
    private const int MaxSheetNameLength = 31;

    public bool CanRender(ReportFormat format) => format == ReportFormat.Xlsx;

    public Task<ReportRenderResult> RenderAsync(ReportRenderRequest request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.ReportDataJson);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.OutputFilePath);
        if (!CanRender(request.Format))
            throw new NotSupportedException($"{nameof(XlsxReportRenderer)} cannot render format {request.Format}.");

        ct.ThrowIfCancellationRequested();
        var report = ReportCompositionProcessor.BindJson(request.ReportDataJson, request.Parameters);
        var tables = SectionBody.CollectTables(report.Sections);
        if (tables.Count == 0)
            throw new ReportValidationException("XLSX generation requires at least one table in the report.");

        using (var writer = xlsxService.CreateDocumentWriter(request.OutputFilePath)) {
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;
            foreach (var table in tables) {
                ct.ThrowIfCancellationRequested();
                index++;
                var sheetName = BuildSheetName(table.Title, index, usedNames);
                writer.AddSheetFromDictionary(sheetName, TableToDictionary(table), true, false, ct);
            }
        }

        var fileName = request.SuggestedFileName ?? "report" + ReportFormat.Xlsx.Extension;
        return Task.FromResult(
            new ReportRenderResult {
                FilePath = request.OutputFilePath,
                ContentType = ReportFormat.Xlsx.ContentType,
                FileName = fileName,
                ByteLength = new FileInfo(request.OutputFilePath).Length
            });
    }

    private static IReadOnlyDictionary<int, IReadOnlyDictionary<int, string>> TableToDictionary(Table table)
    {
        var dict = new Dictionary<int, IReadOnlyDictionary<int, string>>();
        var header = new Dictionary<int, string>();
        for (var c = 0; c < table.Columns.Count; c++)
            header[c] = table.Columns[c].Header;

        dict[0] = header;
        for (var r = 0; r < table.Rows.Count; r++) {
            var row = table.Rows[r];
            var cells = new Dictionary<int, string>();
            for (var c = 0; c < table.Columns.Count; c++) {
                var value = c < row.Cells.Count ? row.Cells[c] : null;
                var col = table.Columns[c];
                cells[c] = col.ValueFormatter != null ? col.ValueFormatter(value) : value?.ToString() ?? string.Empty;
            }

            dict[r + 1] = cells;
        }

        return dict;
    }

    private static string BuildSheetName(string? title, int index, HashSet<string> usedNames)
    {
        var baseName = string.IsNullOrWhiteSpace(title) ? $"Table {index}" : title!.Trim();
        // Excel does not allow these characters in worksheet names.
        baseName = new string(baseName.Where(c => c is not ('\\' or '/' or '*' or '[' or ']' or ':' or '?')).ToArray()).Trim();
        if (baseName.Length == 0)
            baseName = $"Table {index}";

        if (baseName.Length > MaxSheetNameLength)
            baseName = baseName[..MaxSheetNameLength];

        var name = baseName;
        var suffix = 2;
        while (!usedNames.Add(name)) {
            var tag = $" ({suffix++})";
            name = baseName.Length + tag.Length > MaxSheetNameLength ? baseName[..(MaxSheetNameLength - tag.Length)] + tag : baseName + tag;
        }

        return name;
    }
}