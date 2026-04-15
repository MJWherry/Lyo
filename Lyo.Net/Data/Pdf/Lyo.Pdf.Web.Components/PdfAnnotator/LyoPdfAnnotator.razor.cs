using System.Text.Json;
using Lyo.Pdf.Models;
using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.Pdf.Web.Components.PdfAnnotator;

public partial class LyoPdfAnnotator : IAsyncDisposable
{
    private List<LyoPdfAnnotationResult> _annotationRows = [];
    private LyoPdfAnnotatorController? _annotatorController;
    private string? _annotatorHtml;
    private bool _attachPending;

    private ElementReference _iframe;
    private string? _latestAnnotationsJson;
    private byte[]? _pdfBytes;
    private LoadedPdfLease? _pdfLease;
    private bool _resultPhase;
    private bool _viewerAttachPending;
    private string? _viewerHtml;
    private ElementReference _viewerIframe;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    [Inject]
    private ISnackbar Snackbar { get; set; } = default!;

    [Inject]
    private IPdfService PdfService { get; set; } = default!;

    [Parameter]
    public EventCallback<IReadOnlyList<LyoPdfAnnotationResult>> AnnotationsChanged { get; set; }

    [Parameter]
    public EventCallback<IReadOnlyList<LyoPdfAnnotationResult>> AnnotationsSaved { get; set; }

    public async ValueTask DisposeAsync()
    {
        if (_annotatorController != null) {
            try {
                await _annotatorController.DisposeAsync();
            }
            catch (JSDisconnectedException) { }
        }

        DisposePdfLease();
    }

    protected override void OnInitialized() => _annotatorController = new(JsRuntime);

    private async Task OnPdfLoadedAsync(LocalBrowserFile f)
    {
        if (f.Content.Length == 0) {
            Snackbar.Add("PDF is empty.", Severity.Warning);
            return;
        }

        if (_annotatorController != null)
            await _annotatorController.ResetAsync();

        DisposePdfLease();
        _pdfLease = await PdfService.LoadPdfFromBytesAsync(f.Content);
        _pdfBytes = f.Content;
        _annotatorHtml = PdfAnnotatorHtml.GetForBlazor(Convert.ToBase64String(f.Content));
        _annotationRows.Clear();
        _resultPhase = false;
        _viewerHtml = null;
        _latestAnnotationsJson = null;
        _attachPending = true;
        await InvokeAsync(StateHasChanged);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_attachPending && _annotatorHtml != null && _annotatorController != null) {
            _attachPending = false;
            try {
                await _annotatorController.AttachAnnotatorAsync(_iframe, _annotatorHtml, this);
            }
            catch (JSException ex) {
                Snackbar.Add($"Annotator failed to start: {ex.Message}", Severity.Error);
            }
        }

        if (_viewerAttachPending && _resultPhase && _viewerHtml != null && _annotatorController != null) {
            _viewerAttachPending = false;
            try {
                await _annotatorController.SetViewerHtmlAsync(_viewerIframe, _viewerHtml);
            }
            catch (JSException ex) {
                Snackbar.Add($"Viewer failed: {ex.Message}", Severity.Error);
            }
        }
    }

    [JSInvokable]
    public async Task OnPdfAnnotationProgress(string annotationsJson)
    {
        try {
            await UpdateAnnotationRowsAsync(annotationsJson);
            await NotifyAnnotationsChangedAsync();
        }
        catch (Exception ex) {
            Snackbar.Add($"Live update failed: {ex.Message}", Severity.Warning);
        }

        await InvokeAsync(StateHasChanged);
    }

    [JSInvokable]
    public Task OnPdfAnnotatorWarning(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
            Snackbar.Add(message, Severity.Warning);

        return Task.CompletedTask;
    }

    [JSInvokable]
    public async Task OnPdfAnnotationsSaved(string annotationsJson)
    {
        try {
            await UpdateAnnotationRowsAsync(annotationsJson);
            await NotifyAnnotationsChangedAsync();
            await NotifyAnnotationsSavedAsync();
        }
        catch (Exception ex) {
            Snackbar.Add($"Extraction failed: {ex.Message}", Severity.Error);
        }

        if (_pdfBytes != null)
            _viewerHtml = PdfAnnotatorHtml.GetReadOnlyViewerForBlazor(Convert.ToBase64String(_pdfBytes), annotationsJson);

        _resultPhase = true;
        _annotatorHtml = null;
        _viewerAttachPending = true;
        if (_annotatorController != null)
            await _annotatorController.ResetAsync();

        await InvokeAsync(StateHasChanged);
    }

    private async Task UpdateAnnotationRowsAsync(string annotationsJson)
    {
        _latestAnnotationsJson = annotationsJson;
        var parsed = ParseAnnotations(annotationsJson).ToList();
        var duplicateKeys = parsed.GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase).Where(x => x.Count() > 1).Select(x => x.Key).ToList();
        if (duplicateKeys.Count > 0) {
            Snackbar.Add($"Duplicate annotation ids were ignored: {string.Join(", ", duplicateKeys)}", Severity.Warning);
            parsed = parsed.GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase).Select(x => x.First()).ToList();
        }

        if (parsed.Count == 0) {
            _annotationRows = [];
            return;
        }

        var existingByKey = _annotationRows.ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);
        if (_pdfLease == null) {
            _annotationRows = parsed.Select(a => CreateOrUpdateResult(a, existingByKey.GetValueOrDefault(a.Key))).ToList();
            return;
        }

        var leaseId = _pdfLease.Id;
        var rows = await Task.Run(() => {
            var list = new List<LyoPdfAnnotationResult>(parsed.Count);
            foreach (var a in parsed) {
                var result = CreateOrUpdateResult(a, existingByKey.GetValueOrDefault(a.Key));
                ApplyExtraction(leaseId, a, result);
                list.Add(result);
            }

            return list;
        });

        _annotationRows = rows;
    }

    private static LyoPdfAnnotationResult CreateOrUpdateResult(ParsedAnnotation annotation, LyoPdfAnnotationResult? existing)
        => new() {
            Key = annotation.Key,
            BoundingBoxSummary = FormatBBox(annotation),
            ExtractionType = existing?.ExtractionType ?? PdfAnnotationExtractionType.BoundingBoxText,
            KnownKeys = existing?.KnownKeys?.ToList() ?? [],
            TableHeaders = existing?.TableHeaders?.ToList() ?? [],
            YTolerance = existing?.YTolerance ?? 5.0,
            KeyValueLayout = existing?.KeyValueLayout ?? PdfKeyValueLayout.Horizontal,
            ColumnCount = existing?.ColumnCount ?? 1,
            InferFormattingFlags = existing?.InferFormattingFlags ?? (PdfInferFormattingFlags.Bold | PdfInferFormattingFlags.Underline),
            KeyValueInferDelimiters = existing?.KeyValueInferDelimiters ?? ":;",
            TableKeyColumnLabel = existing?.TableKeyColumnLabel
        };

    private void ApplyExtraction(Guid leaseId, ParsedAnnotation annotation, LyoPdfAnnotationResult result)
    {
        result.ExtractedText = string.Empty;
        result.ErrorMessage = null;
        result.KeyValuePairs = null;
        result.TableRows = null;
        result.ColumnTexts = null;
        var region = new PdfBoundingBox(annotation.Page, new(annotation.Left, annotation.Right, annotation.Top, annotation.Bottom));
        try {
            var lines = PdfService.GetLinesInBoundingBox(leaseId, region, result.YTolerance);
            var words = lines.SelectMany(l => l.Words).ToList();
            switch (result.ExtractionType) {
                case PdfAnnotationExtractionType.KeyValue:
                    ApplyKeyValueExtraction(words, result);
                    break;
                case PdfAnnotationExtractionType.Table:
                    ApplyTableExtraction(words, result);
                    break;
                default:
                    ApplyBoundingBoxTextExtraction(lines, words, result);
                    break;
            }
        }
        catch (Exception ex) {
            result.ErrorMessage = ex.Message;
        }
    }

    private void ApplyBoundingBoxTextExtraction(IReadOnlyList<PdfTextLine> lines, IReadOnlyList<PdfWord> words, LyoPdfAnnotationResult result)
    {
        var columnCount = Math.Max(1, result.ColumnCount);
        if (columnCount <= 1) {
            result.ColumnTexts = null;
            result.ExtractedText = string.Join("\n", lines.Select(l => l.Text)).Trim();
            return;
        }

        var columnar = PdfService.GetColumnarText(words, columnCount, result.YTolerance);
        result.ColumnTexts = columnar.Columns.ToList();
        result.ExtractedText = columnar.ToCombinedString().Trim();
    }

    private void ApplyKeyValueExtraction(IReadOnlyList<PdfWord> words, LyoPdfAnnotationResult result)
    {
        var knownKeys = result.KnownKeys.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var kvColumnCount = Math.Max(1, result.ColumnCount);
        if (knownKeys.Length == 0) {
            var inferred = PdfService.InferKeyValuePairsFromFormatting(
                words,
                result.YTolerance,
                kvColumnCount,
                result.InferFormattingFlags,
                DelimitersForInference(result));
            result.KeyValuePairs = inferred;
            result.KnownKeys = [.. inferred.Keys];
            result.ExtractedText = inferred.Count == 0
                ? string.Empty
                : string.Join(Environment.NewLine, inferred.Select(x => $"{x.Key}: {x.Value ?? "—"}"));
            result.ErrorMessage = inferred.Count == 0
                ? "Could not infer keys with the selected inference options (bold, underline, punctuation delimiters); add keys manually or adjust tolerance/columns."
                : null;
            return;
        }

        var columns = PdfService.ExtractKeyValuePairs(words, knownKeys, result.YTolerance, result.KeyValueLayout, kvColumnCount);
        result.KeyValuePairs = KvColumnResult.Merge(columns);
        result.ExtractedText = result.KeyValuePairs.Count == 0 ? string.Empty : string.Join(Environment.NewLine, result.KeyValuePairs.Select(x => $"{x.Key}: {x.Value ?? "—"}"));
    }

    private void ApplyTableExtraction(IReadOnlyList<PdfWord> words, LyoPdfAnnotationResult result)
    {
        var headers = ParseTableHeaders(result.TableHeaders);
        headers = ApplyTableKeyColumnOverride(headers, result.TableKeyColumnLabel);
        if (headers.Length == 0) {
            headers = PdfService.InferTableHeadersFromFormatting(words, result.YTolerance, result.InferFormattingFlags, DelimitersForInference(result));
            if (headers.Length == 0) {
                result.ErrorMessage = "Could not infer table headers with the selected inference options; add headers manually.";
                return;
            }

            result.TableHeaders = [.. headers.Select(h => h.Label)];
        }

        if (string.IsNullOrWhiteSpace(result.TableKeyColumnLabel) && headers.Length > 0)
            result.TableKeyColumnLabel = headers[0].Label;

        headers = ApplyTableKeyColumnOverride(headers, result.TableKeyColumnLabel);
        result.TableRows = PdfService.ExtractTable(words, headers, result.YTolerance, result.InferFormattingFlags);
        result.ExtractedText = result.TableRows.Count == 0 ? string.Empty : $"{result.TableRows.Count} row(s) extracted.";
    }

    private static IEnumerable<ParsedAnnotation> ParseAnnotations(string json)
    {
        using var doc = JsonDocument.Parse(json);
        foreach (var el in doc.RootElement.EnumerateArray()) {
            var key = el.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";
            if (string.IsNullOrWhiteSpace(key))
                continue;

            var page = el.TryGetProperty("page", out var p) ? p.GetInt32() : 1;
            var left = el.TryGetProperty("left", out var l) ? l.GetDouble() : 0;
            var right = el.TryGetProperty("right", out var r) ? r.GetDouble() : 0;
            var top = el.TryGetProperty("top", out var t) ? t.GetDouble() : 0;
            var bottom = el.TryGetProperty("bottom", out var b) ? b.GetDouble() : 0;
            yield return new(key, page, left, right, top, bottom);
        }
    }

    private static string FormatBBox(ParsedAnnotation a) => $"p{a.Page} L{a.Left:F0} T{a.Top:F0} R{a.Right:F0} B{a.Bottom:F0}";

    private IReadOnlyList<LyoPdfAnnotationResult> ToAnnotationResults() => _annotationRows.Select(CloneResult).ToList();

    private Task NotifyAnnotationsChangedAsync() => AnnotationsChanged.HasDelegate ? AnnotationsChanged.InvokeAsync(ToAnnotationResults()) : Task.CompletedTask;

    private Task NotifyAnnotationsSavedAsync() => AnnotationsSaved.HasDelegate ? AnnotationsSaved.InvokeAsync(ToAnnotationResults()) : Task.CompletedTask;

    private async Task ApplyExtractionSettingsAsync(LyoPdfAnnotationResult result)
    {
        if (string.IsNullOrWhiteSpace(_latestAnnotationsJson))
            return;

        await UpdateAnnotationRowsAsync(_latestAnnotationsJson);
        await NotifyAnnotationsChangedAsync();
        if (_resultPhase)
            await NotifyAnnotationsSavedAsync();

        await InvokeAsync(StateHasChanged);
    }

    private async Task ResetAsync()
    {
        if (_annotatorController != null)
            await _annotatorController.ResetAsync();

        _pdfBytes = null;
        _annotatorHtml = null;
        _viewerHtml = null;
        _resultPhase = false;
        _viewerAttachPending = false;
        _annotationRows.Clear();
        _latestAnnotationsJson = null;
        DisposePdfLease();
        await InvokeAsync(StateHasChanged);
    }

    private void DisposePdfLease()
    {
        _pdfLease?.Dispose();
        _pdfLease = null;
    }

    private static ColumnHeader[] ParseTableHeaders(IEnumerable<string>? headers)
        => headers?.Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.StartsWith('*') ? new(x[1..].Trim(), true) : new ColumnHeader(x))
            .Where(x => !string.IsNullOrWhiteSpace(x.Label))
            .ToArray() ?? [];

    /// <summary>When <paramref name="tableKeyColumnLabel" /> is set, only that header is marked <see cref="ColumnHeader.IsKey" /> (overrides <c>*</c> on chips). When null/empty, callers may treat the first column as the key.</summary>
    private static ColumnHeader[] ApplyTableKeyColumnOverride(ColumnHeader[] headers, string? tableKeyColumnLabel)
    {
        if (headers.Length == 0 || string.IsNullOrWhiteSpace(tableKeyColumnLabel))
            return headers;

        var k = tableKeyColumnLabel.Trim();
        return headers.Select(h => new ColumnHeader(h.Label, string.Equals(h.Label, k, StringComparison.OrdinalIgnoreCase))).ToArray();
    }

    private static LyoPdfAnnotationResult CloneResult(LyoPdfAnnotationResult row)
        => new() {
            Key = row.Key,
            BoundingBoxSummary = row.BoundingBoxSummary,
            ExtractionType = row.ExtractionType,
            ExtractedText = row.ExtractedText,
            ErrorMessage = row.ErrorMessage,
            KeyValuePairs = row.KeyValuePairs == null ? null : new Dictionary<string, string?>(row.KeyValuePairs, StringComparer.OrdinalIgnoreCase),
            TableRows = row.TableRows?.Select(r => (IReadOnlyDictionary<string, string?>)new Dictionary<string, string?>(r, StringComparer.OrdinalIgnoreCase)).ToList(),
            KnownKeys = row.KnownKeys.ToList(),
            TableHeaders = row.TableHeaders.ToList(),
            YTolerance = row.YTolerance,
            KeyValueLayout = row.KeyValueLayout,
            ColumnCount = row.ColumnCount,
            InferFormattingFlags = row.InferFormattingFlags,
            KeyValueInferDelimiters = row.KeyValueInferDelimiters,
            TableKeyColumnLabel = row.TableKeyColumnLabel,
            ColumnTexts = row.ColumnTexts?.ToList()
        };

    /// <summary>Distinct delimiter characters for <see cref="PdfService.InferKeyValuePairsFromFormatting" />, or <c>null</c> to use service defaults.</summary>
    private static IReadOnlyList<char>? DelimitersForInference(LyoPdfAnnotationResult result)
    {
        var s = result.KeyValueInferDelimiters;
        if (string.IsNullOrEmpty(s))
            return null;

        var list = new List<char>();
        foreach (var c in s) {
            if (char.IsWhiteSpace(c) || char.IsControl(c))
                continue;

            if (list.Contains(c))
                continue;

            list.Add(c);
        }

        return list.Count > 0 ? list : null;
    }

    private sealed record ParsedAnnotation(string Key, int Page, double Left, double Right, double Top, double Bottom);
}