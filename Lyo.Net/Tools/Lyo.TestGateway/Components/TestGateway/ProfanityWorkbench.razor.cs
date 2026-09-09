using System.Diagnostics;
using System.Net.Http.Json;
using Lyo.Common.Metadata.Records;
using Lyo.Profanity;
using Lyo.Profanity.Models;
using Lyo.TestGateway.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using MudBlazor;
using static Microsoft.AspNetCore.Components.Web.RenderMode;

namespace Lyo.TestGateway.Components.TestGateway;

public partial class ProfanityWorkbench
{
    private readonly IReadOnlyList<LanguageCodeInfo> _languages = LanguageCodeInfo.All.Where(i => i != LanguageCodeInfo.Unknown).OrderBy(i => i.Description).ToArray();
    private readonly IReadOnlyList<ProfanityReplacementStrategy> _replacementStrategies = Enum.GetValues<ProfanityReplacementStrategy>();

    private bool _busy;
    private bool _caseSensitive;
    private bool _matchWholeWordsOnly;
    private bool? _containsResult;
    private ProfanityFilterResult? _filterResult;
    private FileProfanityFilterService? _cachedService;
    private List<string> _additionalWords = ["badword"];
    private string _inputText = "This sample badword should be filtered.";
    private List<string> _excludedWords = [];
    private string? _serviceCacheKey;
    private string _replacementCharText = "*";
    private string _replacementWord = "***";
    private ProfanityReplacementStrategy _replacementStrategy = ProfanityReplacementStrategy.Mask;
    private string _selectedLanguage = LanguageCodeInfo.EnUs.Bcp47;
    private string _wordsFilePath = string.Empty;
    private string _wordsUrl = string.Empty;

    private LanguageCodeInfo CurrentLanguage => LanguageCodeInfo.FromBcp47(_selectedLanguage);

    protected override void OnInitialized() => ResetToDefaults();

    private async Task FilterAsync()
    {
        if (string.IsNullOrWhiteSpace(_inputText)) {
            SetStatus("Enter text to filter.", Severity.Warning);
            return;
        }

        _busy = true;
        try {
            var service = GetOrBuildService();
            var result = await service.FilterAsync(_inputText, CurrentLanguage);
            _filterResult = result;
            _containsResult = result.HasProfanity;
            SetStatus(result.HasProfanity ? $"Detected {result.Matches.Count} match(es)." : "No profanity detected.", result.HasProfanity ? Severity.Warning : Severity.Success);
        }
        catch (Exception ex) {
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task CheckAsync()
    {
        if (string.IsNullOrWhiteSpace(_inputText)) {
            SetStatus("Enter text to check.", Severity.Warning);
            return;
        }

        _busy = true;
        try {
            var service = GetOrBuildService();
            var t = Stopwatch.StartNew();
            _containsResult = await service.ContainsProfanityAsync(_inputText, CurrentLanguage);
            t.Stop();
            SetStatus(_containsResult == true ? "Profanity detected." : "No profanity detected.", _containsResult == true ? Severity.Warning : Severity.Success);
        }
        catch (Exception ex) {
            SetStatus(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private void ResetToDefaults()
    {
        _selectedLanguage = ProfanityFilterOptions.ResolveLanguageCode(BaseOptions.Language).Bcp47;
        _replacementStrategy = BaseOptions.ReplacementStrategy;
        _replacementCharText = BaseOptions.ReplacementChar.ToString();
        _replacementWord = BaseOptions.ReplacementWord;
        _caseSensitive = BaseOptions.CaseSensitive;
        _matchWholeWordsOnly = BaseOptions.MatchWholeWordsOnly;
        _additionalWords = BaseOptions.AdditionalWords.Count > 0 ? NormalizeWords(BaseOptions.AdditionalWords.OrderBy(i => i, StringComparer.OrdinalIgnoreCase)) : ["badword"];
        _excludedWords = NormalizeWords(BaseOptions.ExcludedWords.OrderBy(i => i, StringComparer.OrdinalIgnoreCase));
        _wordsFilePath = BaseOptions.WordsFilePath;
        _wordsUrl = BaseOptions.WordsUrl;
        _filterResult = null;
        _containsResult = null;
        _cachedService = null;
        _serviceCacheKey = null;
        ClearStatus();
    }

    private FileProfanityFilterService GetOrBuildService()
    {
        var cacheKey = BuildServiceCacheKey();
        if (_cachedService != null && string.Equals(_serviceCacheKey, cacheKey, StringComparison.Ordinal))
            return _cachedService;

        var options = new FileProfanityFilterOptions {
            Language = _selectedLanguage,
            ReplacementStrategy = _replacementStrategy,
            ReplacementChar = string.IsNullOrWhiteSpace(_replacementCharText) ? '*' : _replacementCharText.Trim()[0],
            ReplacementWord = string.IsNullOrWhiteSpace(_replacementWord) ? "***" : _replacementWord.Trim(),
            CaseSensitive = _caseSensitive,
            MatchWholeWordsOnly = _matchWholeWordsOnly,
            EnableMetrics = BaseOptions.EnableMetrics,
            AllowRefresh = BaseOptions.AllowRefresh,
            Encoding = BaseOptions.Encoding,
            WordsFilePath = string.IsNullOrWhiteSpace(_wordsFilePath) ? BaseOptions.WordsFilePath : _wordsFilePath.Trim(),
            WordsUrl = string.IsNullOrWhiteSpace(_wordsUrl) ? BaseOptions.WordsUrl : _wordsUrl.Trim(),
            WordsByLanguage = CloneWordsByLanguage(BaseOptions.WordsByLanguage),
            AdditionalWords = ToWordSet(_additionalWords),
            ExcludedWords = ToWordSet(_excludedWords)
        };

        // Leave the page usable if appsettings has no word source yet.
        if (options.AdditionalWords.Count == 0 && string.IsNullOrWhiteSpace(options.WordsFilePath) && string.IsNullOrWhiteSpace(options.WordsUrl) && (options.WordsByLanguage == null || options.WordsByLanguage.Count == 0)) {
            options.AdditionalWords = ["badword"];
        }

        _cachedService = new(options);
        _serviceCacheKey = cacheKey;
        return _cachedService;
    }

    private Task OnAdditionalWordsChanged(IEnumerable<string> values)
    {
        _additionalWords = NormalizeWords(values);
        return Task.CompletedTask;
    }

    private Task OnExcludedWordsChanged(IEnumerable<string> values)
    {
        _excludedWords = NormalizeWords(values);
        return Task.CompletedTask;
    }

    private static List<string> NormalizeWords(IEnumerable<string>? values)
    {
        if (values == null)
            return [];

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return values.Select(i => i?.Trim()).Where(i => !string.IsNullOrWhiteSpace(i)).Cast<string>().Where(seen.Add).ToList();
    }

    private static HashSet<string> ToWordSet(IEnumerable<string>? values) => NormalizeWords(values).ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static Dictionary<string, LanguageWordSourceConfig>? CloneWordsByLanguage(Dictionary<string, LanguageWordSourceConfig>? source)
    {
        if (source == null || source.Count == 0)
            return null;

        return source.ToDictionary(pair => pair.Key, pair => new LanguageWordSourceConfig { WordsFilePath = pair.Value.WordsFilePath, WordsUrl = pair.Value.WordsUrl }, StringComparer.OrdinalIgnoreCase);
    }

    private string BuildServiceCacheKey()
    {
        var wordsByLanguage = BaseOptions.WordsByLanguage is { Count: > 0 } ? string.Join("|", BaseOptions.WordsByLanguage.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).Select(pair => $"{pair.Key}:{pair.Value.WordsFilePath}:{pair.Value.WordsUrl}")) : string.Empty;
        return string.Join("||", _selectedLanguage, _replacementStrategy, string.IsNullOrWhiteSpace(_replacementCharText) ? "*" : _replacementCharText.Trim(), string.IsNullOrWhiteSpace(_replacementWord) ? "***" : _replacementWord.Trim(), _caseSensitive, _matchWholeWordsOnly, BaseOptions.EnableMetrics, BaseOptions.AllowRefresh, BaseOptions.Encoding.WebName, string.IsNullOrWhiteSpace(_wordsFilePath) ? BaseOptions.WordsFilePath : _wordsFilePath.Trim(), string.IsNullOrWhiteSpace(_wordsUrl) ? BaseOptions.WordsUrl : _wordsUrl.Trim(), wordsByLanguage, NormalizeWordSet(_additionalWords), NormalizeWordSet(_excludedWords));
    }

    private static string NormalizeWordSet(IEnumerable<string>? values) => string.Join("|", ToWordSet(values).OrderBy(word => word, StringComparer.OrdinalIgnoreCase));

    private string FormatContainsResult()
        => _containsResult switch {
            true => "Yes",
            false => "No",
            var _ => "Not run"
        };
}
