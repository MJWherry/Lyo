using Lyo.Csv.Models;
using Lyo.Exceptions;
using Lyo.Reporting.Models;
using Lyo.Reporting.Models.Composition;
using Lyo.Reporting.Models.Controls;
using Lyo.Reporting.Models.Enums;
using Lyo.Reporting.Models.Models;
using Lyo.Reporting.Models.Rendering;

namespace Lyo.Reporting.Postgres.Rendering;

/// <summary>
/// Renders every report table to one CSV without Blazor. Tables after the first are appended below a blank separator row and a title row, so a multi-table report keeps all
/// of its data (the XLSX renderer puts each table on its own worksheet, which CSV has no equivalent for).
/// </summary>
public sealed class CsvReportRenderer(ICsvService csvService) : IReportRenderer
{
    /// <summary>Rows buffered before a write. Each flush writes its own rows verbatim, so the chunk boundary does not appear in the output.</summary>
    private const int RowsPerFlush = 2_000;

    public bool CanRender(ReportFormat format) => format == ReportFormat.Csv;

    public async Task<ReportRenderResult> RenderAsync(ReportRenderRequest request, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(request);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.ReportDataJson);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(request.OutputFilePath);
        if (!CanRender(request.Format))
            throw new NotSupportedException($"{nameof(CsvReportRenderer)} cannot render format {request.Format}.");

        ct.ThrowIfCancellationRequested();
        var report = ReportCompositionProcessor.BindJson(request.ReportDataJson, request.Parameters);
        var tables = SectionBody.CollectTables(report.Sections);
        if (tables.Count == 0)
            throw new ReportValidationException("CSV generation requires at least one table in the report.");

        // Rows are flushed to the file in chunks so peak memory tracks the chunk size rather than the whole report. A 5 MB composition can expand to several hundred thousand
        // formatted cells, and the previous shape held every one of them in a nested dictionary until the very last write.
        await using (var output = new FileStream(request.OutputFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true)) {
            var chunk = new Dictionary<int, IReadOnlyDictionary<int, string>>(RowsPerFlush);
            var rowIndex = 0;
            for (var g = 0; g < tables.Count; g++) {
                ct.ThrowIfCancellationRequested();
                var table = tables[g];
                if (g > 0) {
                    chunk[rowIndex++] = new Dictionary<int, string>();
                    chunk[rowIndex++] = new Dictionary<int, string> { [0] = string.IsNullOrWhiteSpace(table.Title) ? $"Table {g + 1}" : table.Title! };
                }

                var header = new Dictionary<int, string>();
                for (var c = 0; c < table.Columns.Count; c++)
                    header[c] = table.Columns[c].Header;

                chunk[rowIndex++] = header;
                foreach (var row in table.Rows) {
                    var cells = new Dictionary<int, string>();
                    for (var c = 0; c < table.Columns.Count; c++) {
                        var value = c < row.Cells.Count ? row.Cells[c] : null;
                        var col = table.Columns[c];
                        cells[c] = col.ValueFormatter != null ? col.ValueFormatter(value) : value?.ToString() ?? string.Empty;
                    }

                    chunk[rowIndex++] = cells;
                    if (chunk.Count < RowsPerFlush)
                        continue;

                    await FlushChunkAsync(csvService, chunk, output, ct).ConfigureAwait(false);
                    rowIndex = 0;
                }
            }

            await FlushChunkAsync(csvService, chunk, output, ct).ConfigureAwait(false);
        }

        var fileName = request.SuggestedFileName ?? "report" + ReportFormat.Csv.Extension;
        return new() {
            FilePath = request.OutputFilePath,
            ContentType = ReportFormat.Csv.ContentType,
            FileName = fileName,
            ByteLength = new FileInfo(request.OutputFilePath).Length
        };
    }

    /// <summary>
    /// Writes the buffered rows and clears the buffer. <c>hasHeaderRow</c> is true because the writer emits synthetic <c>Column0…</c> headers otherwise. With it set, the chunk's
    /// first row is written as-is, which is what every chunk needs.
    /// </summary>
    private static async Task FlushChunkAsync(ICsvService csvService, Dictionary<int, IReadOnlyDictionary<int, string>> chunk, Stream output, CancellationToken ct)
    {
        if (chunk.Count == 0)
            return;

        await csvService.ExportToCsvStreamFromDictionaryAsync(chunk, output, true, false, ct).ConfigureAwait(false);
        chunk.Clear();
    }
}