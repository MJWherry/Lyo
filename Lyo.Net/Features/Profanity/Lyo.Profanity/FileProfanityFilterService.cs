using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Lyo.Common.Records;
using Lyo.Metrics;
using Lyo.Profanity.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Profanity;

/// <summary>
/// File-based profanity filter service. Loads from: - Structured JSON: [{ "id", "match", "tags", "severity", "exceptions" }] where match is a regex pattern. - Plain JSON
/// array: ["word1", "word2", ...] converted to default entries (id=match=word, tags=[], severity=1, exceptions=[]). - Plain newline-separated text: one word per line, same defaults.
/// Supports per-language word lists via Language and WordsByLanguage configuration.
/// </summary>
public sealed class FileProfanityFilterService : IProfanityFilterService
{
    private readonly Dictionary<LanguageCodeInfo, List<ProfanityEntry>> _entriesByLanguage = new();
    private readonly HttpClient? _httpClient;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private readonly ILogger<FileProfanityFilterService> _logger;
    private readonly IMetrics _metrics;
    private readonly FileProfanityFilterOptions _options;
    private readonly ConcurrentDictionary<string, Regex> _regexCache = new();
    private List<ProfanityEntry> _entries = new();
    private bool _loaded;

    public FileProfanityFilterService(
        FileProfanityFilterOptions? options = null,
        ILogger<FileProfanityFilterService>? logger = null,
        IMetrics? metrics = null,
        HttpClient? httpClient = null)
    {
        _options = options ?? new FileProfanityFilterOptions();
        _logger = logger ?? NullLogger<FileProfanityFilterService>.Instance;
        _metrics = _options.EnableMetrics && metrics != null ? metrics : NullMetrics.Instance;
        _httpClient = httpClient;
        LoadWords();
    }

    /// <inheritdoc />
    public ProfanityFilterResult Filter(string? input, CancellationToken ct = default) => Filter(input, _options.GetLanguageCode(), ct);

    /// <inheritdoc />
    public ProfanityFilterResult Filter(string? input, LanguageCodeInfo language, CancellationToken ct = default)
    {
        using var timer = _metrics.StartTimer(Constants.Metrics.FilterDuration);
        var text = input ?? string.Empty;
        if (text.Length == 0) {
            _metrics.IncrementCounter(Constants.Metrics.FilterSuccess);
            return new(text, false, []);
        }

        ct.ThrowIfCancellationRequested();
        var entries = GetEntries(language, ct);
        if (entries.Count == 0) {
            _logger.LogDebug("No profanity entries loaded for {Language}; returning input unchanged", language.Bcp47);
            _metrics.IncrementCounter(Constants.Metrics.FilterSuccess);
            _metrics.RecordGauge(Constants.Metrics.FilterInputLength, text.Length);
            return new(text, false, []);
        }

        var stopwatch = Stopwatch.StartNew();
        try {
            var matches = FindMatches(text, entries, ct);
            if (matches.Count == 0) {
                stopwatch.Stop();
                _metrics.IncrementCounter(Constants.Metrics.FilterSuccess);
                _metrics.RecordGauge(Constants.Metrics.FilterInputLength, text.Length);
                _metrics.RecordHistogram(Constants.Metrics.FilterDurationMs, stopwatch.ElapsedMilliseconds);
                return new(text, false, []);
            }

            var filtered = ApplyReplacements(text, matches);
            stopwatch.Stop();
            _metrics.IncrementCounter(Constants.Metrics.FilterSuccess);
            _metrics.RecordGauge(Constants.Metrics.FilterInputLength, text.Length);
            _metrics.RecordGauge(Constants.Metrics.FilterMatchCount, matches.Count);
            _metrics.RecordHistogram(Constants.Metrics.FilterDurationMs, stopwatch.ElapsedMilliseconds);
            return new(filtered, true, matches);
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _metrics.IncrementCounter(Constants.Metrics.FilterFailure);
            _metrics.RecordError(Constants.Metrics.FilterDuration, ex);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<ProfanityFilterResult> FilterAsync(string? input, CancellationToken ct = default) => FilterAsync(input, _options.GetLanguageCode(), ct);

    /// <inheritdoc />
    public async Task<ProfanityFilterResult> FilterAsync(string? input, LanguageCodeInfo language, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        using var timer = _metrics.StartTimer(Constants.Metrics.FilterDuration);
        var text = input ?? string.Empty;
        if (text.Length == 0) {
            _metrics.IncrementCounter(Constants.Metrics.FilterSuccess);
            return new(text, false, []);
        }

        var entries = await GetEntriesAsync(language, ct).ConfigureAwait(false);
        if (entries.Count == 0) {
            _logger.LogDebug("No profanity entries loaded for {Language}; returning input unchanged", language.Bcp47);
            _metrics.IncrementCounter(Constants.Metrics.FilterSuccess);
            _metrics.RecordGauge(Constants.Metrics.FilterInputLength, text.Length);
            return new(text, false, []);
        }

        ct.ThrowIfCancellationRequested();
        var stopwatch = Stopwatch.StartNew();
        try {
            var matches = FindMatches(text, entries, ct);
            if (matches.Count == 0) {
                stopwatch.Stop();
                _metrics.IncrementCounter(Constants.Metrics.FilterSuccess);
                _metrics.RecordGauge(Constants.Metrics.FilterInputLength, text.Length);
                _metrics.RecordHistogram(Constants.Metrics.FilterDurationMs, stopwatch.ElapsedMilliseconds);
                return new(text, false, []);
            }

            var filtered = ApplyReplacements(text, matches);
            stopwatch.Stop();
            _metrics.IncrementCounter(Constants.Metrics.FilterSuccess);
            _metrics.RecordGauge(Constants.Metrics.FilterInputLength, text.Length);
            _metrics.RecordGauge(Constants.Metrics.FilterMatchCount, matches.Count);
            _metrics.RecordHistogram(Constants.Metrics.FilterDurationMs, stopwatch.ElapsedMilliseconds);
            return new(filtered, true, matches);
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _metrics.IncrementCounter(Constants.Metrics.FilterFailure);
            _metrics.RecordError(Constants.Metrics.FilterDuration, ex);
            throw;
        }
    }

    /// <inheritdoc />
    public bool ContainsProfanity(string? input, CancellationToken ct = default) => ContainsProfanity(input, _options.GetLanguageCode(), ct);

    /// <inheritdoc />
    public bool ContainsProfanity(string? input, LanguageCodeInfo language, CancellationToken ct = default)
    {
        using var timer = _metrics.StartTimer(Constants.Metrics.ContainsProfanityDuration);
        var text = input ?? string.Empty;
        if (text.Length == 0) {
            _metrics.IncrementCounter(Constants.Metrics.ContainsProfanityCalls);
            return false;
        }

        ct.ThrowIfCancellationRequested();
        var entries = GetEntries(language, ct);
        var hasProfanity = entries.Count > 0 && ContainsAnyMatch(text, entries, ct);
        _metrics.IncrementCounter(Constants.Metrics.ContainsProfanityCalls);
        if (hasProfanity)
            _metrics.IncrementCounter(Constants.Metrics.ContainsProfanityPositive);

        return hasProfanity;
    }

    /// <inheritdoc />
    public Task<bool> ContainsProfanityAsync(string? input, CancellationToken ct = default) => ContainsProfanityAsync(input, _options.GetLanguageCode(), ct);

    /// <inheritdoc />
    public async Task<bool> ContainsProfanityAsync(string? input, LanguageCodeInfo language, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        using var timer = _metrics.StartTimer(Constants.Metrics.ContainsProfanityDuration);
        var text = input ?? string.Empty;
        if (text.Length == 0) {
            _metrics.IncrementCounter(Constants.Metrics.ContainsProfanityCalls);
            return false;
        }

        var entries = await GetEntriesAsync(language, ct).ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();
        var hasProfanity = entries.Count > 0 && ContainsAnyMatch(text, entries, ct);
        _metrics.IncrementCounter(Constants.Metrics.ContainsProfanityCalls);
        if (hasProfanity)
            _metrics.IncrementCounter(Constants.Metrics.ContainsProfanityPositive);

        return hasProfanity;
    }

    /// <inheritdoc />
    public void RefreshWords(CancellationToken ct = default)
    {
        if (!_options.AllowRefresh) {
            _logger.LogDebug("Refresh disabled; skipping reload");
            return;
        }

        ct.ThrowIfCancellationRequested();
        using var timer = _metrics.StartTimer(Constants.Metrics.RefreshDuration);
        var stopwatch = Stopwatch.StartNew();
        try {
            _loadLock.Wait(ct);
            try {
                _entriesByLanguage.Clear();
                _regexCache.Clear();
            }
            finally {
                _loadLock.Release();
            }

            LoadWords();
            stopwatch.Stop();
            _metrics.IncrementCounter(Constants.Metrics.RefreshSuccess);
            _metrics.RecordGauge(Constants.Metrics.RefreshWordCount, GetEntries(null, ct).Count);
            _metrics.RecordHistogram(Constants.Metrics.RefreshDurationMs, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _metrics.IncrementCounter(Constants.Metrics.RefreshFailure);
            _metrics.RecordError(Constants.Metrics.RefreshDuration, ex);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RefreshWordsAsync(CancellationToken ct = default)
    {
        if (!_options.AllowRefresh) {
            _logger.LogDebug("Refresh disabled; skipping reload");
            return;
        }

        ct.ThrowIfCancellationRequested();
        using var timer = _metrics.StartTimer(Constants.Metrics.RefreshDuration);
        var stopwatch = Stopwatch.StartNew();
        try {
            await _loadLock.WaitAsync(ct).ConfigureAwait(false);
            try {
                _entriesByLanguage.Clear();
                _regexCache.Clear();
            }
            finally {
                _loadLock.Release();
            }

            await LoadWordsAsync(ct).ConfigureAwait(false);
            stopwatch.Stop();
            _metrics.IncrementCounter(Constants.Metrics.RefreshSuccess);
            _metrics.RecordGauge(Constants.Metrics.RefreshWordCount, GetEntries(null, ct).Count);
            _metrics.RecordHistogram(Constants.Metrics.RefreshDurationMs, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex) {
            stopwatch.Stop();
            _metrics.IncrementCounter(Constants.Metrics.RefreshFailure);
            _metrics.RecordError(Constants.Metrics.RefreshDuration, ex);
            throw;
        }
    }

    private List<ProfanityEntry> GetEntries(LanguageCodeInfo? language = null, CancellationToken ct = default)
    {
        var lang = language ?? _options.GetLanguageCode();
        _loadLock.Wait(ct);
        try {
            if (lang.Equals(_options.GetLanguageCode()))
                return _entries;

            if (_entriesByLanguage.TryGetValue(lang, out var cached))
                return cached;

            var combined = LoadEntriesForLanguage(lang);
            _entriesByLanguage[lang] = combined;
            return combined;
        }
        finally {
            _loadLock.Release();
        }
    }

    private async Task<List<ProfanityEntry>> GetEntriesAsync(LanguageCodeInfo? language, CancellationToken ct)
    {
        var lang = language ?? _options.GetLanguageCode();
        await _loadLock.WaitAsync(ct).ConfigureAwait(false);
        try {
            if (lang.Equals(_options.GetLanguageCode()))
                return _entries;

            if (_entriesByLanguage.TryGetValue(lang, out var cached))
                return cached;

            var combined = await LoadEntriesForLanguageAsync(lang, ct).ConfigureAwait(false);
            _entriesByLanguage[lang] = combined;
            return combined;
        }
        finally {
            _loadLock.Release();
        }
    }

    private void LoadWords()
    {
        _loadLock.Wait();
        try {
            var defaultLang = _options.GetLanguageCode();
            _entries = LoadEntriesForLanguage(defaultLang);
            _loaded = true;
        }
        finally {
            _loadLock.Release();
        }
    }

    private async Task LoadWordsAsync(CancellationToken ct)
    {
        await _loadLock.WaitAsync(ct).ConfigureAwait(false);
        try {
            var defaultLang = _options.GetLanguageCode();
            _entries = await LoadEntriesForLanguageAsync(defaultLang, ct).ConfigureAwait(false);
            _loaded = true;
        }
        finally {
            _loadLock.Release();
        }
    }

    private List<ProfanityEntry> LoadEntriesForLanguage(LanguageCodeInfo language)
    {
        var combined = new Dictionary<string, ProfanityEntry>(_options.StringComparison == StringComparison.Ordinal ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
        var (path, url) = _options.GetWordSourceForLanguage(language);
        if (!string.IsNullOrWhiteSpace(path)) {
            try {
                if (!File.Exists(path))
                    _logger.LogWarning("Profanity words file not found: {FilePath}", path);
                else {
                    var content = File.ReadAllText(path, _options.Encoding);
                    AddEntriesFromContent(content, combined, _options);
                    _logger.LogInformation("Loaded profanity entries from {FilePath} for {Language}", path, language.Bcp47);
                }
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to load profanity words from {FilePath}", path);
                if (!_loaded && language.Equals(_options.GetLanguageCode()))
                    throw;
            }
        }

        if (!string.IsNullOrWhiteSpace(url)) {
            try {
                var client = _httpClient ?? new HttpClient();
                var content = client.GetStringAsync(url).GetAwaiter().GetResult();
                var countBefore = combined.Count;
                AddEntriesFromContent(content, combined, _options);
                _logger.LogInformation("Loaded {Count} profanity entries from {Url} for {Language}", combined.Count - countBefore, url, language.Bcp47);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to load profanity words from {Url}", url);
                if (!_loaded && language.Equals(_options.GetLanguageCode()))
                    throw;
            }
        }

        foreach (var word in _options.AdditionalWords) {
            if (!string.IsNullOrEmpty(word) && !_options.ExcludedWords.Contains(word))
                combined[word] = ProfanityEntry.FromPlainWord(word);
        }

        foreach (var excl in _options.ExcludedWords)
            combined.Remove(excl);

        return [..combined.Values];
    }

    private async Task<List<ProfanityEntry>> LoadEntriesForLanguageAsync(LanguageCodeInfo language, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var combined = new Dictionary<string, ProfanityEntry>(_options.StringComparison == StringComparison.Ordinal ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
        var (path, url) = _options.GetWordSourceForLanguage(language);
        if (!string.IsNullOrWhiteSpace(path)) {
            try {
                if (!File.Exists(path))
                    _logger.LogWarning("Profanity words file not found: {FilePath}", path);
                else {
                    var content = await ReadAllTextAsync(path, _options.Encoding, ct).ConfigureAwait(false);
                    AddEntriesFromContent(content, combined, _options);
                    _logger.LogInformation("Loaded profanity entries from {FilePath} for {Language}", path, language.Bcp47);
                }
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to load profanity words from {FilePath}", path);
                if (!_loaded && language.Equals(_options.GetLanguageCode()))
                    throw;
            }
        }

        if (!string.IsNullOrWhiteSpace(url)) {
            try {
                var client = _httpClient ?? new HttpClient();
                string content;
#if NET
                content = await client.GetStringAsync(url, ct).ConfigureAwait(false);
#else
                using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseContentRead, ct).ConfigureAwait(false)) {
                    response.EnsureSuccessStatusCode();
                    content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
#endif
                var countBefore = combined.Count;
                AddEntriesFromContent(content, combined, _options);
                _logger.LogInformation("Loaded {Count} profanity entries from {Url} for {Language}", combined.Count - countBefore, url, language.Bcp47);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to load profanity words from {Url}", url);
                if (!_loaded && language.Equals(_options.GetLanguageCode()))
                    throw;
            }
        }

        foreach (var word in _options.AdditionalWords) {
            if (!string.IsNullOrEmpty(word) && !_options.ExcludedWords.Contains(word))
                combined[word] = ProfanityEntry.FromPlainWord(word);
        }

        foreach (var excl in _options.ExcludedWords)
            combined.Remove(excl);

        return [..combined.Values];
    }

    private static async Task<string> ReadAllTextAsync(string path, Encoding encoding, CancellationToken ct)
    {
#if NET
        return await File.ReadAllTextAsync(path, encoding, ct).ConfigureAwait(false);
#else
        ct.ThrowIfCancellationRequested();
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        using var sr = new StreamReader(fs, encoding);
        return await sr.ReadToEndAsync().ConfigureAwait(false);

#endif
    }

    private static void AddEntriesFromContent(string content, Dictionary<string, ProfanityEntry> combined, FileProfanityFilterOptions options)
    {
        var trimmed = content?.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return;

        // Try structured JSON array: [{ "id": "...", "match": "...", ... }, ...]
        try {
            using var doc = JsonDocument.Parse(trimmed!);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Array) {
                foreach (var el in root.EnumerateArray()) {
                    if (el.ValueKind != JsonValueKind.Object)
                        continue;

                    var id = el.TryGetProperty("id", out var idProp) ? idProp.GetString()?.Trim() : null;
                    var match = el.TryGetProperty("match", out var matchProp) ? matchProp.GetString()?.Trim() : null;
                    if (string.IsNullOrEmpty(id))
                        id = match;

                    if (string.IsNullOrEmpty(match))
                        continue;

                    var idOrMatch = (id ?? match)!;
                    if (options.ExcludedWords.Contains(idOrMatch))
                        continue;

                    var tags = el.TryGetProperty("tags", out var tagsProp) && tagsProp.ValueKind == JsonValueKind.Array
                        ? tagsProp.EnumerateArray().Select(e => e.GetString() ?? string.Empty).Where(s => s.Length > 0).ToArray()
                        : [];

                    var severity = el.TryGetProperty("severity", out var sevProp) ? sevProp.GetInt32() : 1;
                    var exceptions = el.TryGetProperty("exceptions", out var excProp) && excProp.ValueKind == JsonValueKind.Array
                        ? excProp.EnumerateArray().Select(e => e.GetString() ?? string.Empty).Where(s => s.Length > 0).ToArray()
                        : [];

                    var isLiteral = el.TryGetProperty("literal", out var litProp) && litProp.GetBoolean();
                    combined[idOrMatch] = new(idOrMatch, match!, tags, severity, exceptions, isLiteral);
                }

                return;
            }
        }
        catch (JsonException) { /* fall through to plain formats */
        }

        // Try plain JSON array of strings: ["word1", "word2", ...]
        try {
            var words = JsonSerializer.Deserialize<string[]>(trimmed!);
            if (words != null) {
                foreach (var word in words) {
                    var w = word.Trim() ?? string.Empty;
                    if (string.IsNullOrEmpty(w) || options.ExcludedWords.Contains(w))
                        continue;

                    combined[w] = ProfanityEntry.FromPlainWord(w);
                }

                return;
            }
        }
        catch (JsonException) { }

        // Plain newline-separated list
        foreach (var line in trimmed!.Split('\n', '\r')) {
            var w = line.Trim();
            if (string.IsNullOrEmpty(w) || options.ExcludedWords.Contains(w))
                continue;

            combined[w] = ProfanityEntry.FromPlainWord(w);
        }
    }

    /// <summary>Returns true at first match (early exit for ContainsProfanity).</summary>
    private bool ContainsAnyMatch(string input, IReadOnlyList<ProfanityEntry> entries, CancellationToken ct)
    {
        var regexOpts = RegexOptions.Compiled | (_options.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
        foreach (var entry in entries) {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(entry.Match))
                continue;

            var matchPattern = entry.IsLiteral ? Regex.Escape(entry.Match) : entry.Match;
            var pattern = _options.MatchWholeWordsOnly ? $@"\b(?:{matchPattern})\b" : matchPattern;
            var regex = GetOrCreateRegex(pattern, regexOpts);
            if (regex == null)
                continue;

            foreach (Match m in regex.Matches(input)) {
                if (!IsException(input, m.Index, m.Length, entry.Exceptions))
                    return true;
            }
        }

        return false;
    }

    private IReadOnlyList<ProfanityMatch> FindMatches(string input, IReadOnlyList<ProfanityEntry> entries, CancellationToken ct)
    {
        var matches = new List<ProfanityMatch>();
        var regexOpts = RegexOptions.Compiled | (_options.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
        foreach (var entry in entries) {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(entry.Match))
                continue;

            var matchPattern = entry.IsLiteral ? Regex.Escape(entry.Match) : entry.Match;
            var pattern = _options.MatchWholeWordsOnly ? $@"\b(?:{matchPattern})\b" : matchPattern;
            var regex = GetOrCreateRegex(pattern, regexOpts);
            if (regex == null) {
                _logger.LogWarning("Invalid regex pattern for entry {Id}: {Match}", entry.Id, entry.Match);
                continue;
            }

            foreach (Match m in regex.Matches(input)) {
                if (IsException(input, m.Index, m.Length, entry.Exceptions))
                    continue;

                matches.Add(new(m.Index, m.Length, m.Value, entry));
            }
        }

        return matches.OrderBy(m => m.Index).ToList();
    }

    private Regex? GetOrCreateRegex(string pattern, RegexOptions options)
    {
        var key = pattern + "\uffff" + (uint)options;
        if (_regexCache.TryGetValue(key, out var cached))
            return cached;

        try {
            var regex = new Regex(pattern, options);
            _regexCache[key] = regex;
            return regex;
        }
        catch (ArgumentException) {
            return null;
        }
    }

    private static bool IsException(string input, int matchStart, int matchLength, IReadOnlyList<string> exceptions)
    {
        if (exceptions.Count == 0)
            return false;

        var word = GetContainingWord(input, matchStart, matchLength);
        var comparison = StringComparison.OrdinalIgnoreCase;
        foreach (var exc in exceptions) {
            if (string.IsNullOrEmpty(exc))
                continue;

            if (exc.StartsWith("*", StringComparison.Ordinal) && exc.Length > 1) {
                var suffix = exc.Substring(1);
                if (word.EndsWith(suffix, comparison))
                    return true;
            }
            else if (exc.EndsWith("*", StringComparison.Ordinal) && exc.Length > 1) {
                var prefix = exc.Substring(0, exc.Length - 1);
                if (word.StartsWith(prefix, comparison))
                    return true;
            }
            else if (word.Equals(exc, comparison))
                return true;
        }

        return false;
    }

    private static string GetContainingWord(string input, int matchStart, int matchLength)
    {
        var start = matchStart;
        while (start > 0 && char.IsLetterOrDigit(input[start - 1]))
            start--;

        var end = matchStart + matchLength;
        while (end < input.Length && char.IsLetterOrDigit(input[end]))
            end++;

        return input.Substring(start, end - start);
    }

    private string ApplyReplacements(string input, IReadOnlyList<ProfanityMatch> matches)
    {
        if (_options.ReplacementStrategy == ProfanityReplacementStrategy.DetectOnly)
            return input;

        var str = _options.ReplacementStrategy;
        var c = _options.ReplacementChar;
        var word = _options.ReplacementWord;
        if (str == ProfanityReplacementStrategy.Remove) {
            var sb = new StringBuilder(input.Length);
            var lastEnd = 0;
            foreach (var m in matches) {
                if (m.Index > lastEnd)
                    sb.Append(input, lastEnd, m.Index - lastEnd);

                lastEnd = Math.Max(lastEnd, m.Index + m.Length);
            }

            if (lastEnd < input.Length)
                sb.Append(input, lastEnd, input.Length - lastEnd);

            return sb.ToString();
        }

        if (str == ProfanityReplacementStrategy.ReplaceWithWord) {
            var rw = word ?? string.Empty;
            var sb = new StringBuilder(input.Length);
            var pos = 0;
            foreach (var m in matches.OrderBy(x => x.Index)) {
                if (m.Index < pos)
                    continue;

                sb.Append(input, pos, m.Index - pos);
                sb.Append(rw);
                pos = m.Index + m.Length;
            }

            sb.Append(input, pos, input.Length - pos);
            return sb.ToString();
        }

        var result = input.ToCharArray();
        foreach (var m in matches) {
            var replacement = str switch {
                ProfanityReplacementStrategy.ReplaceWithChar => new(c, m.Length),
                ProfanityReplacementStrategy.Mask => new('*', m.Length),
                ProfanityReplacementStrategy.PreserveBoundary when m.Length <= 2 => new(c, m.Length),
                ProfanityReplacementStrategy.PreserveBoundary => input[m.Index] + new string(c, m.Length - 2) + input[m.Index + m.Length - 1],
                var _ => new(c, m.Length)
            };

            for (var i = 0; i < replacement.Length && m.Index + i < result.Length; i++)
                result[m.Index + i] = replacement[i];
        }

        return new(result);
    }
}