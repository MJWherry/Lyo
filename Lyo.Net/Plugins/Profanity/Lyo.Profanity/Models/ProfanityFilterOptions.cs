using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;

namespace Lyo.Profanity.Models;

/// <summary>Settings that configure the profanity filter service.</summary>
public class ProfanityFilterOptions
{
    /// <summary>Options binding. Default: "ProfanityFilter" configuration section name.</summary>
    public const string SectionName = "ProfanityFilter";

    /// <summary>
    /// Language used while filtering. Picks the WordsByLanguage list when that map is set. Accepts BCP 47 (for example "en-US"), ISO 639-1 (for example "en"), or ISO
    /// 639-3 (for example "eng"). Default: "en-US".
    /// </summary>
    public string Language { get; set; } = "en-US";

    /// <summary>Replacing detected profanity. Default: ReplaceWithChar strategy.</summary>
    public ProfanityReplacementStrategy ReplacementStrategy { get; set; } = ProfanityReplacementStrategy.ReplaceWithChar;

    /// <summary>Mask character when ReplacementStrategy is ReplaceWithChar or Mask. Default: '*'.</summary>
    public char ReplacementChar { get; set; } = '*';

    /// <summary>Replacement placeholder when ReplacementStrategy is ReplaceWithWord. Default: "***".</summary>
    public string ReplacementWord { get; set; } = "***";

    /// <summary>True when matching is case-sensitive. Default: false (case-insensitive).</summary>
    public bool CaseSensitive { get; set; }

    /// <summary>True when to match only whole words (word boundaries). Default: true.</summary>
    public bool MatchWholeWordsOnly { get; set; } = true;

    /// <summary>Runtime additions without modifying the file additional words to include beyond the file. Useful.</summary>
    public HashSet<string> AdditionalWords { get; set; } = [];

    /// <summary>Words omitted from the loaded list (e.g. false positives).</summary>
    public HashSet<string> ExcludedWords { get; set; } = [];

    /// <summary>Flag: to enable metrics collection. Default is false. When enabled, requires IMetrics to be provided via constructor.</summary>
    public bool EnableMetrics { get; set; }

    /// <summary>Throws when language or replacement settings are invalid.</summary>
    public virtual void Validate()
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(Language);
        ArgumentHelpers.ThrowIfNotDefined(ReplacementStrategy);
        ArgumentHelpers.ThrowIfNull(ReplacementWord);
        ArgumentHelpers.ThrowIfNull(AdditionalWords);
        ArgumentHelpers.ThrowIfNull(ExcludedWords);
    }

    /// <summary>Matching. Derived from CaseSensitive string comparison used.</summary>
    public StringComparison StringComparison => CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    /// <summary>Looks up the configured Language string to a LanguageCodeInfo. Falls back to EnUs when unknown.</summary>
    public LanguageCodeInfo GetLanguageCode() => ResolveLanguageCode(Language);

    /// <summary>Looks up a language string (BCP 47, ISO 639-1, or ISO 639-3) to LanguageCodeInfo.</summary>
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