using Lyo.Common.Core;
using Lyo.Common.Core.Extensions;

namespace Lyo.Common.Metadata.Records;

/// <summary>Catalog row for a language tag, covering BCP 47, ISO 639-1, and ISO 639-3.</summary>
/// <remarks>
/// <para>One record can carry codes from several standards:</para>
/// <para>- <strong>BCP 47:</strong> <c>{language}-{country}</c> tags (for example <c>en-US</c>, <c>fr-FR</c>)</para>
/// <para>- <strong>ISO 639-1:</strong> two-letter language codes (for example <c>en</c>, <c>es</c>, <c>fr</c>)</para>
/// <para>- <strong>ISO 639-3:</strong> three-letter language codes (for example <c>eng</c>, <c>spa</c>, <c>fra</c>)</para>
/// <para>Base variants populate all three. Regional sub-variants carry only a BCP 47 tag.</para>
/// </remarks>
public record LanguageCodeInfo(string Name, string Bcp47, string? Iso6391, string? Iso6393, string Description)
{
    // Unrecognized tag
    public static readonly LanguageCodeInfo Unknown = new("Unknown", "unknown", "unknown", "unknown", "Unknown");

    // English (ISO 639-1: en, ISO 639-3: eng)
    public static readonly LanguageCodeInfo EnUs = new("EnUs", "en-US", "en", "eng", "English (United States)");
    public static readonly LanguageCodeInfo EnGb = new("EnGb", "en-GB", "en", "eng", "English (United Kingdom)");
    public static readonly LanguageCodeInfo EnAu = new("EnAu", "en-AU", "en", "eng", "English (Australia)");
    public static readonly LanguageCodeInfo EnCa = new("EnCa", "en-CA", "en", "eng", "English (Canada)");
    public static readonly LanguageCodeInfo EnIn = new("EnIn", "en-IN", "en", "eng", "English (India)");
    public static readonly LanguageCodeInfo EnIe = new("EnIe", "en-IE", "en", "eng", "English (Ireland)");
    public static readonly LanguageCodeInfo EnNz = new("EnNz", "en-NZ", "en", "eng", "English (New Zealand)");
    public static readonly LanguageCodeInfo EnZa = new("EnZa", "en-ZA", "en", "eng", "English (South Africa)");

    // Spanish (ISO 639-1: es, ISO 639-3: spa)
    public static readonly LanguageCodeInfo EsEs = new("EsEs", "es-ES", "es", "spa", "Spanish (Spain)");
    public static readonly LanguageCodeInfo EsMx = new("EsMx", "es-MX", "es", "spa", "Spanish (Mexico)");
    public static readonly LanguageCodeInfo EsUs = new("EsUs", "es-US", "es", "spa", "Spanish (United States)");
    public static readonly LanguageCodeInfo EsAr = new("EsAr", "es-AR", "es", "spa", "Spanish (Argentina)");
    public static readonly LanguageCodeInfo EsCo = new("EsCo", "es-CO", "es", "spa", "Spanish (Colombia)");
    public static readonly LanguageCodeInfo EsCl = new("EsCl", "es-CL", "es", "spa", "Spanish (Chile)");
    public static readonly LanguageCodeInfo EsPe = new("EsPe", "es-PE", "es", "spa", "Spanish (Peru)");

    // French (ISO 639-1: fr, ISO 639-3: fra)
    public static readonly LanguageCodeInfo FrFr = new("FrFr", "fr-FR", "fr", "fra", "French (France)");
    public static readonly LanguageCodeInfo FrCa = new("FrCa", "fr-CA", "fr", "fra", "French (Canada)");
    public static readonly LanguageCodeInfo FrBe = new("FrBe", "fr-BE", "fr", "fra", "French (Belgium)");
    public static readonly LanguageCodeInfo FrCh = new("FrCh", "fr-CH", "fr", "fra", "French (Switzerland)");

    // German (ISO 639-1: de, ISO 639-3: deu)
    public static readonly LanguageCodeInfo DeDe = new("DeDe", "de-DE", "de", "deu", "German (Germany)");
    public static readonly LanguageCodeInfo DeAt = new("DeAt", "de-AT", "de", "deu", "German (Austria)");
    public static readonly LanguageCodeInfo DeCh = new("DeCh", "de-CH", "de", "deu", "German (Switzerland)");

    // Portuguese (ISO 639-1: pt, ISO 639-3: por)
    public static readonly LanguageCodeInfo PtBr = new("PtBr", "pt-BR", "pt", "por", "Portuguese (Brazil)");
    public static readonly LanguageCodeInfo PtPt = new("PtPt", "pt-PT", "pt", "por", "Portuguese (Portugal)");

    // Chinese (ISO 639-1: zh, ISO 639-3: zho)
    public static readonly LanguageCodeInfo ZhCn = new("ZhCn", "zh-CN", "zh", "zho", "Chinese (Simplified, China)");
    public static readonly LanguageCodeInfo ZhTw = new("ZhTw", "zh-TW", "zh", "zho", "Chinese (Traditional, Taiwan)");
    public static readonly LanguageCodeInfo ZhHk = new("ZhHk", "zh-HK", "zh", "zho", "Chinese (Traditional, Hong Kong)");

    // Japanese entries (ISO 639-1: ja, ISO 639-3: jpn)
    public static readonly LanguageCodeInfo JaJp = new("JaJp", "ja-JP", "ja", "jpn", "Japanese (Japan)");

    // Korean entries (ISO 639-1: ko, ISO 639-3: kor)
    public static readonly LanguageCodeInfo KoKr = new("KoKr", "ko-KR", "ko", "kor", "Korean (South Korea)");

    // Italian entries (ISO 639-1: it, ISO 639-3: ita)
    public static readonly LanguageCodeInfo ItIt = new("ItIt", "it-IT", "it", "ita", "Italian (Italy)");

    // Russian entries (ISO 639-1: ru, ISO 639-3: rus)
    public static readonly LanguageCodeInfo RuRu = new("RuRu", "ru-RU", "ru", "rus", "Russian (Russia)");

    // Arabic (ISO 639-1: ar, ISO 639-3: ara)
    public static readonly LanguageCodeInfo ArSa = new("ArSa", "ar-SA", "ar", "ara", "Arabic (Saudi Arabia)");
    public static readonly LanguageCodeInfo ArAe = new("ArAe", "ar-AE", "ar", "ara", "Arabic (United Arab Emirates)");
    public static readonly LanguageCodeInfo ArEg = new("ArEg", "ar-EG", "ar", "ara", "Arabic (Egypt)");

    // Hindi entries (ISO 639-1: hi, ISO 639-3: hin)
    public static readonly LanguageCodeInfo HiIn = new("HiIn", "hi-IN", "hi", "hin", "Hindi (India)");

    // Dutch (ISO 639-1: nl, ISO 639-3: nld)
    public static readonly LanguageCodeInfo NlNl = new("NlNl", "nl-NL", "nl", "nld", "Dutch (Netherlands)");
    public static readonly LanguageCodeInfo NlBe = new("NlBe", "nl-BE", "nl", "nld", "Dutch (Belgium)");

    // Polish entries (ISO 639-1: pl, ISO 639-3: pol)
    public static readonly LanguageCodeInfo PlPl = new("PlPl", "pl-PL", "pl", "pol", "Polish (Poland)");

    // Turkish entries (ISO 639-1: tr, ISO 639-3: tur)
    public static readonly LanguageCodeInfo TrTr = new("TrTr", "tr-TR", "tr", "tur", "Turkish (Turkey)");

    // Vietnamese entries (ISO 639-1: vi, ISO 639-3: vie)
    public static readonly LanguageCodeInfo ViVn = new("ViVn", "vi-VN", "vi", "vie", "Vietnamese (Vietnam)");

    // Thai entries (ISO 639-1: th, ISO 639-3: tha)
    public static readonly LanguageCodeInfo ThTh = new("ThTh", "th-TH", "th", "tha", "Thai (Thailand)");

    // Swedish entries (ISO 639-1: sv, ISO 639-3: swe)
    public static readonly LanguageCodeInfo SvSe = new("SvSe", "sv-SE", "sv", "swe", "Swedish (Sweden)");

    // Norwegian entries (ISO 639-1: no, ISO 639-3: nor)
    public static readonly LanguageCodeInfo NoNo = new("NoNo", "no-NO", "no", "nor", "Norwegian (Norway)");

    // Danish entries (ISO 639-1: da, ISO 639-3: dan)
    public static readonly LanguageCodeInfo DaDk = new("DaDk", "da-DK", "da", "dan", "Danish (Denmark)");

    // Finnish entries (ISO 639-1: fi, ISO 639-3: fin)
    public static readonly LanguageCodeInfo FiFi = new("FiFi", "fi-FI", "fi", "fin", "Finnish (Finland)");

    // Czech entries (ISO 639-1: cs, ISO 639-3: ces)
    public static readonly LanguageCodeInfo CsCz = new("CsCz", "cs-CZ", "cs", "ces", "Czech (Czech Republic)");

    // Greek entries (ISO 639-1: el, ISO 639-3: ell)
    public static readonly LanguageCodeInfo ElGr = new("ElGr", "el-GR", "el", "ell", "Greek (Greece)");

    // Hebrew entries (ISO 639-1: he, ISO 639-3: heb)
    public static readonly LanguageCodeInfo HeIl = new("HeIl", "he-IL", "he", "heb", "Hebrew (Israel)");

    // Romanian entries (ISO 639-1: ro, ISO 639-3: ron)
    public static readonly LanguageCodeInfo RoRo = new("RoRo", "ro-RO", "ro", "ron", "Romanian (Romania)");

    // Hungarian entries (ISO 639-1: hu, ISO 639-3: hun)
    public static readonly LanguageCodeInfo HuHu = new("HuHu", "hu-HU", "hu", "hun", "Hungarian (Hungary)");

    // Indonesian entries (ISO 639-1: id, ISO 639-3: ind)
    public static readonly LanguageCodeInfo IdId = new("IdId", "id-ID", "id", "ind", "Indonesian (Indonesia)");

    // Malay entries (ISO 639-1: ms, ISO 639-3: zsm)
    public static readonly LanguageCodeInfo MsMy = new("MsMy", "ms-MY", "ms", "zsm", "Malay (Malaysia)");

    // Filipino entries (ISO 639-1: fil, ISO 639-3: fil)
    public static readonly LanguageCodeInfo FilPh = new("FilPh", "fil-PH", "fil", "fil", "Filipino (Philippines)");

    // Lookup tables keyed by BCP 47, ISO 639-1, and ISO 639-3
    private static readonly Dictionary<string, LanguageCodeInfo> ByBcp47 = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, LanguageCodeInfo> ByIso6391 = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, LanguageCodeInfo> ByIso6393 = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<LanguageCodeInfo> AllCodes = [];

    /// <summary>Every registered language-code catalog row.</summary>
    public static IReadOnlyList<LanguageCodeInfo> All => AllCodes;

    static LanguageCodeInfo()
    {
        var fields = typeof(LanguageCodeInfo).PublicStaticFields<LanguageCodeInfo>();

        foreach (var langCode in fields) {
            AllCodes.Add(langCode);
            ByBcp47[langCode.Bcp47] = langCode;
            if (!langCode.Iso6391.IsNullOrWhitespace()) {
                // Keep the first ISO 639-1 hit (base variant).
                var iso1 = langCode.Iso6391;
                if (!ByIso6391.ContainsKey(iso1))
                    ByIso6391[iso1] = langCode;
            }

            if (!langCode.Iso6393.IsNullOrWhitespace()) {
                // Keep the first ISO 639-3 hit (base variant).
                var iso3 = langCode.Iso6393;
                if (!ByIso6393.ContainsKey(iso3))
                    ByIso6393[iso3] = langCode;
            }
        }
    }

    /// <summary>Looks up a language by BCP 47 tag.</summary>
    /// <param name="bcp47Code">BCP 47 tag, for example <c>en-US</c> or <c>fr-FR</c>.</param>
    /// <returns>The catalog row, or <see cref="Unknown" /> when the tag is not registered.</returns>
    public static LanguageCodeInfo FromBcp47(string? bcp47Code)
    {
        if (bcp47Code.IsNullOrWhitespace())
            return Unknown;

        var trimmed = bcp47Code.Trim();
        return ByBcp47.TryGetValue(trimmed, out var code) ? code : Unknown;
    }

    /// <summary>Looks up a language by ISO 639-1 (two-letter) code.</summary>
    /// <param name="iso6391Code">ISO 639-1 token, for example <c>en</c>, <c>es</c>, or <c>fr</c>.</param>
    /// <returns>The first matching row (base variant), or <see cref="Unknown" /> when none match.</returns>
    /// <remarks>When several catalog rows share an ISO 639-1 code, the first registered (base) variant wins.</remarks>
    public static LanguageCodeInfo FromIso6391(string? iso6391Code)
    {
        if (iso6391Code.IsNullOrWhitespace())
            return Unknown;

        var trimmed = iso6391Code.Trim().ToLowerInvariant();
        return ByIso6391.TryGetValue(trimmed, out var code) ? code : Unknown;
    }

    /// <summary>Looks up a language by ISO 639-3 (three-letter) code.</summary>
    /// <param name="iso6393Code">ISO 639-3 token, for example <c>eng</c>, <c>spa</c>, or <c>fra</c>.</param>
    /// <returns>The first matching row (base variant), or <see cref="Unknown" /> when none match.</returns>
    /// <remarks>When several catalog rows share an ISO 639-3 code, the first registered (base) variant wins.</remarks>
    public static LanguageCodeInfo FromIso6393(string? iso6393Code)
    {
        if (iso6393Code.IsNullOrWhitespace())
            return Unknown;

        var trimmed = iso6393Code.Trim().ToLowerInvariant();
        return ByIso6393.TryGetValue(trimmed, out var code) ? code : Unknown;
    }
}