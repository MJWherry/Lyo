using System.Text;
using Lyo.Common.Records;

namespace Lyo.Profanity.Models;

/// <summary>Options for the file-based profanity filter service. Extends <see cref="ProfanityFilterOptions" /> with file-specific settings.</summary>
public class FileProfanityFilterOptions : ProfanityFilterOptions
{
    /// <summary>Configuration section name for options binding. Default: "ProfanityFilter".</summary>
    public new const string SectionName = "ProfanityFilter";

    /// <summary>
    /// Path to the JSON file containing profane words as an array, e.g. ["word1", "word2", ...]. Used when WordsByLanguage is empty or does not contain an entry for the
    /// configured Language.
    /// </summary>
    public string WordsFilePath { get; set; } = string.Empty;

    /// <summary>
    /// URL to load a JSON word list from (e.g. https://raw.githubusercontent.com/.../words.json). Words from URL are merged with WordsFilePath and AdditionalWords when both are
    /// configured. Used when WordsByLanguage is empty or does not contain an entry for the configured Language.
    /// </summary>
    public string WordsUrl { get; set; } = string.Empty;

    /// <summary>
    /// Per-language word sources. Key is BCP 47 (e.g. "en-US"), ISO 639-1 (e.g. "en"), or ISO 639-3 (e.g. "eng"). When configured, the Language option selects which entry to
    /// use. Enables Filter(input, language) overloads to filter by different languages.
    /// </summary>
    public Dictionary<string, LanguageWordSourceConfig>? WordsByLanguage { get; set; }

    /// <summary>Encoding used when reading the words file or URL response. Default: UTF-8.</summary>
    public Encoding Encoding { get; set; } = Encoding.UTF8;

    /// <summary>Whether to reload the words file when RefreshWords is called. If false, file is loaded once at construction.</summary>
    public bool AllowRefresh { get; set; } = true;

    /// <summary>Gets the word source config for the specified language, or the default WordsFilePath/WordsUrl if not found in WordsByLanguage.</summary>
    internal (string Path, string Url) GetWordSourceForLanguage(LanguageCodeInfo language)
    {
        if (WordsByLanguage is { Count: > 0 }) {
            bool HasSource(LanguageWordSourceConfig c) => !string.IsNullOrWhiteSpace(c.WordsFilePath) || !string.IsNullOrWhiteSpace(c.WordsUrl);

            if (WordsByLanguage.TryGetValue(language.Bcp47, out var config) && HasSource(config))
                return (config.WordsFilePath ?? string.Empty, config.WordsUrl ?? string.Empty);

            if (!string.IsNullOrWhiteSpace(language.Iso6391) && WordsByLanguage!.TryGetValue(language.Iso6391!, out config) && HasSource(config))
                return (config.WordsFilePath ?? string.Empty, config.WordsUrl ?? string.Empty);

            if (!string.IsNullOrWhiteSpace(language.Iso6393) && WordsByLanguage!.TryGetValue(language.Iso6393!, out config) && HasSource(config))
                return (config.WordsFilePath ?? string.Empty, config.WordsUrl ?? string.Empty);
        }

        return (WordsFilePath, WordsUrl);
    }
}