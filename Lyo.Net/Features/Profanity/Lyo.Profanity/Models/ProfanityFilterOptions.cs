using Lyo.Common.Records;

namespace Lyo.Profanity.Models;

/// <summary>Options for configuring the profanity filter service.</summary>
public class ProfanityFilterOptions
{
    /// <summary>Configuration section name for options binding. Default: "ProfanityFilter".</summary>
    public const string SectionName = "ProfanityFilter";

    /// <summary>
    /// Language for profanity filtering. Used to select which word list to use when WordsByLanguage is configured. Accepts BCP 47 (e.g. "en-US"), ISO 639-1 (e.g. "en"), or ISO
    /// 639-3 (e.g. "eng"). Default: "en-US".
    /// </summary>
    public string Language { get; set; } = "en-US";

    /// <summary>Strategy for replacing detected profanity. Default: ReplaceWithChar.</summary>
    public ProfanityReplacementStrategy ReplacementStrategy { get; set; } = ProfanityReplacementStrategy.ReplaceWithChar;

    /// <summary>Character used when ReplacementStrategy is ReplaceWithChar or Mask. Default: '*'.</summary>
    public char ReplacementChar { get; set; } = '*';

    /// <summary>Placeholder text used when ReplacementStrategy is ReplaceWithWord. Default: "***".</summary>
    public string ReplacementWord { get; set; } = "***";

    /// <summary>Whether matching is case-sensitive. Default: false (case-insensitive).</summary>
    public bool CaseSensitive { get; set; }

    /// <summary>Whether to match only whole words (word boundaries). Default: true.</summary>
    public bool MatchWholeWordsOnly { get; set; } = true;

    /// <summary>Additional words to include beyond the file. Useful for runtime additions without modifying the file.</summary>
    public HashSet<string> AdditionalWords { get; set; } = [];

    /// <summary>Words to exclude from the loaded list (e.g. false positives).</summary>
    public HashSet<string> ExcludedWords { get; set; } = [];

    /// <summary>Whether to enable metrics collection. Default is false. When enabled, requires IMetrics to be provided via constructor.</summary>
    public bool EnableMetrics { get; set; }

    /// <summary>String comparison used for matching. Derived from CaseSensitive.</summary>
    public StringComparison StringComparison => CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    /// <summary>Resolves the configured Language string to a LanguageCodeInfo. Falls back to EnUs when unknown.</summary>
    public LanguageCodeInfo GetLanguageCode() => ResolveLanguageCode(Language);

    /// <summary>Resolves a language string (BCP 47, ISO 639-1, or ISO 639-3) to LanguageCodeInfo.</summary>
    public static LanguageCodeInfo ResolveLanguageCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return LanguageCodeInfo.EnUs;

        var info = LanguageCodeInfo.FromBcp47(code);
        if (info != LanguageCodeInfo.Unknown)
            return info;

        info = LanguageCodeInfo.FromIso6391(code);
        if (info != LanguageCodeInfo.Unknown)
            return info;

        info = LanguageCodeInfo.FromIso6393(code);
        return info != LanguageCodeInfo.Unknown ? info : LanguageCodeInfo.EnUs;
    }
}