using System.Text;
using System.Text.RegularExpressions;
using Lyo.Common;
using Lyo.Common.Extensions;
using Lyo.Common.Records;
using Lyo.DataTable.Models;
using Lyo.Exceptions;
using Lyo.Metrics;
using Lyo.Pdf.Internal;
using Lyo.Pdf.Models;
using Lyo.Result;
using UglyToad.PdfPig.Annotations;
using UglyToad.PdfPig.Tokens;
using PigDoc = UglyToad.PdfPig.PdfDocument;

namespace Lyo.Pdf.Documents;

public sealed class PdfTextExtractor : ITextExtractor
{
    private readonly PdfReader _owner;
    private readonly IMetrics _metrics;
    private readonly PdfServiceOptions _options;

    internal PdfTextExtractor(PdfReader owner)
    {
        ArgumentHelpers.ThrowIfNull(owner);
        _owner = owner;
        _options = owner.Options;
        _metrics = owner.Metrics;
    }

        public Task<IReadOnlyList<PdfWord>> GetWordsAsync(int? page = null, CancellationToken ct = default)
            => Task.Run(() => GetWordsInternal(_owner, page), ct);

        public IReadOnlyList<PdfWord> GetWords(int? page = null) => GetWordsInternal(_owner, page);

        public Task<IReadOnlyList<PdfTextLine>> GetLinesAsync(int? page = null, double? yTolerance = null, CancellationToken ct = default)
            => Task.Run(() => GetLinesInternal(_owner, page, yTolerance), ct);

        public IReadOnlyList<PdfTextLine> GetLines(int? page = null, double? yTolerance = null) => GetLinesInternal(_owner, page, yTolerance);

        public Task<IReadOnlyList<PdfWord>> GetWordsBetweenAsync(string? startText = null, string? endText = null, int? page = null, CancellationToken ct = default)
            => Task.Run(() => GetWordsBetweenInternal(_owner, startText, endText, page), ct);

        public IReadOnlyList<PdfWord> GetWordsBetween(string? startText = null, string? endText = null, int? page = null)
            => GetWordsBetweenInternal(_owner, startText, endText, page);

        public Task<IReadOnlyList<PdfTextLine>> GetLinesBetweenAsync(
            string? startText = null,
            string? endText = null,
            int? page = null,
            double? yTolerance = null,
            CancellationToken ct = default)
            => Task.Run(() => GetLinesBetweenInternal(_owner, startText, endText, page, yTolerance), ct);

        public IReadOnlyList<PdfTextLine> GetLinesBetween(string? startText = null, string? endText = null, int? page = null, double? yTolerance = null)
            => GetLinesBetweenInternal(_owner, startText, endText, page, yTolerance);

        public Task<IReadOnlyList<PdfTextLine>> GetLinesInBoundingBoxAsync(PdfBoundingBox region, double? yTolerance = null, CancellationToken ct = default)
            => Task.Run(() => GetLinesInBoundingBoxInternal(_owner, region, yTolerance), ct);

        public IReadOnlyList<PdfTextLine> GetLinesInBoundingBox(PdfBoundingBox region, double? yTolerance = null)
            => GetLinesInBoundingBoxInternal(_owner, region, yTolerance);

        public Task<PdfColumnarText> GetColumnarTextInBoundingBoxAsync(
            PdfBoundingBox region,
            int columnCount,
            double? yTolerance = null,
            CancellationToken ct = default)
            => Task.Run(() => GetColumnarTextInBoundingBox(region, columnCount, yTolerance), ct);

        public PdfColumnarText GetColumnarTextInBoundingBox(PdfBoundingBox region, int columnCount, double? yTolerance = null)
        {
            var lines = GetLinesInBoundingBoxInternal(_owner, region, yTolerance);
            var words = lines.SelectMany(l => l.Words).ToList();
            return BuildColumnarText(words, columnCount, yTolerance);
        }

        public async Task<PdfColumnarText> GetColumnarTextAsync(IReadOnlyList<PdfWord> words, int columnCount, double? yTolerance = null, CancellationToken ct = default)
            => await Task.Run(() => GetColumnarText(words, columnCount, yTolerance), ct).ConfigureAwait(false);

        public PdfColumnarText GetColumnarText(IReadOnlyList<PdfWord> words, int columnCount, double? yTolerance = null)
        {
            ArgumentHelpers.ThrowIfNull(words);
            return BuildColumnarText(words.ToList(), columnCount, yTolerance);
        }

        public async Task<IReadOnlyList<KvColumnResult>> ExtractKeyValuePairsAsync(
            IEnumerable<string> knownKeys,
            int? page = null,
            double yTolerance = 5.0,
            PdfKeyValueLayout keyValueLayout = PdfKeyValueLayout.Horizontal,
            int keyValueColumnCount = 1,
            CancellationToken ct = default)
            => await Task.Run(() => ExtractKeyValuePairsInternal(_owner, knownKeys, page, yTolerance, keyValueLayout, keyValueColumnCount), ct).ConfigureAwait(false);

        public IReadOnlyList<KvColumnResult> ExtractKeyValuePairs(
            IEnumerable<string> knownKeys,
            int? page = null,
            double yTolerance = 5.0,
            PdfKeyValueLayout keyValueLayout = PdfKeyValueLayout.Horizontal,
            int keyValueColumnCount = 1)
            => ExtractKeyValuePairsInternal(_owner, knownKeys, page, yTolerance, keyValueLayout, keyValueColumnCount);

        public async Task<IReadOnlyList<KvColumnResult>> ExtractKeyValuePairsAsync(
            IReadOnlyList<PdfWord> words,
            IEnumerable<string> knownKeys,
            double yTolerance = 5.0,
            PdfKeyValueLayout keyValueLayout = PdfKeyValueLayout.Horizontal,
            int keyValueColumnCount = 1,
            CancellationToken ct = default)
            => await Task.Run(() => ExtractKeyValuePairs(words, knownKeys, yTolerance, keyValueLayout, keyValueColumnCount), ct).ConfigureAwait(false);

        public IReadOnlyList<KvColumnResult> ExtractKeyValuePairs(
            IReadOnlyList<PdfWord> words,
            IEnumerable<string> knownKeys,
            double yTolerance = 5.0,
            PdfKeyValueLayout keyValueLayout = PdfKeyValueLayout.Horizontal,
            int keyValueColumnCount = 1)
            => PdfOperationMetrics.Execute(_metrics,
                Constants.Metrics.ExtractKeyValueDuration, Constants.Metrics.ExtractKeyValueSuccess, Constants.Metrics.ExtractKeyValueFailure, () => {
                    ArgumentHelpers.ThrowIfNull(words);
                    ArgumentHelpers.ThrowIfNull(knownKeys);
                    return ExtractKeyValueColumns(words.ToList(), knownKeys, yTolerance, keyValueLayout, keyValueColumnCount);
                });

        public async Task<IReadOnlyList<IReadOnlyDictionary<string, string?>>> ExtractTableAsync(
            ColumnHeader[] headers,
            int? page = null,
            double yTolerance = 5.0,
            CancellationToken ct = default)
            => await Task.Run(() => ExtractTableInternal(_owner, headers, page, yTolerance), ct).ConfigureAwait(false);

        public IReadOnlyList<IReadOnlyDictionary<string, string?>> ExtractTable(ColumnHeader[] headers, int? page = null, double yTolerance = 5.0)
            => ExtractTableInternal(_owner, headers, page, yTolerance);

        public async Task<IReadOnlyList<IReadOnlyDictionary<string, string?>>> ExtractTableAsync(
            IReadOnlyList<PdfWord> words,
            ColumnHeader[] headers,
            double yTolerance = 5.0,
            PdfInferFormattingFlags? inferFormattingForHeaderRows = null,
            CancellationToken ct = default)
            => await Task.Run(() => ExtractTable(words, headers, yTolerance, inferFormattingForHeaderRows), ct).ConfigureAwait(false);

        public IReadOnlyList<IReadOnlyDictionary<string, string?>> ExtractTable(
            IReadOnlyList<PdfWord> words,
            ColumnHeader[] headers,
            double yTolerance = 5.0,
            PdfInferFormattingFlags? inferFormattingForHeaderRows = null)
            => PdfOperationMetrics.Execute(_metrics,
                Constants.Metrics.ExtractTableDuration, Constants.Metrics.ExtractTableSuccess, Constants.Metrics.ExtractTableFailure, () => {
                    ArgumentHelpers.ThrowIfNull(words);
                    ArgumentHelpers.ThrowIfNull(headers);
                    return ExtractTableFromWords(words.ToList(), headers, yTolerance, inferFormattingForHeaderRows);
                });

        public IReadOnlyDictionary<string, string?> InferKeyValuePairsFromFormatting(
            IReadOnlyList<PdfWord> words,
            double yTolerance = 5.0,
            int columnCount = 1,
            PdfInferFormattingFlags inferFlags = PdfInferFormattingFlags.Bold | PdfInferFormattingFlags.Semicolon | PdfInferFormattingFlags.Underline,
            IReadOnlyList<char>? keyValueDelimiters = null)
        {
            ArgumentHelpers.ThrowIfNull(words);
            if (words.Count == 0 || inferFlags == PdfInferFormattingFlags.None)
                return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            var delimChars = PdfInferenceParsing.NormalizeKeyValueDelimiters(keyValueDelimiters);
            var list = words.ToList();
            var bands = PdfInferenceParsing.ClampKvColumnBandCount(columnCount);
            if (bands <= 1)
                return InferKeyValuePairsFromFormattingInternal(list, yTolerance, inferFlags, delimChars);

            var columns = BandWordsIntoVerticalColumns(list, bands);
            var merged = new List<KvColumnResult>(columns.Count);
            for (var i = 0; i < columns.Count; i++) {
                var dict = InferKeyValuePairsFromFormattingInternal(columns[i], yTolerance, inferFlags, delimChars);
                merged.Add(new(i, dict));
            }

            return KvColumnResult.Merge(merged);
        }

        public ColumnHeader[] InferTableHeadersFromFormatting(
            IReadOnlyList<PdfWord> words,
            double? yTolerance = null,
            PdfInferFormattingFlags inferFlags = PdfInferFormattingFlags.Bold | PdfInferFormattingFlags.Semicolon | PdfInferFormattingFlags.Underline,
            IReadOnlyList<char>? keyValueDelimiters = null)
        {
            ArgumentHelpers.ThrowIfNull(words);
            var delimChars = PdfInferenceParsing.NormalizeKeyValueDelimiters(keyValueDelimiters);
            return InferTableHeadersFromFormattingInternal(words.ToList(), yTolerance, inferFlags, delimChars);
        }

        public Result<DataTable.Models.DataTable> ParseBytesAsDataTable(byte[] pdfBytes, ColumnHeader[] headers, int? page = null, double yTolerance = 5.0)
        {
            ArgumentHelpers.ThrowIfNull(pdfBytes);
            ArgumentHelpers.ThrowIfNull(headers);
            if (headers.Length == 0)
                return Result<DataTable.Models.DataTable>.Failure("At least one column header is required.", "PdfTabular.NoHeaders");

            try {
                using var doc = PdfReader.OpenTransient(_owner.LoggerFactory, _owner.Options, _owner.Metrics, pdfBytes);
                var (headerCells, formattedRows) = ExtractTableFormattedInternal(doc, headers, page, yTolerance);
                return Result<DataTable.Models.DataTable>.Success(RowsToDataTable(headers, formattedRows, headerCells));
            }
            catch (Exception ex) {
                return Result<DataTable.Models.DataTable>.Failure(ex);
            }
        }

        public async Task<Result<DataTable.Models.DataTable>> ParseBytesAsDataTableAsync(
            byte[] pdfBytes,
            ColumnHeader[] headers,
            int? page = null,
            double yTolerance = 5.0,
            CancellationToken ct = default)
            => await Task.Run(() => ParseBytesAsDataTable(pdfBytes, headers, page, yTolerance), ct).ConfigureAwait(false);

        public DataTable.Models.DataTable ExtractDataTable(ColumnHeader[] headers, int? page = null, double yTolerance = 5.0)
        {
            ArgumentHelpers.ThrowIfNull(headers);
            var (headerCells, formattedRows) = ExtractTableFormattedInternal(_owner, headers, page, yTolerance);
            return RowsToDataTable(headers, formattedRows, headerCells);
        }

        public DataTable.Models.DataTable ExtractDataTable(IReadOnlyList<PdfWord> words, ColumnHeader[] headers, double yTolerance = 5.0)
        {
            ArgumentHelpers.ThrowIfNull(words);
            ArgumentHelpers.ThrowIfNull(headers);
            var (headerCells, formattedRows) = ExtractTableFromWordsFormatted(words.ToList(), headers, yTolerance);
            return RowsToDataTable(headers, formattedRows, headerCells);
        }

        public async Task<DataTable.Models.DataTable> ExtractDataTableAsync(
            ColumnHeader[] headers,
            int? page = null,
            double yTolerance = 5.0,
            CancellationToken ct = default)
            => await Task.Run(() => ExtractDataTable(headers, page, yTolerance), ct).ConfigureAwait(false);

        public async Task<DataTable.Models.DataTable> ExtractDataTableAsync(
            IReadOnlyList<PdfWord> words,
            ColumnHeader[] headers,
            double yTolerance = 5.0,
            CancellationToken ct = default)
            => await Task.Run(() => ExtractDataTable(words, headers, yTolerance), ct).ConfigureAwait(false);

        public IReadOnlyList<PdfWord> GetWordsBetweenSections(
            string startSection,
            IEnumerable<string> sectionsInOrder,
            string? defaultEndSection = null,
            int? startPage = null,
            int? endPage = null)
        {
            ArgumentHelpers.ThrowIfNull(startSection);
            ArgumentHelpers.ThrowIfNull(sectionsInOrder);
            var sections = sectionsInOrder.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();
            var startIdx = sections.FindIndex(s => string.Equals(s, startSection.Trim(), StringComparison.OrdinalIgnoreCase));
            var endCandidates = startIdx >= 0 && startIdx < sections.Count - 1 ? sections.Skip(startIdx + 1).ToArray() : [];
            var pageCount = _owner.GetInfo().PageCount;
            var firstPage = startPage ?? 1;
            var lastPage = endPage ?? pageCount;
            firstPage = Math.Max(1, firstPage);
            lastPage = Math.Min(pageCount, lastPage);
            var result = new List<PdfWord>();
            for (var pageNum = firstPage; pageNum <= lastPage; pageNum++) {
                var pageLines = GetLinesInternal(_owner, pageNum, null);
                var sectionStartIdx = -1;
                for (var i = 0; i < pageLines.Count; i++) {
                    if (!pageLines[i].Text.Trim().StartsWith(startSection.Trim(), StringComparison.OrdinalIgnoreCase))
                        continue;

                    sectionStartIdx = i;
                    break;
                }

                if (sectionStartIdx < 0)
                    continue;

                string? endSection = null;
                for (var i = sectionStartIdx + 1; i < pageLines.Count; i++) {
                    var lineText = pageLines[i].Text.Trim();
                    var found = endCandidates.FirstOrDefault(s => lineText.StartsWith(s, StringComparison.OrdinalIgnoreCase));
                    if (found == null)
                        continue;

                    endSection = found;
                    break;
                }

                endSection ??= defaultEndSection;
                var pageWords = GetWordsBetweenInternal(_owner, startSection, endSection, pageNum);
                result.AddRange(pageWords);
            }

            return result;
        }

        public IReadOnlyList<PdfTextLine> GetLinesBetweenSections(
            string startSection,
            IEnumerable<string> sectionsInOrder,
            string? defaultEndSection = null,
            int? startPage = null,
            int? endPage = null,
            double? yTolerance = null)
        {
            ArgumentHelpers.ThrowIfNull(startSection);
            ArgumentHelpers.ThrowIfNull(sectionsInOrder);
            var sections = sectionsInOrder.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();
            var startIdx = sections.FindIndex(s => string.Equals(s, startSection.Trim(), StringComparison.OrdinalIgnoreCase));
            var endCandidates = startIdx >= 0 && startIdx < sections.Count - 1 ? sections.Skip(startIdx + 1).ToArray() : [];
            var pageCount = _owner.GetInfo().PageCount;
            var firstPage = startPage ?? 1;
            var lastPage = endPage ?? pageCount;
            firstPage = Math.Max(1, firstPage);
            lastPage = Math.Min(pageCount, lastPage);
            var result = new List<PdfTextLine>();
            for (var pageNum = firstPage; pageNum <= lastPage; pageNum++) {
                var pageLines = GetLinesInternal(_owner, pageNum, yTolerance);
                var sectionStartIdx = -1;
                for (var i = 0; i < pageLines.Count; i++) {
                    if (!pageLines[i].Text.Trim().StartsWith(startSection.Trim(), StringComparison.OrdinalIgnoreCase))
                        continue;

                    sectionStartIdx = i;
                    break;
                }

                if (sectionStartIdx < 0)
                    continue;

                string? endSection = null;
                for (var i = sectionStartIdx + 1; i < pageLines.Count; i++) {
                    var lineText = pageLines[i].Text.Trim();
                    var found = endCandidates.FirstOrDefault(s => lineText.StartsWith(s, StringComparison.OrdinalIgnoreCase));
                    if (found == null)
                        continue;

                    endSection = found;
                    break;
                }

                endSection ??= defaultEndSection;
                var sectionLines = GetLinesBetweenInternal(_owner, startSection, endSection, pageNum, yTolerance);
                result.AddRange(sectionLines);
            }

            return result;
        }

        public Task<IReadOnlyList<PdfTextLine>> GetLinesBetweenSectionsAsync(
            string startSection,
            IEnumerable<string> sectionsInOrder,
            string? defaultEndSection = null,
            int? startPage = null,
            int? endPage = null,
            double? yTolerance = null,
            CancellationToken ct = default)
            => Task.Run(() => GetLinesBetweenSections(startSection, sectionsInOrder, defaultEndSection, startPage, endPage, yTolerance), ct);

        public PdfSection? GetSection(
            string startSection,
            IEnumerable<string> sectionsInOrder,
            string? defaultEndSection = null,
            int? startPage = null,
            int? endPage = null,
            double? yTolerance = null)
        {
            ArgumentHelpers.ThrowIfNull(startSection);
            ArgumentHelpers.ThrowIfNull(sectionsInOrder);
            var sections = sectionsInOrder.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();
            var startIdx = sections.FindIndex(s => string.Equals(s, startSection.Trim(), StringComparison.OrdinalIgnoreCase));
            var endCandidates = startIdx >= 0 && startIdx < sections.Count - 1 ? sections.Skip(startIdx + 1).ToArray() : [];
            var pageCount = _owner.GetInfo().PageCount;
            var firstPage = startPage ?? 1;
            var lastPage = endPage ?? pageCount;
            firstPage = Math.Max(1, firstPage);
            lastPage = Math.Min(pageCount, lastPage);
            var result = new List<PdfTextLine>();
            var firstPageFound = -1;
            var lastPageFound = -1;
            for (var pageNum = firstPage; pageNum <= lastPage; pageNum++) {
                var pageLines = GetLinesInternal(_owner, pageNum, yTolerance);
                var sectionStartIdx = -1;
                for (var i = 0; i < pageLines.Count; i++) {
                    if (!pageLines[i].Text.Trim().StartsWith(startSection.Trim(), StringComparison.OrdinalIgnoreCase))
                        continue;

                    sectionStartIdx = i;
                    break;
                }

                if (sectionStartIdx < 0)
                    continue;

                if (firstPageFound < 0)
                    firstPageFound = pageNum;

                lastPageFound = pageNum;
                string? endSection = null;
                for (var i = sectionStartIdx + 1; i < pageLines.Count; i++) {
                    var lineText = pageLines[i].Text.Trim();
                    var found = endCandidates.FirstOrDefault(s => lineText.StartsWith(s, StringComparison.OrdinalIgnoreCase));
                    if (found == null)
                        continue;

                    endSection = found;
                    break;
                }

                endSection ??= defaultEndSection;
                var sectionLines = GetLinesBetweenInternal(_owner, startSection, endSection, pageNum, yTolerance);
                result.AddRange(sectionLines);
            }

            return firstPageFound < 0 ? null : new(startSection.Trim(), firstPageFound, lastPageFound, result);
        }

        public Task<PdfSection?> GetSectionAsync(
            string startSection,
            IEnumerable<string> sectionsInOrder,
            string? defaultEndSection = null,
            int? startPage = null,
            int? endPage = null,
            double? yTolerance = null,
            CancellationToken ct = default)
            => Task.Run(() => GetSection(startSection, sectionsInOrder, defaultEndSection, startPage, endPage, yTolerance), ct);

        private static DataTable.Models.DataTable RowsToDataTable(ColumnHeader[] headers, IReadOnlyList<IReadOnlyDictionary<string, string?>> rows)
        {
            var dt = new DataTable.Models.DataTable();
            for (var i = 0; i < headers.Length; i++)
                dt.SetHeader(i, DataTableCell.FromValue(headers[i].Label));

            for (var r = 0; r < rows.Count; r++) {
                var row = rows[r];
                var dataRow = dt.AddRow();
                for (var c = 0; c < headers.Length; c++) {
                    var val = row.TryGetValue(headers[c].Label, out var v) ? v : null;
                    dataRow.SetCell(c, DataTableCell.FromValue(val ?? ""));
                }
            }

            return dt;
        }

        private static DataTable.Models.DataTable RowsToDataTable(
            ColumnHeader[] headers,
            IReadOnlyList<IReadOnlyDictionary<string, IDataTableCell>> formattedRows,
            IReadOnlyList<IDataTableCell>? formattedHeaderCells = null)
        {
            var dt = new DataTable.Models.DataTable();
            for (var i = 0; i < headers.Length; i++) {
                var headerCell = formattedHeaderCells != null && i < formattedHeaderCells.Count ? formattedHeaderCells[i] : DataTableCell.FromValue(headers[i].Label);
                dt.SetHeader(i, headerCell);
            }

            for (var r = 0; r < formattedRows.Count; r++) {
                var row = formattedRows[r];
                var dataRow = dt.AddRow();
                for (var c = 0; c < headers.Length; c++) {
                    var cell = row.TryGetValue(headers[c].Label, out var v) ? v : DataTableCell.FromValue("");
                    dataRow.SetCell(c, cell);
                }
            }

            return dt;
        }
        private IReadOnlyList<PdfWord> GetWordsInternal(PdfReader pdfRead, int? page)
            => PdfOperationMetrics.Execute(_metrics,
                Constants.Metrics.WordsDuration, Constants.Metrics.WordsSuccess, Constants.Metrics.WordsFailure, () => pdfRead.WithPdf(
                    pdfDoc => {
                        var document = pdfDoc;
                        if (page.HasValue) {
                            ArgumentHelpers.ThrowIfNotInRange(page.Value, 1, document.NumberOfPages, nameof(page), $"Page number must be between 1 and {document.NumberOfPages}.");
                            var pdfPage = document.GetPage(page.Value);
                            return pdfPage.GetWords().ToPdfWords(pdfPage.Paths);
                        }

                        var allWords = new List<PdfWord>();
                        for (var i = 1; i <= document.NumberOfPages; i++) {
                            var pdfPage = document.GetPage(i);
                            allWords.AddRange(pdfPage.GetWords().ToPdfWords(pdfPage.Paths));
                        }

                        return allWords;
                    }));

        private IReadOnlyList<PdfTextLine> GetLinesInternal(PdfReader pdfRead, int? page, double? yTolerance)
            => PdfOperationMetrics.Execute(_metrics,
                Constants.Metrics.LinesDuration, Constants.Metrics.LinesSuccess, Constants.Metrics.LinesFailure, () => pdfRead.WithPdf(
                    pdfDoc => {
                        var document = pdfDoc;
                        var tolerance = yTolerance ?? _options.DefaultYTolerance;
                        if (page.HasValue) {
                            ArgumentHelpers.ThrowIfNotInRange(page.Value, 1, document.NumberOfPages, nameof(page), $"Page number must be between 1 and {document.NumberOfPages}.");
                            var pdfPage = document.GetPage(page.Value);
                            return GroupIntoLines(pdfPage.GetWords().ToPdfWords(pdfPage.Paths), tolerance);
                        }

                        var lines = new List<PdfTextLine>();
                        for (var i = 1; i <= document.NumberOfPages; i++) {
                            var pdfPage = document.GetPage(i);
                            lines.AddRange(GroupIntoLines(pdfPage.GetWords().ToPdfWords(pdfPage.Paths), tolerance).OrderByDescending(l => l.Y));
                        }

                        return lines;
                    }));

        private List<PdfTextLine> GetDocumentLines(PigDoc document, double yTolerance, int? page = null)
        {
            var lines = new List<PdfTextLine>();
            var startPage = page ?? 1;
            var endPage = page ?? document.NumberOfPages;
            if (page.HasValue)
                ArgumentHelpers.ThrowIfNotInRange(page.Value, 1, document.NumberOfPages, nameof(page), $"Page number must be between 1 and {document.NumberOfPages}.");

            for (var pageNumber = startPage; pageNumber <= endPage; pageNumber++) {
                var pdfPage = document.GetPage(pageNumber);
                lines.AddRange(GroupIntoLines(pdfPage.GetWords().ToPdfWords(pdfPage.Paths), yTolerance).OrderByDescending(l => l.Y));
            }

            return lines;
        }

        private IReadOnlyList<PdfWord> GetWordsBetweenInternal(PdfReader pdfRead, string? startText, string? endText, int? page)
            => PdfOperationMetrics.Execute(_metrics,
                Constants.Metrics.WordsBetweenDuration, Constants.Metrics.WordsBetweenSuccess, Constants.Metrics.WordsBetweenFailure, () => {
                    var tolerance = _options.DefaultYTolerance;
                    var lines = pdfRead.WithPdf(d => GetDocumentLines(d, tolerance, page));
                    var sectionLines = FindSectionLines(lines, startText, endText);
                    return sectionLines.SelectMany(l => l.Words.OrderBy(w => w.BoundingBox.Left)).ToList();
                });

        private IReadOnlyList<PdfTextLine> GetLinesBetweenInternal(PdfReader pdfRead, string? startText, string? endText, int? page, double? yTolerance)
            => PdfOperationMetrics.Execute(_metrics,
                Constants.Metrics.LinesBetweenDuration, Constants.Metrics.LinesBetweenSuccess, Constants.Metrics.LinesBetweenFailure, () => {
                    var tolerance = yTolerance ?? _options.DefaultYTolerance;
                    var lines = pdfRead.WithPdf(d => GetDocumentLines(d, tolerance, page));
                    return FindSectionLines(lines, startText, endText);
                });

        private IReadOnlyList<PdfTextLine> GetLinesInBoundingBoxInternal(PdfReader pdfRead, PdfBoundingBox region, double? yTolerance)
            => PdfOperationMetrics.Execute(_metrics,
                Constants.Metrics.LinesDuration, Constants.Metrics.LinesSuccess, Constants.Metrics.LinesFailure, () => pdfRead.WithPdf(
                    pdfDoc => {
                        var document = pdfDoc;
                        ArgumentHelpers.ThrowIfNotInRange(region.Page, 1, document.NumberOfPages, nameof(region), $"Page number must be between 1 and {document.NumberOfPages}.");
                        var pdfPage = document.GetPage(region.Page);
                        var overlapThreshold = Math.Max(0, Math.Min(1, _options.BoundingBoxOverlapThreshold));

                        // Page-content words: apply overlap threshold (excludes words that barely touch the region)
                        var pageWords = pdfPage.GetWords().ToPdfWords(pdfPage.Paths).ToList();
                        var filteredPageWords = pageWords.Where(w => w.BoundingBox.OverlapRatio(region.Box) >= overlapThreshold).ToList();

                        // Annotation text: use Intersects only (form fields have large rects; user's box may be smaller and fully inside)
                        var words = new List<PdfWord>(filteredPageWords);
                        foreach (var ann in pdfPage.GetAnnotations()) {
                            var text = GetAnnotationText(ann);
                            if (text.IsNullOrEmpty())
                                continue;

                            var rect = ann.Rectangle;
                            var annBox = new BoundingBox2D(rect.Left, rect.Right, rect.Top, rect.Bottom);
                            if (!annBox.Intersects(region.Box))
                                continue;

                            var format = GetAnnotationFormat(ann);
                            words.Add(new(text.Trim(), annBox, format));
                        }

                        var tolerance = yTolerance ?? _options.DefaultYTolerance;
                        words = DedupeOverlappingDuplicateText(words);
                        return GroupIntoLines(words, tolerance);
                    }));

        /// <summary>When the same text appears twice (drawn text + AcroForm widget), keep the tighter bounding box and drop the duplicate.</summary>
        private static List<PdfWord> DedupeOverlappingDuplicateText(IReadOnlyList<PdfWord> words)
        {
            var candidates = words.Where(w => !string.IsNullOrWhiteSpace(w.Text)).OrderBy(w => w.BoundingBox.Width * w.BoundingBox.Height).ToList();
            if (candidates.Count <= 1)
                return candidates;

            var kept = new List<PdfWord>(candidates.Count);
            foreach (var w in candidates) {
                var tw = DedupeNormalizeText(w.Text);
                if (tw.Length == 0) {
                    kept.Add(w);
                    continue;
                }

                var duplicate = false;
                foreach (var k in kept) {
                    if (!string.Equals(DedupeNormalizeText(k.Text), tw, StringComparison.OrdinalIgnoreCase))
                        continue;

                    var o1 = k.BoundingBox.OverlapRatio(w.BoundingBox);
                    var o2 = w.BoundingBox.OverlapRatio(k.BoundingBox);
                    if (o1 > 0.35 || o2 > 0.35) {
                        duplicate = true;
                        break;
                    }
                }

                if (!duplicate)
                    kept.Add(w);
            }

            return kept;
        }

        private static string DedupeNormalizeText(string text) => text.Trim().Replace('\u00a0', ' ');

        private Dictionary<string, string?> InferKeyValuePairsFromFormattingInternal(
            List<PdfWord> words,
            double yTolerance,
            PdfInferFormattingFlags inferFlags,
            char[] keyValueDelimiters)
        {
            var tolerance = yTolerance > 0 ? yTolerance : _options.DefaultYTolerance;
            var lines = GroupIntoLines(words, tolerance).OrderByDescending(l => l.Y).ToList();
            var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            if (inferFlags == PdfInferFormattingFlags.None)
                return result;

            var useDelimiters = (inferFlags & PdfInferFormattingFlags.Semicolon) != 0;
            var useFontEmphasis = (inferFlags & (PdfInferFormattingFlags.Bold | PdfInferFormattingFlags.Underline)) != 0;
            string? pendingKey = null;
            var valueParts = new List<string>();

            void FlushPending()
            {
                if (string.IsNullOrWhiteSpace(pendingKey))
                    return;

                var key = PdfInferenceParsing.CanonicalInferredKey(pendingKey!, keyValueDelimiters);
                pendingKey = null;
                var val = string.Join(" ", valueParts).Trim();
                valueParts.Clear();
                if (string.IsNullOrEmpty(key))
                    return;

                if (result.TryGetValue(key, out var existing) && !string.IsNullOrWhiteSpace(existing))
                    result[key] = string.IsNullOrWhiteSpace(val) ? existing : existing + " " + val;
                else
                    result[key] = string.IsNullOrWhiteSpace(val) ? null : val;
            }

            foreach (var line in lines) {
                var ws = line.Words.OrderBy(w => w.BoundingBox.Left).ToList();
                if (ws.Count == 0)
                    continue;

                var lineText = string.Join(" ", ws.Select(w => w.Text)).Trim();
                if (useDelimiters && PdfInferenceParsing.TryParseDelimiterKeyValueLine(lineText, keyValueDelimiters, out var delimKey, out var delimVal, out var delimLabelOnly)) {
                    FlushPending();
                    if (delimLabelOnly) {
                        pendingKey = delimKey;
                        continue;
                    }

                    var ckey = PdfInferenceParsing.CanonicalInferredKey(delimKey!, keyValueDelimiters);
                    if (!string.IsNullOrEmpty(ckey)) {
                        if (result.TryGetValue(ckey, out var ex) && !string.IsNullOrWhiteSpace(ex))
                            result[ckey] = string.IsNullOrWhiteSpace(delimVal) ? ex : ex + " " + delimVal;
                        else
                            result[ckey] = string.IsNullOrWhiteSpace(delimVal) ? null : delimVal;
                    }

                    continue;
                }

                if (!useFontEmphasis) {
                    if (pendingKey != null)
                        valueParts.Add(lineText);

                    continue;
                }

                var allE = ws.All(w => PdfFontStyleInference.IsInferEmphasizedForFlags(w, inferFlags));
                var anyE = ws.Any(w => PdfFontStyleInference.IsInferEmphasizedForFlags(w, inferFlags));
                if (allE) {
                    if (LooksLikeSectionHeader(lineText)) {
                        FlushPending();
                        continue;
                    }

                    FlushPending();
                    pendingKey = lineText;
                    continue;
                }

                if (anyE && !allE) {
                    FlushPending();
                    var i = 0;
                    while (i < ws.Count && PdfFontStyleInference.IsInferEmphasizedForFlags(ws[i], inferFlags))
                        i++;

                    if (i == 0) {
                        if (pendingKey != null)
                            valueParts.Add(lineText);

                        continue;
                    }

                    var keyText = string.Join(" ", ws.Take(i).Select(w => w.Text)).Trim();
                    var rest = string.Join(" ", ws.Skip(i).Select(w => w.Text)).Trim();
                    var ck = PdfInferenceParsing.CanonicalInferredKey(keyText, keyValueDelimiters);
                    if (!string.IsNullOrEmpty(ck)) {
                        if (result.TryGetValue(ck, out var ex) && !string.IsNullOrWhiteSpace(ex))
                            result[ck] = string.IsNullOrWhiteSpace(rest) ? ex : ex + " " + rest;
                        else
                            result[ck] = string.IsNullOrWhiteSpace(rest) ? null : rest;
                    }

                    continue;
                }

                if (pendingKey != null)
                    valueParts.Add(lineText);
            }

            FlushPending();
            return result;
        }

        private ColumnHeader[] InferTableHeadersFromFormattingInternal(List<PdfWord> words, double? yTolerance, PdfInferFormattingFlags inferFlags, char[] keyValueDelimiters)
        {
            if (inferFlags == PdfInferFormattingFlags.None)
                return [];

            var tolerance = yTolerance ?? _options.DefaultYTolerance;
            var lines = GroupIntoLines(words, tolerance).OrderByDescending(l => l.Y).ToList();
            if (lines.Count == 0)
                return [];

            var useFontEmphasis = (inferFlags & (PdfInferFormattingFlags.Bold | PdfInferFormattingFlags.Underline)) != 0;
            var useDelimiters = (inferFlags & PdfInferFormattingFlags.Semicolon) != 0;
            var delimSpan = keyValueDelimiters.AsSpan();
            var bestIdx = 0;
            PdfTextLine? bestLine = null;
            if (useFontEmphasis) {
                var scan = Math.Min(8, lines.Count);
                var scored = new List<(int Idx, double Score)>();
                for (var li = 0; li < scan; li++) {
                    var line = lines[li];
                    var ws = line.Words;
                    if (ws.Count < 2)
                        continue;

                    // Skip lines that don't match the selected inference styling (e.g. plain body rows when inferring underline).
                    if (PdfInferenceParsing.LineHasNegligibleInferenceEmphasis(line, inferFlags))
                        continue;

                    var emphasized = ws.Count(w => PdfFontStyleInference.IsInferEmphasizedForFlags(w, inferFlags));
                    var ratio = (double)emphasized / ws.Count;
                    var avgSize = ws.Where(w => w.Format?.FontSize is > 0).Select(w => w.Format!.FontSize!.Value).DefaultIfEmpty(0).Average();
                    var score = ratio * 3.0 + avgSize * 0.05;
                    scored.Add((li, score));
                }

                if (scored.Count > 0) {
                    var maxScore = scored.Max(s => s.Score);
                    // Among similar scores, prefer the topmost line (smallest index — lines are sorted by descending Y).
                    const double tieEps = 0.08;
                    var best = scored.Where(s => s.Score >= maxScore - tieEps).OrderBy(s => s.Idx).First();
                    bestIdx = best.Idx;
                    bestLine = lines[bestIdx];
                }
            }

            if (bestLine == null) {
                bestLine = lines[0];
                bestIdx = 0;
            }

            // Lookahead: include consecutive lines while each still looks like part of the styled header (spacing + formatting).
            var mergeTh = _options.TableHeaderMergeThreshold;
            var blockIndices = new List<int> { bestIdx };
            var i = bestIdx;
            while (i + 1 < lines.Count && lines[i].Y - lines[i + 1].Y <= mergeTh && PdfInferenceParsing.LineQualifiesForHeaderBlockExtension(lines[i], lines[i + 1], inferFlags)) {
                i++;
                blockIndices.Add(i);
            }

            i = bestIdx;
            while (i > 0 && lines[i - 1].Y - lines[i].Y <= mergeTh && PdfInferenceParsing.LineQualifiesForHeaderBlockExtension(lines[i - 1], lines[i], inferFlags)) {
                i--;
                blockIndices.Insert(0, i);
            }

            blockIndices.Sort();
            var blockLines = blockIndices.Select(idx => lines[idx]).ToList();
            var joinedBlock = string.Join(" ", blockLines.Select(l => string.Join(" ", l.Words.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text)))).Trim();
            var joinedBestSingle = string.Join(" ", bestLine.Words.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text)).Trim();
            if (useDelimiters) {
                var split = PdfInferenceParsing.SplitHeaderLineByDelimiters(joinedBlock, delimSpan);
                if (split.Length >= 2)
                    return split.Select(s => new ColumnHeader(s)).ToArray();

                split = PdfInferenceParsing.SplitHeaderLineByDelimiters(joinedBestSingle, delimSpan);
                if (split.Length >= 2)
                    return split.Select(s => new ColumnHeader(s)).ToArray();
            }

            if (useFontEmphasis)
                return InferColumnHeadersFromEmphasizedBlock(blockLines);

            return [];
        }

        /// <summary>Infers one column header per horizontal band by clustering words using X gaps (not every word is a column).</summary>
        private ColumnHeader[] InferColumnHeadersFromEmphasizedBlock(IReadOnlyList<PdfTextLine> blockLines)
        {
            if (blockLines.Count == 0)
                return [];

            var anchor = blockLines.OrderByDescending(l => l.Words.Count).First();
            var xTol = _options.TableColumnXTolerance;
            var ordered = anchor.Words.OrderBy(w => w.BoundingBox.Left).ToList();
            if (ordered.Count == 0)
                return [];

            var anchorCols = ClusterWordsIntoHeaderColumns(ordered, xTol, blockLines.Count);
            if (anchorCols.Count == 0)
                return [];

            // Single header row: column boundaries come from horizontal spacing only.
            if (blockLines.Count == 1)
                return BuildColumnHeadersFromGapClusters(anchorCols);

            // Multi-line header: if the anchor line didn't split into columns, try the top physical line.
            if (anchorCols.Count == 1 && blockLines[0].Words.Count > 0) {
                var topOrdered = blockLines[0].Words.OrderBy(w => w.BoundingBox.Left).ToList();
                var topCols = ClusterWordsIntoHeaderColumns(topOrdered, xTol, 1);
                if (topCols.Count > 1)
                    anchorCols = topCols;
            }

            if (anchorCols.Count == 1) {
                var joined = string.Join(" ", anchorCols[0].Select(w => w.Text.Trim()).Where(t => !string.IsNullOrWhiteSpace(t))).Trim();
                return string.IsNullOrWhiteSpace(joined) ? [] : [new(joined)];
            }

            var columnRanges = anchorCols.Select(col => {
                    var left = col.Min(w => w.BoundingBox.Left) - xTol;
                    var right = col.Max(w => w.BoundingBox.Right) + xTol;
                    return (Left: left, Right: right);
                })
                .ToList();

            var buckets = Enumerable.Range(0, columnRanges.Count).Select(_ => new List<string>()).ToArray();
            foreach (var line in blockLines) {
                foreach (var w in line.Words.OrderBy(x => x.BoundingBox.Left)) {
                    var j = AssignWordToColumnIndex(w, columnRanges);
                    var t = w.Text.Trim();
                    if (!string.IsNullOrWhiteSpace(t))
                        buckets[j].Add(t);
                }
            }

            var headers = new List<ColumnHeader>();
            foreach (var b in buckets) {
                if (b.Count == 0)
                    continue;

                var label = string.Join(" ", b);
                if (!string.IsNullOrWhiteSpace(label))
                    headers.Add(new(label));
            }

            return headers.Count > 0 ? headers.ToArray() : [];
        }

        private List<List<PdfWord>> ClusterWordsIntoHeaderColumns(IReadOnlyList<PdfWord> ordered, double xTol, int blockLineCount)
        {
            var words = ordered as List<PdfWord> ?? ordered.ToList();
            var minGutter = ComputeAdaptiveColumnGutter(ordered, xTol);
            var cols = SplitWordsIntoColumnsByHorizontalGaps(words, minGutter);
            if (cols.Count == 1 && ordered.Count >= 3) {
                minGutter = Math.Max(4.0, minGutter * 0.35);
                cols = SplitWordsIntoColumnsByHorizontalGaps(words, minGutter);
            }

            if (cols.Count == 1 && ordered.Count >= 4 && blockLineCount > 1) {
                minGutter = Math.Max(3.0, minGutter * 0.28);
                cols = SplitWordsIntoColumnsByHorizontalGaps(words, minGutter);
            }

            return cols;
        }

        private static ColumnHeader[] BuildColumnHeadersFromGapClusters(List<List<PdfWord>> clusters)
        {
            var labels = clusters.Select(col => string.Join(" ", col.Select(w => w.Text.Trim()).Where(t => !string.IsNullOrWhiteSpace(t))).Trim()).Where(l => l.Length > 0).ToList();
            labels = MergeOrphanDtHeaderTokens(labels);
            return labels.Count == 0 ? [] : labels.Select(l => new ColumnHeader(l)).ToArray();
        }

        private static double ComputeAdaptiveColumnGutter(IReadOnlyList<PdfWord> wordsSortedLeftToRight, double toleranceFallback)
        {
            if (wordsSortedLeftToRight.Count < 2)
                return Math.Max(12.0, toleranceFallback * 2.0);

            var gaps = new List<double>();
            for (var i = 1; i < wordsSortedLeftToRight.Count; i++) {
                var g = wordsSortedLeftToRight[i].BoundingBox.Left - wordsSortedLeftToRight[i - 1].BoundingBox.Right;
                if (g > 0.5)
                    gaps.Add(g);
            }

            if (gaps.Count == 0)
                return Math.Max(12.0, toleranceFallback * 2.0);

            gaps.Sort();
            var median = gaps[gaps.Count / 2];
            return Math.Max(8.0, Math.Min(median * 2.5, toleranceFallback * 4.0));
        }

        private static List<List<PdfWord>> SplitWordsIntoColumnsByHorizontalGaps(IReadOnlyList<PdfWord> sortedLeftToRight, double minGutter)
        {
            if (sortedLeftToRight.Count == 0)
                return [];

            var cols = new List<List<PdfWord>>();
            var cur = new List<PdfWord> { sortedLeftToRight[0] };
            for (var i = 1; i < sortedLeftToRight.Count; i++) {
                var prev = sortedLeftToRight[i - 1];
                var w = sortedLeftToRight[i];
                var gap = w.BoundingBox.Left - prev.BoundingBox.Right;
                if (gap > minGutter) {
                    cols.Add(cur);
                    cur = [w];
                }
                else
                    cur.Add(w);
            }

            cols.Add(cur);
            return cols;
        }

        private static int AssignWordToColumnIndex(PdfWord w, IReadOnlyList<(double Left, double Right)> columnRanges)
        {
            var cx = (w.BoundingBox.Left + w.BoundingBox.Right) * 0.5;
            for (var j = 0; j < columnRanges.Count; j++) {
                var r = columnRanges[j];
                if (cx >= r.Left && cx <= r.Right)
                    return j;
            }

            var best = 0;
            var bestD = double.MaxValue;
            for (var j = 0; j < columnRanges.Count; j++) {
                var r = columnRanges[j];
                var center = (r.Left + r.Right) * 0.5;
                var d = Math.Abs(center - cx);
                if (d < bestD) {
                    bestD = d;
                    best = j;
                }
            }

            return best;
        }

        /// <summary>Gets text from an annotation: Content, /Contents, or /V (form field value). Handles FT=/Tx (text), FT=/Btn (checkbox).</summary>
        private static string? GetAnnotationText(Annotation ann)
        {
            if (!string.IsNullOrWhiteSpace(ann.Content))
                return ann.Content;

            var dict = ann.AnnotationDictionary;

            // /Contents - used by FreeText, Text, and other annotation types
            if (dict.TryGet(NameToken.Contents, out var contentsToken) && contentsToken is StringToken contentsSt && !string.IsNullOrWhiteSpace(contentsSt.Data))
                return contentsSt.Data;

            // /FT - field type: /Tx (text), /Btn (button/checkbox), /Ch (choice)
            var ft = GetTokenString(dict, "FT");

            // /Btn (checkbox): V = /1 or /Yes = checked, /Off = unchecked. Format: [x] Label or [] Label
            if (string.Equals(ft, "Btn", StringComparison.OrdinalIgnoreCase)) {
                var label = GetCheckboxLabel(dict);
                var vStr = GetTokenString(dict, "V");
                var isChecked = !string.IsNullOrEmpty(vStr) && !string.Equals(vStr, "Off", StringComparison.OrdinalIgnoreCase);
                var checkText = isChecked ? "[x]" : "[]";
                return string.IsNullOrWhiteSpace(label) ? checkText : $"{checkText} {label}";
            }

            // /Tx (text input): V holds the string value
            if (dict.TryGet(NameToken.Create("V"), out var valueToken)) {
                if (valueToken is StringToken valueSt && !string.IsNullOrWhiteSpace(valueSt.Data))
                    return valueSt.Data;

                if (valueToken is HexToken valueHex && !string.IsNullOrWhiteSpace(valueHex.Data))
                    return valueHex.Data;
            }

            return null;
        }

        /// <summary>Gets checkbox label: TU (tooltip) preferred; else T with internal names like c1_1[0] stripped.</summary>
        private static string? GetCheckboxLabel(DictionaryToken dict)
        {
            var tu = GetTokenString(dict, "TU");
            if (!tu.IsNullOrWhitespace())
                return tu.Trim();

            var t = GetTokenString(dict, "T");
            if (t.IsNullOrWhitespace())
                return null;

            // Strip internal field names like "c1_1[0]", "f1_08[0]" from the start
            var stripped = Regex.Replace(t.Trim(), @"^[\w\-]+\s*\[\d+\]\s*", "", RegexOptions.IgnoreCase);
            return stripped.IsNullOrWhitespace() ? null : stripped.Trim();
        }

        /// <summary>Gets string from dict key: StringToken, HexToken, or NameToken.</summary>
        private static string? GetTokenString(DictionaryToken dict, string key)
        {
            if (!dict.TryGet(NameToken.Create(key), out var token))
                return null;

            return token switch {
                StringToken st => st.Data,
                HexToken ht => ht.Data,
                NameToken nt => nt.Data,
                var _ => null
            };
        }

        /// <summary>Parses /DA (Default Appearance) to extract font, size, and color. Format: /Font size Tf [gray g | R G B rg].</summary>
        private static PdfWordFormat? GetAnnotationFormat(Annotation ann)
        {
            var da = GetDAString(ann);
            if (string.IsNullOrWhiteSpace(da))
                return null;

            string? fontName = null;
            double? fontSize = null;
            string? fontColor = null;
            var bold = false;
            var italic = false;

            // /FontName size Tf or (FontName) size Tf - font and size
            var tfMatch = Regex.Match(da, @"(?:/\s*([^\s/]+)|\(([^)]+)\))\s+([\d.]+)\s+Tf", RegexOptions.IgnoreCase);
            if (tfMatch.Success) {
                fontName = tfMatch.Groups[1].Success ? tfMatch.Groups[1].Value.Trim() : tfMatch.Groups[2].Value.Trim();
                if (double.TryParse(tfMatch.Groups[3].Value, out var size) && size > 0)
                    fontSize = size;

                bold = PdfFontStyleInference.InferBold(fontName, bold);
                italic = PdfFontStyleInference.InferItalic(fontName, italic);
            }

            // R G B rg - RGB color
            var rgMatch = Regex.Match(da, @"([\d.]+)\s+([\d.]+)\s+([\d.]+)\s+rg", RegexOptions.IgnoreCase);
            if (rgMatch.Success && double.TryParse(rgMatch.Groups[1].Value, out var r) && double.TryParse(rgMatch.Groups[2].Value, out var g) &&
                double.TryParse(rgMatch.Groups[3].Value, out var b)) {
                var rr = (byte)(Math.Max(0, Math.Min(1, r)) * 255);
                var gg = (byte)(Math.Max(0, Math.Min(1, g)) * 255);
                var bb = (byte)(Math.Max(0, Math.Min(1, b)) * 255);
                fontColor = $"#{rr:X2}{gg:X2}{bb:X2}";
            }
            else {
                // gray g - grayscale
                var gMatch = Regex.Match(da, @"([\d.]+)\s+g\b", RegexOptions.IgnoreCase);
                if (gMatch.Success && double.TryParse(gMatch.Groups[1].Value, out var gray)) {
                    var v = (byte)(Math.Max(0, Math.Min(1, gray)) * 255);
                    fontColor = $"#{v:X2}{v:X2}{v:X2}";
                }
            }

            if (fontName == null && fontSize == null && fontColor == null && !bold && !italic)
                return null;

            return new(fontSize, fontName, bold, italic, fontColor);
        }

        private static string? GetDAString(Annotation ann)
        {
            var dict = ann.AnnotationDictionary;
            if (dict.TryGet(NameToken.Create("DA"), out var daToken) && daToken is StringToken daSt && !string.IsNullOrWhiteSpace(daSt.Data))
                return daSt.Data;

            return null;
        }

        private static List<PdfTextLine> FindSectionLines(List<PdfTextLine> lines, string? startText, string? endText)
        {
            if (lines.Count == 0)
                return [];

            var startIdx = 0;
            if (!startText.IsNullOrWhitespace()) {
                var normalizedStart = startText.Trim();
                startIdx = lines.FindIndex(l => l.Text.Trim().StartsWith(normalizedStart, StringComparison.OrdinalIgnoreCase));
                if (startIdx < 0)
                    return [];
            }

            var endIdx = lines.Count;
            if (!endText.IsNullOrWhitespace()) {
                var normalizedEnd = endText.Trim();
                var found = lines.FindIndex(startIdx + 1, l => l.Text.Trim().IndexOf(normalizedEnd, StringComparison.OrdinalIgnoreCase) >= 0);
                if (found >= 0)
                    endIdx = found;
            }

            return lines.Skip(startIdx).Take(Math.Max(0, endIdx - startIdx)).ToList();
        }

        private IReadOnlyList<KvColumnResult> ExtractKeyValuePairsInternal(
            PdfReader pdfRead,
            IEnumerable<string> knownKeys,
            int? page,
            double yTolerance,
            PdfKeyValueLayout keyValueLayout,
            int keyValueColumnCount)
            => PdfOperationMetrics.Execute(_metrics,
                Constants.Metrics.ExtractKeyValueDuration, Constants.Metrics.ExtractKeyValueSuccess, Constants.Metrics.ExtractKeyValueFailure, () => {
                    ArgumentHelpers.ThrowIfNull(knownKeys);
                    var words = GetWordsInternal(pdfRead, page);
                    return ExtractKeyValueColumns(words.AsReadOnlyList(), knownKeys, yTolerance, keyValueLayout, keyValueColumnCount);
                });

        private (IReadOnlyList<IDataTableCell> HeaderCells, IReadOnlyList<IReadOnlyDictionary<string, IDataTableCell>> Rows) ExtractTableFormattedInternal(
            PdfReader pdfRead,
            ColumnHeader[] headers,
            int? page,
            double yTolerance)
            => PdfOperationMetrics.Execute(_metrics,
                Constants.Metrics.ExtractTableDuration, Constants.Metrics.ExtractTableSuccess, Constants.Metrics.ExtractTableFailure, () => {
                    ArgumentHelpers.ThrowIfNull(headers);
                    return pdfRead.WithPdf(
                        pdfDoc => {
                            var document = pdfDoc;
                            if (page.HasValue) {
                                ArgumentHelpers.ThrowIfNotInRange(page.Value, 1, document.NumberOfPages, nameof(page), $"Page number must be between 1 and {document.NumberOfPages}.");
                                var pdfPage = document.GetPage(page.Value);
                                return ExtractTableFromWordsFormatted(pdfPage.GetWords().ToPdfWords(pdfPage.Paths), headers, yTolerance);
                            }

                            var allWords = new List<PdfWord>();
                            for (var i = 1; i <= document.NumberOfPages; i++) {
                                var pdfPage = document.GetPage(i);
                                allWords.AddRange(pdfPage.GetWords().ToPdfWords(pdfPage.Paths));
                            }

                            return ExtractTableFromWordsFormatted(allWords, headers, yTolerance);
                        });
                });

        private IReadOnlyList<IReadOnlyDictionary<string, string?>> ExtractTableInternal(PdfReader pdfRead, ColumnHeader[] headers, int? page, double yTolerance)
        {
            var (_, rows) = ExtractTableFormattedInternal(pdfRead, headers, page, yTolerance);
            return rows.Select(r => (IReadOnlyDictionary<string, string?>)r.ToDictionary(kv => kv.Key, kv => kv.Value.DisplayValue, StringComparer.OrdinalIgnoreCase)).ToList();
        }
        private PdfColumnarText BuildColumnarText(List<PdfWord> words, int columnCount, double? yTolerance)
        {
            ArgumentHelpers.ThrowIfLessThan(columnCount, 1, "Column count must be at least 1.");
            if (words.Count == 0)
                return new(Enumerable.Repeat(string.Empty, columnCount).ToList());

            var tolerance = yTolerance ?? _options.DefaultYTolerance;
            if (columnCount == 1) {
                var singleLines = GroupIntoLines(words, tolerance).OrderByDescending(l => l.Y).ToList();
                return new([string.Join("\n", singleLines.Select(l => l.Text))]);
            }

            var linesForGutter = GroupIntoLines(words, tolerance).OrderByDescending(l => l.Y).ToList();
            var minX = words.Min(w => w.BoundingBox.Left);
            var maxX = words.Max(w => w.BoundingBox.Right);
            var width = maxX - minX;
            int[] columnOfWord;
            if (columnCount == 2 && width > 0) {
                var splitX = DetectTwoColumnSplitX(linesForGutter, minX, maxX);
                var boundary = splitX ?? minX + width * 0.5;
                columnOfWord = words.Select(w => CenterX(w) <= boundary ? 0 : 1).ToArray();
            }
            else if (width > 0)
                columnOfWord = words.Select(w => ColumnIndexEqualBands(CenterX(w), minX, maxX, columnCount)).ToArray();
            else
                columnOfWord = new int[words.Count];

            var columnTexts = new List<string>(columnCount);
            for (var c = 0; c < columnCount; c++) {
                var colWords = new List<PdfWord>();
                for (var i = 0; i < words.Count; i++) {
                    if (columnOfWord[i] == c)
                        colWords.Add(words[i]);
                }

                if (colWords.Count == 0) {
                    columnTexts.Add("");
                    continue;
                }

                var colLines = GroupIntoLines(colWords, tolerance).OrderByDescending(l => l.Y).ToList();
                columnTexts.Add(string.Join("\n", colLines.Select(l => l.Text)));
            }

            return new(columnTexts);
        }

        private static double CenterX(PdfWord w) => (w.BoundingBox.Left + w.BoundingBox.Right) * 0.5;

        private static int ColumnIndexEqualBands(double centerX, double minX, double maxX, int columnCount)
        {
            if (maxX <= minX)
                return 0;

            var t = (centerX - minX) / (maxX - minX);
            var idx = (int)(t * columnCount);
            if (idx < 0)
                idx = 0;

            if (idx >= columnCount)
                idx = columnCount - 1;

            return idx;
        }

        /// <summary>Median X of the widest inter-word gap per line (when the gap clears a minimum), for two-column gutter detection.</summary>
        private static double? DetectTwoColumnSplitX(IReadOnlyList<PdfTextLine> lines, double minX, double maxX)
        {
            var width = maxX - minX;
            if (width <= 0)
                return null;

            var minGap = Math.Max(8.0, width * 0.025);
            var candidates = new List<double>();
            foreach (var line in lines) {
                var ws = line.Words.OrderBy(w => w.BoundingBox.Left).ToList();
                if (ws.Count < 2)
                    continue;

                double bestGap = 0;
                var bestMid = 0.0;
                for (var i = 0; i < ws.Count - 1; i++) {
                    var gap = ws[i + 1].BoundingBox.Left - ws[i].BoundingBox.Right;
                    if (gap > bestGap) {
                        bestGap = gap;
                        bestMid = (ws[i].BoundingBox.Right + ws[i + 1].BoundingBox.Left) * 0.5;
                    }
                }

                if (bestGap >= minGap)
                    candidates.Add(bestMid);
            }

            if (candidates.Count == 0)
                return null;

            candidates.Sort();
            return candidates[candidates.Count / 2];
        }

        /// <summary>Groups words into visual rows using proximity-based clustering. Words whose vertical mid-points are within yTolerance of each other are placed on the same line.</summary>
        private IReadOnlyList<PdfTextLine> GroupIntoLines(IEnumerable<PdfWord> words, double? yTolerance = null)
        {
            var tolerance = yTolerance ?? _options.DefaultYTolerance;
            var sorted = words.Select(w => (MidY: (w.BoundingBox.Top + w.BoundingBox.Bottom) * 0.5, Word: w)).OrderByDescending(x => x.MidY).ToList();
            if (sorted.Count == 0)
                return [];

            var groups = new List<(double CentroidY, List<PdfWord> Words)>();
            var centroidY = sorted[0].MidY;
            var current = new List<PdfWord> { sorted[0].Word };
            for (var i = 1; i < sorted.Count; i++) {
                var (midY, word) = sorted[i];
                if (Math.Abs(midY - centroidY) <= tolerance) {
                    current.Add(word);
                    centroidY = (centroidY * (current.Count - 1) + midY) / current.Count;
                }
                else {
                    groups.Add((centroidY, current));
                    centroidY = midY;
                    current = [word];
                }
            }

            groups.Add((centroidY, current));
            return groups.Select(g => {
                    var ordered = g.Words.OrderBy(w => w.BoundingBox.Left).ToList();
                    return new PdfTextLine(g.CentroidY, ordered, string.Join(" ", ordered.Select(w => w.Text)));
                })
                .ToList();
        }

        /// <summary>Merges consecutive lines whose Y gap is ≤ threshold into single logical rows.</summary>
        private IReadOnlyList<PdfTextLine> MergeCloseLines(IReadOnlyList<PdfTextLine> lines, double threshold)
        {
            if (lines.Count == 0)
                return lines;

            var result = new List<PdfTextLine>();
            var pending = new List<PdfWord>(lines[0].Words);
            var pendingY = lines[0].Y;
            for (var i = 1; i < lines.Count; i++) {
                var gap = pendingY - lines[i].Y;
                if (gap <= threshold)
                    pending.AddRange(lines[i].Words);
                else {
                    result.Add(BuildLine(pendingY, pending));
                    pending = new(lines[i].Words);
                    pendingY = lines[i].Y;
                }
            }

            result.Add(BuildLine(pendingY, pending));
            return result;
        }

        private static PdfTextLine BuildLine(double y, List<PdfWord> words)
        {
            var ordered = words.OrderBy(w => w.BoundingBox.Left).ToList();
            return new(y, ordered, string.Join(" ", ordered.Select(w => w.Text)));
        }

        /// <summary>
        /// Extracts key/value pairs from words using known keys. Use <paramref name="keyValueColumnCount" /> &gt; 1 to split the region into that many vertical bands (e.g. duplicate
        /// keys side by side).
        /// </summary>
        private IReadOnlyList<KvColumnResult> ExtractKeyValueColumns(
            IReadOnlyList<PdfWord> words,
            IEnumerable<string> knownKeys,
            double? yTolerance = null,
            PdfKeyValueLayout keyValueLayout = PdfKeyValueLayout.Horizontal,
            int keyValueColumnCount = 1)
        {
            if (words.Count == 0)
                return [new(0, new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase))];

            var tolerance = yTolerance ?? _options.DefaultYTolerance;
            var aliasToCanonical = BuildKnownKeyAliases(knownKeys);
            var keySet = new HashSet<string>(aliasToCanonical.Keys, StringComparer.OrdinalIgnoreCase);
            var explicitBands = PdfInferenceParsing.ClampKvColumnBandCount(keyValueColumnCount);
            var columnCount = explicitBands > 1 ? explicitBands : 1;
            if (columnCount > 1) {
                var bandLists = BandWordsIntoVerticalColumns(words.ToList(), columnCount);
                var results = new List<KvColumnResult>();
                for (var col = 0; col < bandLists.Count; col++) {
                    var dict = ExtractKeyValueFromWords(bandLists[col], aliasToCanonical, keySet, tolerance, keyValueLayout);
                    results.Add(new(col, dict));
                }

                return results;
            }

            var singleResult = ExtractKeyValueFromWords(words.ToList(), aliasToCanonical, keySet, tolerance, keyValueLayout);
            return [new(0, singleResult)];
        }

        /// <summary>Splits words into <paramref name="columnCount" /> equal-width vertical bands by min/max X in the region.</summary>
        private static List<List<PdfWord>> BandWordsIntoVerticalColumns(IReadOnlyList<PdfWord> words, int columnCount)
        {
            if (words.Count == 0 || columnCount <= 1)
                return [words.ToList()];

            var minX = words.Min(w => w.BoundingBox.Left);
            var maxX = words.Max(w => w.BoundingBox.Right);
            var width = maxX - minX;
            if (width <= 0)
                return [words.ToList()];

            var results = new List<List<PdfWord>>(columnCount);
            for (var col = 0; col < columnCount; col++) {
                var colMinX = minX + col * width / columnCount;
                var colMaxX = minX + (col + 1) * width / columnCount;
                var colWords = words.Where(w => {
                        var x = w.BoundingBox.Left;
                        return x >= colMinX && x < colMaxX;
                    })
                    .ToList();

                results.Add(colWords);
            }

            return results;
        }

        /// <summary>Extracts key/value pairs from a single column of words.</summary>
        private Dictionary<string, string?> ExtractKeyValueFromWords(
            List<PdfWord> words,
            Dictionary<string, string> aliasToCanonical,
            HashSet<string> keySet,
            double tolerance,
            PdfKeyValueLayout keyValueLayout)
        {
            if (keyValueLayout == PdfKeyValueLayout.Vertical)
                return ExtractKeyValueFromWordsVertical(words, aliasToCanonical, keySet, tolerance);

            var continuationYGap = Math.Max(_options.MaxContinuationYGap, tolerance * 3);
            var rawLines = GroupIntoLines(words, tolerance);
            // Don't merge lines for key-value extraction - we want strict line boundaries
            var lines = rawLines.OrderByDescending(l => l.Y).ToList();
            var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

            // Track active value columns: Key, ValueColumnLeft (where values start), LineY
            var activeValueColumns = new List<(string Key, double ValueColumnLeft, double Y)>();
            foreach (var line in lines) {
                var ws = line.Words.OrderBy(w => w.BoundingBox.Left).ToList();
                if (ws.Count == 0)
                    continue;

                // Check if this line contains any keys
                var keySpans = FindAllKeySpans(ws, keySet);
                if (keySpans.Count > 0) {
                    // This line has keys - extract values from this line only
                    // Clear active columns and set new ones based on this line
                    activeValueColumns.Clear();
                    var claimedIdxs = new HashSet<int>();
                    foreach (var ks in keySpans) {
                        for (var i = ks.StartWordIdx; i <= ks.EndWordIdx; i++)
                            claimedIdxs.Add(i);
                    }

                    var keyRights = keySpans.Select(ks => ws[ks.EndWordIdx].BoundingBox.Right).ToList();
                    var keyLefts = keySpans.Select(ks => ws[ks.StartWordIdx].BoundingBox.Left).ToList();
                    for (var ki = 0; ki < keySpans.Count; ki++) {
                        var ks = keySpans[ki];
                        var canonicalKey = aliasToCanonical.TryGetValue(ks.Key, out var mappedKey) ? mappedKey : ks.Key;
                        var keyRight = keyRights[ki];
                        var nextKeyLeft = ki + 1 < keySpans.Count ? keyLefts[ki + 1] : double.MaxValue;

                        // Extract value words on THIS line only, between key's right edge and next key's left edge
                        var valueWords = ws.Select((w, idx) => (Word: w, Idx: idx))
                            .Where(x => !claimedIdxs.Contains(x.Idx) && x.Word.BoundingBox.Left >= keyRight + _options.DefaultKeyValueGap && x.Word.BoundingBox.Left < nextKeyLeft &&
                                !keySet.Contains(x.Word.Text.Trim()))
                            .Select(x => x.Word)
                            .ToList();

                        if (valueWords.Count > 0) {
                            var valueColumnLeft = valueWords[0].BoundingBox.Left;
                            var valueText = string.Join(" ", valueWords.Select(w => w.Text));

                            // Append to existing value if it exists (for multi-line values on same line as key)
                            result.TryGetValue(canonicalKey, out var existing);
                            result[canonicalKey] = string.IsNullOrWhiteSpace(existing) ? valueText : existing + " " + valueText;

                            // Track this value column for potential continuation
                            activeValueColumns.Add((canonicalKey, valueColumnLeft, line.Y));
                        }
                        else {
                            // No value on this line, but initialize the key and track for continuation
                            if (!result.ContainsKey(canonicalKey)) {
                                result[canonicalKey] = null;
                                // Still track the key position for continuation (use key's right edge)
                                activeValueColumns.Add((canonicalKey, keyRight + _options.DefaultKeyValueGap, line.Y));
                            }
                        }
                    }
                }
                else {
                    if (activeValueColumns.Count == 0)
                        continue;

                    var lineText = line.Text.Trim();
                    if (LooksLikeSectionHeader(lineText)) {
                        activeValueColumns.Clear();
                        continue;
                    }

                    // If a full key phrase appears, this is no longer a continuation line.
                    if (FindAllKeySpans(ws, keySet).Count > 0) {
                        activeValueColumns.Clear();
                        continue;
                    }

                    for (var c = 0; c < activeValueColumns.Count; c++) {
                        var vc = activeValueColumns[c];
                        if (Math.Abs(line.Y - vc.Y) > continuationYGap)
                            continue;

                        var continuationWords = ws.Where(w
                                => w.BoundingBox.Left >= vc.ValueColumnLeft - _options.ValueColumnXTolerance &&
                                w.BoundingBox.Left <= vc.ValueColumnLeft + _options.MaxContinuationXDistance)
                            .ToList();

                        if (continuationWords.Count == 0)
                            continue;

                        var continuationText = string.Join(" ", continuationWords.Select(w => w.Text));
                        result.TryGetValue(vc.Key, out var existing);
                        result[vc.Key] = string.IsNullOrWhiteSpace(existing) ? continuationText : existing + " " + continuationText;

                        // Move the Y anchor down as we continue so multi-line values remain connected
                        activeValueColumns[c] = (vc.Key, vc.ValueColumnLeft, line.Y);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Primarily label-over-value (stacked) fields, but many PDFs mix that with label+value on one line (e.g. "Pages: 13 pages"). Same-line values are taken first; otherwise
        /// values are read from subsequent lines in the key's horizontal band.
        /// </summary>
        private Dictionary<string, string?> ExtractKeyValueFromWordsVertical(List<PdfWord> words, Dictionary<string, string> aliasToCanonical, HashSet<string> keySet, double tolerance)
        {
            var continuationYGap = Math.Max(_options.MaxContinuationYGap, tolerance * 3);
            var firstBlockMaxGap = Math.Max(_options.KeyValueStackedMaxFirstGap, Math.Max(continuationYGap * 2, _options.MaxContinuationYGap + tolerance * 4));
            var rawLines = GroupIntoLines(words, tolerance);
            var lines = rawLines.OrderByDescending(l => l.Y).ToList();
            var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (var li = 0; li < lines.Count; li++) {
                var line = lines[li];
                var ws = line.Words.OrderBy(w => w.BoundingBox.Left).ToList();
                if (ws.Count == 0)
                    continue;

                var keySpans = FindAllKeySpans(ws, keySet);
                if (keySpans.Count == 0)
                    continue;

                var keyLefts = keySpans.Select(ks => ws[ks.StartWordIdx].BoundingBox.Left).ToList();
                var keyY = line.Y;
                for (var ki = 0; ki < keySpans.Count; ki++) {
                    var ks = keySpans[ki];
                    var canonicalKey = aliasToCanonical.TryGetValue(ks.Key, out var mappedKey) ? mappedKey : ks.Key;
                    var bandLeft = keyLefts[ki] - _options.ValueColumnXTolerance;
                    var bandRight = ki + 1 < keySpans.Count ? keyLefts[ki + 1] - _options.DefaultKeyValueGap : double.MaxValue;
                    var sameLineValueWords = GetSameLineValueWordsAfterKey(ws, keySpans, ki, keySet);
                    if (sameLineValueWords.Count > 0) {
                        var sameLineText = string.Join(" ", sameLineValueWords.Select(w => w.Text)).Trim();
                        result.TryGetValue(canonicalKey, out var existingSame);
                        result[canonicalKey] = string.IsNullOrWhiteSpace(existingSame) ? sameLineText : existingSame + " " + sameLineText;
                        continue;
                    }

                    var pieces = new List<string>();
                    double? lastContentY = null;
                    double? valueColumnLeft = null;
                    for (var lj = li + 1; lj < lines.Count; lj++) {
                        var vline = lines[lj];
                        var vwsOrdered = vline.Words.OrderBy(w => w.BoundingBox.Left).ToList();
                        if (vwsOrdered.Count == 0)
                            continue;

                        var gapFromKey = keyY - vline.Y;
                        if (lastContentY == null) {
                            if (gapFromKey > firstBlockMaxGap)
                                break;
                        }
                        else if (lastContentY.Value - vline.Y > continuationYGap)
                            break;

                        if (LooksLikeSectionHeader(vline.Text.Trim()))
                            break;

                        if (FindAllKeySpans(vwsOrdered, keySet).Count > 0)
                            break;

                        List<PdfWord> bandWords;
                        if (valueColumnLeft == null) {
                            bandWords = vwsOrdered.Where(w => w.BoundingBox.Left >= bandLeft && w.BoundingBox.Left < bandRight && !keySet.Contains(w.Text.Trim())).ToList();
                            if (bandWords.Count > 0)
                                valueColumnLeft = bandWords[0].BoundingBox.Left;
                        }
                        else {
                            bandWords = vwsOrdered.Where(w
                                    => w.BoundingBox.Left >= valueColumnLeft.Value - _options.ValueColumnXTolerance &&
                                    w.BoundingBox.Left <= valueColumnLeft.Value + _options.MaxContinuationXDistance && !keySet.Contains(w.Text.Trim()))
                                .ToList();
                        }

                        if (bandWords.Count == 0)
                            continue;

                        pieces.Add(string.Join(" ", bandWords.Select(w => w.Text)));
                        lastContentY = vline.Y;
                    }

                    var combined = string.Join(" ", pieces).Trim();
                    if (string.IsNullOrWhiteSpace(combined)) {
                        if (!result.ContainsKey(canonicalKey))
                            result[canonicalKey] = null;
                    }
                    else {
                        result.TryGetValue(canonicalKey, out var existing);
                        result[canonicalKey] = string.IsNullOrWhiteSpace(existing) ? combined : existing + " " + combined;
                    }
                }
            }

            return result;
        }

        /// <summary>Words on the same line as the key, to the right of the key span (horizontal strip), excluding other keys.</summary>
        private List<PdfWord> GetSameLineValueWordsAfterKey(List<PdfWord> ws, IReadOnlyList<KeySpan> keySpans, int ki, HashSet<string> keySet)
        {
            var claimedIdxs = new HashSet<int>();
            foreach (var ksp in keySpans) {
                for (var i = ksp.StartWordIdx; i <= ksp.EndWordIdx; i++)
                    claimedIdxs.Add(i);
            }

            var keyRights = keySpans.Select(ks => ws[ks.EndWordIdx].BoundingBox.Right).ToList();
            var keyLefts = keySpans.Select(ks => ws[ks.StartWordIdx].BoundingBox.Left).ToList();
            var keyRight = keyRights[ki];
            var nextKeyLeft = ki + 1 < keySpans.Count ? keyLefts[ki + 1] : double.MaxValue;
            return ws.Select((w, idx) => (Word: w, Idx: idx))
                .Where(x => !claimedIdxs.Contains(x.Idx) && x.Word.BoundingBox.Left >= keyRight + _options.DefaultKeyValueGap && x.Word.BoundingBox.Left < nextKeyLeft &&
                    !keySet.Contains(x.Word.Text.Trim()))
                .Select(x => x.Word)
                .ToList();
        }

        private List<KeySpan> FindAllKeySpans(List<PdfWord> words, HashSet<string> keySet)
        {
            var spans = new List<KeySpan>();
            var keyPrefixes = BuildPrefixSet(keySet);
            for (var i = 0; i < words.Count; i++) {
                var candidate = new StringBuilder();
                var bestEnd = -1;
                string? bestKey = null;
                for (var j = i; j < words.Count; j++) {
                    if (candidate.Length > 0)
                        candidate.Append(' ');

                    candidate.Append(words[j].Text);
                    var candidateStr = candidate.ToString();
                    if (!keyPrefixes.Contains(candidateStr))
                        break;

                    if (!keySet.Contains(candidateStr))
                        continue;

                    bestEnd = j;
                    bestKey = candidateStr;
                }

                if (bestKey == null)
                    continue;

                spans.Add(new(bestKey, i, bestEnd));
                i = bestEnd;
            }

            return spans;
        }

        private HashSet<string> BuildPrefixSet(HashSet<string> keySet)
        {
            var prefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in keySet) {
                var tokens = key.Split([' '], StringSplitOptions.RemoveEmptyEntries);
                var sb = new StringBuilder();
                foreach (var token in tokens) {
                    if (sb.Length > 0)
                        sb.Append(' ');

                    sb.Append(token);
                    prefixes.Add(sb.ToString());
                }
            }

            return prefixes;
        }

        private static bool LooksLikeSectionHeader(string lineText)
        {
            if (string.IsNullOrWhiteSpace(lineText))
                return false;

            var words = lineText.Split([' '], StringSplitOptions.RemoveEmptyEntries);
            return (lineText.Length > 5 && words.Length <= 5 && lineText.All(c => char.IsUpper(c) || char.IsWhiteSpace(c) || char.IsDigit(c) || c == '/' || c == '-' || c == ':')) ||
                (words.Length == 1 && lineText.Length > 3 && lineText.All(char.IsUpper));
        }

        /// <summary>Extracts a table from words using column headers. Data rows are expected to be below the header row.</summary>
        private (IReadOnlyList<IDataTableCell> HeaderCells, IReadOnlyList<IReadOnlyDictionary<string, IDataTableCell>> Rows) ExtractTableFromWordsFormatted(
            IReadOnlyList<PdfWord> words,
            ColumnHeader[] headers,
            double? yTolerance = null,
            PdfInferFormattingFlags? inferFormattingForHeaderRows = null)
        {
            if (words.Count == 0 || headers.Length == 0)
                return ([], []);

            ArgumentHelpers.ThrowIfNullOrEmpty(headers);
            var tolerance = yTolerance ?? _options.DefaultYTolerance;
            var allLines = GroupIntoLinesPreservingInputOrder(words, tolerance);
            var headerLabels = headers.Select(h => h.Label).ToArray();

            // Find the first matching header row in reading order.
            PdfTextLine? headerLine = null;
            var headerLineIndex = -1;
            var headerLinesSpanned = 1;
            List<PdfWord> headerWords = new();
            for (var li = 0; li < allLines.Count; li++) {
                var spanWords = new List<PdfWord>(allLines[li].Words);
                var mergedLines = 1;
                var j = li;
                while (j + 1 < allLines.Count && allLines[j].Y - allLines[j + 1].Y <= _options.TableHeaderMergeThreshold &&
                    inferFormattingForHeaderRows is PdfInferFormattingFlags hf && (hf & (PdfInferFormattingFlags.Bold | PdfInferFormattingFlags.Underline)) != 0 &&
                    PdfInferenceParsing.LineQualifiesForHeaderBlockExtension(allLines[j], allLines[j + 1], hf)) {
                    spanWords.AddRange(allLines[j + 1].Words);
                    mergedLines++;
                    j++;
                }

                var mergedForHits = string.Join(" ", Enumerable.Range(0, mergedLines).Select(k => allLines[li + k].Text));
                var candidateHits = headerLabels.Count(h => LineContainsHeaderLabel(mergedForHits, h));
                var minRequired = Math.Ceiling(headerLabels.Length * _options.TableHeaderMatchThreshold);
                if (!(candidateHits >= minRequired))
                    continue;

                headerWords = spanWords.OrderBy(w => w.BoundingBox.Left).ToList();
                headerLine = allLines[li];
                headerLineIndex = li;
                headerLinesSpanned = mergedLines;
                break;
            }

            if (headerLine == null || headerWords.Count == 0 || headerLineIndex < 0)
                return ([], []);

            // Map headers to column X positions
            var columnPositions = new List<(ColumnHeader Header, double StartX)>();
            foreach (var header in headers) {
                if (TryFindHeaderStartX(headerWords, header.Label, out var startX))
                    columnPositions.Add((header, startX));
            }

            if (columnPositions.Count == 0)
                return ([], []);

            columnPositions = columnPositions.OrderBy(c => c.StartX).ToList();

            // Build formatted header cells from actual PDF header words
            var headerCells = new List<IDataTableCell>();
            for (var colIndex = 0; colIndex < columnPositions.Count; colIndex++) {
                var col = columnPositions[colIndex];
                var nextStart = colIndex + 1 < columnPositions.Count ? columnPositions[colIndex + 1].StartX : double.MaxValue;
                var startX = Math.Max(double.MinValue, col.StartX - _options.TableColumnXTolerance);
                var endX = nextStart - _options.TableColumnXTolerance;
                var cellWords = headerWords.Where(w => w.BoundingBox.Left >= startX && w.BoundingBox.Left < endX).ToList();
                headerCells.Add(BuildCellFromWords(cellWords));
            }

            // When the header spans two visual lines, both were merged into headerWords — data must start after the second line.
            var dataLines = allLines.Skip(headerLineIndex + headerLinesSpanned).ToList();
            var rows = new List<Dictionary<string, IDataTableCell>>();
            foreach (var line in dataLines) {
                if (string.IsNullOrWhiteSpace(line.Text))
                    continue;

                var row = new Dictionary<string, IDataTableCell>(StringComparer.OrdinalIgnoreCase);
                var orderedWords = line.Words.OrderBy(w => w.BoundingBox.Left).ToList();

                // Left-aligned table assumption:
                // a column owns words between its start X and the next column's start X.
                for (var colIndex = 0; colIndex < columnPositions.Count; colIndex++) {
                    var col = columnPositions[colIndex];
                    var nextStart = colIndex + 1 < columnPositions.Count ? columnPositions[colIndex + 1].StartX : double.MaxValue;
                    var startX = Math.Max(double.MinValue, col.StartX - _options.TableColumnXTolerance);
                    var endX = nextStart - _options.TableColumnXTolerance;
                    var cellWords = orderedWords.Where(w => w.BoundingBox.Left >= startX && w.BoundingBox.Left < endX).ToList();
                    row[col.Header.Label] = BuildCellFromWords(cellWords);
                }

                if (IsHeaderEchoRowFormatted(row, headers))
                    continue;

                if (row.Values.Any(v => !string.IsNullOrWhiteSpace(v.DisplayValue)))
                    rows.Add(row);
            }

            // Merge multi-line rows using explicit key columns.
            var keyLabels = new HashSet<string>(headers.Where(h => h.IsKey).Select(h => h.Label), StringComparer.OrdinalIgnoreCase);
            if (keyLabels.Count == 0)
                return (headerCells, rows);

            var mergedRows = new List<IReadOnlyDictionary<string, IDataTableCell>>();
            Dictionary<string, IDataTableCell>? currentRow = null;
            foreach (var row in rows) {
                var hasKey = keyLabels.Any(label => {
                    row.TryGetValue(label, out var cell);
                    return !string.IsNullOrWhiteSpace(cell?.DisplayValue);
                });

                if (hasKey) {
                    if (currentRow != null)
                        mergedRows.Add(currentRow);

                    currentRow = new(row, StringComparer.OrdinalIgnoreCase);
                }
                else if (currentRow != null) {
                    foreach (var kvp in row) {
                        if (string.IsNullOrWhiteSpace(kvp.Value.DisplayValue))
                            continue;

                        currentRow.TryGetValue(kvp.Key, out var existing);
                        currentRow[kvp.Key] = CombineCells(existing, kvp.Value);
                    }
                }
                else
                    currentRow = new(row, StringComparer.OrdinalIgnoreCase);
            }

            if (currentRow != null)
                mergedRows.Add(currentRow);

            return (headerCells, mergedRows);
        }

        private static IDataTableCell BuildCellFromWords(List<PdfWord> cellWords)
        {
            if (cellWords.Count == 0)
                return DataTableCell.FromValue("");

            var text = string.Join(" ", cellWords.Select(w => w.Text));
            var firstWithFormat = cellWords.FirstOrDefault(w => w.Format != null)?.Format;
            if (firstWithFormat == null)
                return DataTableCell.FromValue(text);

            return new DataTableCell<string>(
                text, firstWithFormat.FontSize, firstWithFormat.FontName, firstWithFormat.FontBold, firstWithFormat.FontItalic, firstWithFormat.FontUnderline, null,
                firstWithFormat.FontColor);
        }

        private static IDataTableCell CombineCells(IDataTableCell? a, IDataTableCell b)
        {
            if (a == null || string.IsNullOrWhiteSpace(a.DisplayValue))
                return b;

            if (string.IsNullOrWhiteSpace(b.DisplayValue))
                return a;

            var value = a.DisplayValue + " " + b.DisplayValue;
            return new DataTableCell<string>(value, a.FontSize, a.FontName, a.FontBold, a.FontItalic, a.FontUnderline, a.FontStrikethrough, a.FontColor);
        }

        private static bool IsHeaderEchoRowFormatted(Dictionary<string, IDataTableCell> row, ColumnHeader[] headers)
        {
            var strRow = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in row)
                strRow[kv.Key] = kv.Value.DisplayValue;

            return IsHeaderEchoRow(strRow, headers);
        }

        private IReadOnlyList<IReadOnlyDictionary<string, string?>> ExtractTableFromWords(
            IReadOnlyList<PdfWord> words,
            ColumnHeader[] headers,
            double? yTolerance = null,
            PdfInferFormattingFlags? inferFormattingForHeaderRows = null)
        {
            var (_, rows) = ExtractTableFromWordsFormatted(words, headers, yTolerance, inferFormattingForHeaderRows);
            return rows.Select(r => (IReadOnlyDictionary<string, string?>)r.ToDictionary(kv => kv.Key, kv => kv.Value.DisplayValue, StringComparer.OrdinalIgnoreCase)).ToList();
        }

        private static List<PdfTextLine> GroupIntoLinesPreservingInputOrder(IReadOnlyList<PdfWord> words, double yTolerance)
        {
            if (words.Count == 0)
                return [];

            var ordered = words.ToList();
            var lines = new List<PdfTextLine>();
            var currentWords = new List<PdfWord> { ordered[0] };
            var currentY = (ordered[0].BoundingBox.Top + ordered[0].BoundingBox.Bottom) * 0.5;
            var lastX = ordered[0].BoundingBox.Left;
            for (var i = 1; i < ordered.Count; i++) {
                var word = ordered[i];
                var midY = (word.BoundingBox.Top + word.BoundingBox.Bottom) * 0.5;
                var x = word.BoundingBox.Left;
                var sameVisualLine = Math.Abs(midY - currentY) <= yTolerance;
                var continuesLeftToRight = x >= lastX - yTolerance;
                if (sameVisualLine && continuesLeftToRight) {
                    currentWords.Add(word);
                    currentY = (currentY * (currentWords.Count - 1) + midY) / currentWords.Count;
                    lastX = x;
                    continue;
                }

                var finalized = currentWords.OrderBy(w => w.BoundingBox.Left).ToList();
                lines.Add(new(currentY, finalized, string.Join(" ", finalized.Select(w => w.Text))));
                currentWords = [word];
                currentY = midY;
                lastX = x;
            }

            var lastLineWords = currentWords.OrderBy(w => w.BoundingBox.Left).ToList();
            lines.Add(new(currentY, lastLineWords, string.Join(" ", lastLineWords.Select(w => w.Text))));
            return lines;
        }

        private static bool TryFindHeaderStartX(IReadOnlyList<PdfWord> headerWords, string headerLabel, out double startX)
        {
            startX = 0;
            var tokens = SplitHeaderTokens(headerLabel);
            if (tokens.Count == 0)
                return false;

            var ordered = headerWords.OrderBy(w => w.BoundingBox.Left).ToList();
            var normalizedWords = ordered.Select(w => NormalizeToken(w.Text)).ToList();
            for (var i = 0; i < normalizedWords.Count; i++) {
                if (!string.Equals(normalizedWords[i], tokens[0], StringComparison.OrdinalIgnoreCase))
                    continue;

                var match = true;
                for (var j = 1; j < tokens.Count; j++) {
                    var idx = i + j;
                    if (idx >= normalizedWords.Count || !string.Equals(normalizedWords[idx], tokens[j], StringComparison.OrdinalIgnoreCase)) {
                        match = false;
                        break;
                    }
                }

                if (!match)
                    continue;

                startX = ordered[i].BoundingBox.Left;
                return true;
            }

            // Fallback to the first token match if full phrase isn't contiguous.
            var fallbackIdx = normalizedWords.FindIndex(w => string.Equals(w, tokens[0], StringComparison.OrdinalIgnoreCase));
            if (fallbackIdx < 0)
                return false;

            startX = ordered[fallbackIdx].BoundingBox.Left;
            return true;
        }

        private static List<string> SplitHeaderTokens(string text)
            => text.Split([' '], StringSplitOptions.RemoveEmptyEntries).Select(NormalizeToken).Where(t => !string.IsNullOrWhiteSpace(t)).ToList();

        private static bool LineContainsHeaderLabel(string lineText, string headerLabel)
        {
            var lineTokens = SplitHeaderTokens(lineText);
            var labelTokens = SplitHeaderTokens(headerLabel);
            if (lineTokens.Count == 0 || labelTokens.Count == 0 || lineTokens.Count < labelTokens.Count)
                return false;

            for (var i = 0; i <= lineTokens.Count - labelTokens.Count; i++) {
                var allMatch = true;
                for (var j = 0; j < labelTokens.Count; j++) {
                    if (string.Equals(lineTokens[i + j], labelTokens[j], StringComparison.OrdinalIgnoreCase))
                        continue;

                    allMatch = false;
                    break;
                }

                if (allMatch)
                    return true;
            }

            return false;
        }

        private static bool IsHeaderEchoRow(IReadOnlyDictionary<string, string?> row, ColumnHeader[] headers)
        {
            foreach (var header in headers) {
                row.TryGetValue(header.Label, out var value);
                if (!string.Equals(NormalizeToken(value ?? string.Empty), NormalizeToken(header.Label), StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        private static Dictionary<string, string> BuildKnownKeyAliases(IEnumerable<string> knownKeys)
        {
            var keys = knownKeys.Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => k.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var aliasToCanonical = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in keys) {
                aliasToCanonical[key] = key;
                // PDFs often attach ':' to the label word ("Pages:") while users type "Pages".
                if (!key.EndsWith(":") && !aliasToCanonical.ContainsKey(key + ":"))
                    aliasToCanonical[key + ":"] = key;
            }

            return aliasToCanonical;
        }

        private static string NormalizeKeyAlias(string text) => new string(text.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

        private static string NormalizeToken(string token) => new(token.Where(c => char.IsLetterOrDigit(c) || c == '#').ToArray());

        /// <summary>Merges a trailing "Dt." / "Dt" token into the previous header when PDF word segmentation splits "Offense Dt." into two columns.</summary>
        private static List<string> MergeOrphanDtHeaderTokens(IReadOnlyList<string> labels)
        {
            if (labels.Count < 2)
                return labels.ToList();

            var list = new List<string>();
            for (var i = 0; i < labels.Count; i++) {
                if (i < labels.Count - 1 && string.Equals(NormalizeToken(labels[i + 1]), "Dt", StringComparison.OrdinalIgnoreCase) && labels[i + 1].Length <= 12) {
                    list.Add((labels[i].Trim() + " " + labels[i + 1].Trim()).Trim());
                    i++;
                    continue;
                }

                list.Add(labels[i]);
            }

            return list;
        }

        private readonly record struct KeySpan(string Key, int StartWordIdx, int EndWordIdx);
}
