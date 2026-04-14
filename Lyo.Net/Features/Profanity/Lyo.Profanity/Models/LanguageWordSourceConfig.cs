namespace Lyo.Profanity.Models;

/// <summary>Configuration for loading profanity words from a file or URL for a specific language.</summary>
public class LanguageWordSourceConfig
{
    /// <summary>Path to the JSON file containing profane words as an array, e.g. ["word1", "word2", ...].</summary>
    public string WordsFilePath { get; set; } = string.Empty;

    /// <summary>URL to load a JSON word list from. Words from URL are merged with WordsFilePath and AdditionalWords when both are configured.</summary>
    public string WordsUrl { get; set; } = string.Empty;
}