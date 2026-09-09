using Lyo.Pdf.Models;
using Microsoft.AspNetCore.Components;

namespace Lyo.Pdf.Web.Components.PdfAnnotator;

public partial class LyoPdfAnnotatorResultsView
{
    private static readonly (string Value, string Label)[] KvDelimiterSelectOptions = [(":", "Colon"), (";", "Semicolon"), ("=", "Equals"), ("?", "Question"), ("|", "Pipe"), ("-", "Dash"), (".", "Dot")];

    private static readonly HashSet<char> KvDelimiterPresetChars = [.. KvDelimiterSelectOptions.Select(o => o.Value[0])];

    private readonly Dictionary<string, string> _customDelimiterDraft = new(StringComparer.Ordinal);

    [Parameter]
    public IReadOnlyList<LyoPdfAnnotationResult> Rows { get; set; } = [];

    [Parameter]
    public bool ResultPhase { get; set; }

    [Parameter]
    public EventCallback<LyoPdfAnnotationResult> OnApply { get; set; }

    private static IReadOnlyList<string> GetTableHeaders(IReadOnlyList<IReadOnlyDictionary<string, string?>> rows) => rows.SelectMany(x => x.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    private static string GetExtractionSummary(LyoPdfAnnotationResult row)
        => row.ExtractionType switch {
            PdfAnnotationExtractionType.KeyValue => FormatKeyValueExtractionSummary(row),
            PdfAnnotationExtractionType.Table => FormatTableExtractionSummary(row),
            PdfAnnotationExtractionType.BoundingBoxText => FormatPlainTextExtractionSummary(row),
            var _ => "Paragraph"
        };

    private static string FormatTableExtractionSummary(LyoPdfAnnotationResult row)
    {
        var h = row.TableHeaders.Count;
        var r = row.TableRows?.Count ?? 0;
        return $"Table ({Plural(h, "header", "headers")}, {Plural(r, "row", "rows")})";
    }

    private static string FormatKeyValueExtractionSummary(LyoPdfAnnotationResult row)
    {
        var layout = row.KeyValueLayout == PdfKeyValueLayout.Vertical ? "Key/Value vertical" : "Key/Value horizontal";
        var k = row.KnownKeys.Count;
        var c = row.ColumnCount < 1 ? 1 : row.ColumnCount;
        return $"{layout} ({Plural(k, "key", "keys")}, {Plural(c, "column", "columns")})";
    }

    private static string FormatPlainTextExtractionSummary(LyoPdfAnnotationResult row)
    {
        var lines = CountNonEmptyLines(row.ExtractedText);
        var c = row.ColumnCount < 1 ? 1 : row.ColumnCount;
        return $"Paragraph ({Plural(lines, "line", "lines")}, {Plural(c, "column", "columns")})";
    }

    private static int CountNonEmptyLines(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text.Split(["\r\n", "\n", "\r"], StringSplitOptions.None).Count(s => !string.IsNullOrWhiteSpace(s));
    }

    private static string Plural(int n, string singular, string plural) => $"{n} {(n == 1 ? singular : plural)}";

    private void SetExtractionType(LyoPdfAnnotationResult row, PdfAnnotationExtractionType value)
    {
        row.ExtractionType = value;
        InvokeAsync(StateHasChanged);
    }

    private void SetYTolerance(LyoPdfAnnotationResult row, double value)
    {
        row.YTolerance = value;
        InvokeAsync(StateHasChanged);
    }

    private void SetKnownKeys(LyoPdfAnnotationResult row, IEnumerable<string> values)
    {
        row.KnownKeys = values.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        InvokeAsync(StateHasChanged);
    }

    private void SetTableHeaders(LyoPdfAnnotationResult row, IEnumerable<string> values)
    {
        row.TableHeaders = values.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (row.TableKeyColumnLabel is not null && !row.TableHeaders.Any(h => string.Equals(h, row.TableKeyColumnLabel, StringComparison.OrdinalIgnoreCase)))
            row.TableKeyColumnLabel = null;

        InvokeAsync(StateHasChanged);
    }

    private static IReadOnlyList<string> GetTableKeySelectedChips(LyoPdfAnnotationResult row)
    {
        var key = row.TableKeyColumnLabel;
        if (string.IsNullOrWhiteSpace(key) && row.TableHeaders.Count > 0)
            key = row.TableHeaders[0];

        if (string.IsNullOrWhiteSpace(key))
            return [];

        var match = row.TableHeaders.FirstOrDefault(h => string.Equals(h, key, StringComparison.OrdinalIgnoreCase));
        return match is null ? [] : [match];
    }

    private void SetTableKeyChipSelection(LyoPdfAnnotationResult row, IEnumerable<string> selected)
    {
        row.TableKeyColumnLabel = selected.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s))?.Trim();
        InvokeAsync(StateHasChanged);
    }

    private void SetKeyValueLayout(LyoPdfAnnotationResult row, PdfKeyValueLayout value)
    {
        row.KeyValueLayout = value;
        InvokeAsync(StateHasChanged);
    }

    private void SetColumnCount(LyoPdfAnnotationResult row, int value)
    {
        row.ColumnCount = value < 1 ? 1 : value;
        InvokeAsync(StateHasChanged);
    }

    private void SetInferBold(LyoPdfAnnotationResult row, bool on)
    {
        row.InferFormattingFlags = on ? row.InferFormattingFlags | PdfInferFormattingFlags.Bold : row.InferFormattingFlags & ~PdfInferFormattingFlags.Bold;
        InvokeAsync(StateHasChanged);
    }

    private void SetInferSemicolon(LyoPdfAnnotationResult row, bool on)
    {
        row.InferFormattingFlags = on ? row.InferFormattingFlags | PdfInferFormattingFlags.Semicolon : row.InferFormattingFlags & ~PdfInferFormattingFlags.Semicolon;
        if (on && string.IsNullOrWhiteSpace(row.KeyValueInferDelimiters))
            row.KeyValueInferDelimiters = ":;";

        InvokeAsync(StateHasChanged);
    }

    private IReadOnlyCollection<string> GetDelimiterSelectedValues(LyoPdfAnnotationResult row)
    {
        var list = new List<string>();
        foreach (var c in row.KeyValueInferDelimiters) {
            if (char.IsWhiteSpace(c) || char.IsControl(c))
                continue;

            var s = c.ToString();
            if (!list.Contains(s))
                list.Add(s);
        }

        if (list.Count != 0)
            return list;

        list.Add(":");
        list.Add(";");
        return list;
    }

    private IEnumerable<char> GetExtraDelimiterChars(LyoPdfAnnotationResult row)
    {
        var seen = new HashSet<char>();
        foreach (var c in row.KeyValueInferDelimiters) {
            if (char.IsWhiteSpace(c) || char.IsControl(c) || KvDelimiterPresetChars.Contains(c))
                continue;

            if (seen.Add(c))
                yield return c;
        }
    }

    private void SetDelimiterSelectedValues(LyoPdfAnnotationResult row, IEnumerable<string>? values)
    {
        var sel = values?.ToArray() ?? [];
        var ordered = new List<char>();
        foreach (var (val, _) in KvDelimiterSelectOptions) {
            if (val.Length == 1 && sel.Contains(val))
                ordered.Add(val[0]);
        }

        foreach (var s in sel) {
            if (string.IsNullOrEmpty(s) || s.Length != 1)
                continue;

            var c = s[0];
            if (char.IsWhiteSpace(c) || char.IsControl(c) || ordered.Contains(c))
                continue;

            if (KvDelimiterPresetChars.Contains(c))
                continue;

            ordered.Add(c);
        }

        row.KeyValueInferDelimiters = ordered.Count > 0 ? new([.. ordered]) : ":;";
        InvokeAsync(StateHasChanged);
    }

    private static string DelimiterSelectSummary(IEnumerable<string> selected)
    {
        var parts = selected.Select(static s => s.Length == 1 ? $"‘{s}’" : s).ToArray();
        return parts.Length == 0 ? "" : string.Join(" ", parts);
    }

    private string GetCustomDelimiterDraft(string rowKey) => _customDelimiterDraft.GetValueOrDefault(rowKey) ?? "";

    private void SetCustomDelimiterDraft(LyoPdfAnnotationResult row, string v)
    {
        _customDelimiterDraft[row.Key] = v;
        if (v.Length == 0) {
            InvokeAsync(StateHasChanged);
            return;
        }

        var c = v.Trim()[0];
        if (char.IsWhiteSpace(c) || char.IsControl(c))
            return;

        var current = row.KeyValueInferDelimiters;
        if (current.Contains(c)) {
            _customDelimiterDraft[row.Key] = "";
            InvokeAsync(StateHasChanged);
            return;
        }

        row.KeyValueInferDelimiters = current + c;
        _customDelimiterDraft[row.Key] = "";
        InvokeAsync(StateHasChanged);
    }

    private void SetInferUnderline(LyoPdfAnnotationResult row, bool on)
    {
        row.InferFormattingFlags = on ? row.InferFormattingFlags | PdfInferFormattingFlags.Underline : row.InferFormattingFlags & ~PdfInferFormattingFlags.Underline;
        InvokeAsync(StateHasChanged);
    }

    private void SetTableKeyColumn(LyoPdfAnnotationResult row, string? value)
    {
        row.TableKeyColumnLabel = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        InvokeAsync(StateHasChanged);
    }
}
