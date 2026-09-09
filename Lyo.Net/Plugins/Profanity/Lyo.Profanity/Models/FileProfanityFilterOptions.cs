using System.Text;
using Lyo.Common.Metadata.Records;

namespace Lyo.Profanity.Models;

/// <summary>Settings for the file-based profanity filter service. Extends <see cref="ProfanityFilterOptions" /> with file-specific settings.</summary>
public class FileProfanityFilterOptions : ProfanityFilterOptions
{
    /// <summary>Options binding. Default: "ProfanityFilter" configuration section name.</summary>
    public new const string SectionName = "ProfanityFilter";

    /// <summary>
    /// JSON file of profane words as an array, for example ["word1", "word2", ...]. Used when WordsByLanguage is empty or has no entry for the
    /// configured Language value.
    /// </summary>
    public string WordsFilePath { get; set; } = string.Empty;

    /// <summary>
    /// URL of a JSON word list (for example https://raw.githubusercontent.com/.../words.json). URL words merge with WordsFilePath and AdditionalWords when both are
    /// set. Used when WordsByLanguage is empty or has no entry for the configured Language.
    /// </summary>
    public string WordsUrl { get; set; } = string.Empty;

    /// <summary>
    /// Word sources by language. Keys are BCP 47 (for example "en-US"), ISO 639-1 (for example "en"), or ISO 639-3 (for example "eng"). Language picks which entry to
    /// use. Filter(input, language) overloads can then pick another language.
    /// </summary>
    public Dictionary<string, LanguageWordSourceConfig>? WordsByLanguage { get; set; }

    /// <summary>Text encoding when reading the words file or URL response. Default: UTF-8.</summary>
    public Encoding Encoding { get; set; } = Encoding.UTF8;

    /// <summary>True when to reload the words file when RefreshWords is called. If false, file is loaded once at construction.</summary>
    public bool AllowRefresh { get; set; } = true;

    /// <summary>Returns the word source config for the specified language, or the default WordsFilePath/WordsUrl if not found in WordsByLanguage.</summary>
    internal (string Path, string Url) GetWordSourceForLanguage(LanguageCodeInfo language)
    {
        if (WordsByLanguage is not { Count: > 0 })
            return (WordsFilePath, WordsUrl);

        bool HasSource(LanguageWordSourceConfig c) => !string.IsNullOrWhiteSpace(c.WordsFilePath) || !string.IsNullOrWhiteSpace(c.WordsUrl);

        if (WordsByLanguage.TryGetValue(language.Bcp47, out var config) && HasSource(config))
            return (config.WordsFilePath, config.WordsUrl);

        if (!string.IsNullOrWhiteSpace(language.Iso6391) && WordsByLanguage!.TryGetValue(language.Iso6391!, out config) && HasSource(config))
            return (config.WordsFilePath, config.WordsUrl);

        if (!string.IsNullOrWhiteSpace(language.Iso6393) && WordsByLanguage!.TryGetValue(language.Iso6393!, out config) && HasSource(config))
            return (config.WordsFilePath, config.WordsUrl);

        return (WordsFilePath, WordsUrl);
    }
}