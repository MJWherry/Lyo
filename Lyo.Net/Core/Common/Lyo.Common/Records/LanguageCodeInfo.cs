using System.Reflection;

namespace Lyo.Common.Records;

/// <summary>Represents language code information with support for multiple standards (BCP 47, ISO 639-1, ISO 639-3).</summary>
/// <remarks>
/// <para>This record consolidates language codes from multiple standards:</para>
/// <para>- <strong>BCP 47:</strong> Format "{language}-{country}" (e.g., "en-US", "fr-FR")</para>
/// <para>- <strong>ISO 639-1:</strong> 2-letter language codes (e.g., "en", "es", "fr")</para>
/// <para>- <strong>ISO 639-3:</strong> 3-letter language codes (e.g., "eng", "spa", "fra")</para>
/// <para>Base variants include all three code types. Sub-variants (regional variants) only include BCP 47 codes.</para>
/// </remarks>
public record LanguageCodeInfo(string Name, string Bcp47, string? Iso6391, string? Iso6393, string Description)
{
    // Unknown
    public static readonly LanguageCodeInfo Unknown = new("Unknown", "unknown", "unknown", "unknown", "Unknown");

    // English variants (ISO 639-1: en, ISO 639-3: eng)
    public static readonly LanguageCodeInfo EnUs = new("EnUs", "en-US", "en", "eng", "English (United States)");
    public static readonly LanguageCodeInfo EnGb = new("EnGb", "en-GB", "en", "eng", "English (United Kingdom)");
    public static readonly LanguageCodeInfo EnAu = new("EnAu", "en-AU", "en", "eng", "English (Australia)");
    public static readonly LanguageCodeInfo EnCa = new("EnCa", "en-CA", "en", "eng", "English (Canada)");
    public static readonly LanguageCodeInfo EnIn = new("EnIn", "en-IN", "en", "eng", "English (India)");
    public static readonly LanguageCodeInfo EnIe = new("EnIe", "en-IE", "en", "eng", "English (Ireland)");
    public static readonly LanguageCodeInfo EnNz = new("EnNz", "en-NZ", "en", "eng", "English (New Zealand)");
    public static readonly LanguageCodeInfo EnZa = new("EnZa", "en-ZA", "en", "eng", "English (South Africa)");

    // Spanish variants (ISO 639-1: es, ISO 639-3: spa)
    public static readonly LanguageCodeInfo EsEs = new("EsEs", "es-ES", "es", "spa", "Spanish (Spain)");
    public static readonly LanguageCodeInfo EsMx = new("EsMx", "es-MX", "es", "spa", "Spanish (Mexico)");
    public static readonly LanguageCodeInfo EsUs = new("EsUs", "es-US", "es", "spa", "Spanish (United States)");
    public static readonly LanguageCodeInfo EsAr = new("EsAr", "es-AR", "es", "spa", "Spanish (Argentina)");
    public static readonly LanguageCodeInfo EsCo = new("EsCo", "es-CO", "es", "spa", "Spanish (Colombia)");
    public static readonly LanguageCodeInfo EsCl = new("EsCl", "es-CL", "es", "spa", "Spanish (Chile)");
    public static readonly LanguageCodeInfo EsPe = new("EsPe", "es-PE", "es", "spa", "Spanish (Peru)");

    // French variants (ISO 639-1: fr, ISO 639-3: fra)
    public static readonly LanguageCodeInfo FrFr = new("FrFr", "fr-FR", "fr", "fra", "French (France)");
    public static readonly LanguageCodeInfo FrCa = new("FrCa", "fr-CA", "fr", "fra", "French (Canada)");
    public static readonly LanguageCodeInfo FrBe = new("FrBe", "fr-BE", "fr", "fra", "French (Belgium)");
    public static readonly LanguageCodeInfo FrCh = new("FrCh", "fr-CH", "fr", "fra", "French (Switzerland)");

    // German variants (ISO 639-1: de, ISO 639-3: deu)
    public static readonly LanguageCodeInfo DeDe = new("DeDe", "de-DE", "de", "deu", "German (Germany)");
    public static readonly LanguageCodeInfo DeAt = new("DeAt", "de-AT", "de", "deu", "German (Austria)");
    public static readonly LanguageCodeInfo DeCh = new("DeCh", "de-CH", "de", "deu", "German (Switzerland)");

    // Portuguese variants (ISO 639-1: pt, ISO 639-3: por)
    public static readonly LanguageCodeInfo PtBr = new("PtBr", "pt-BR", "pt", "por", "Portuguese (Brazil)");
    public static readonly LanguageCodeInfo PtPt = new("PtPt", "pt-PT", "pt", "por", "Portuguese (Portugal)");

    // Chinese variants (ISO 639-1: zh, ISO 639-3: zho)
    public static readonly LanguageCodeInfo ZhCn = new("ZhCn", "zh-CN", "zh", "zho", "Chinese (Simplified, China)");
    public static readonly LanguageCodeInfo ZhTw = new("ZhTw", "zh-TW", "zh", "zho", "Chinese (Traditional, Taiwan)");
    public static readonly LanguageCodeInfo ZhHk = new("ZhHk", "zh-HK", "zh", "zho", "Chinese (Traditional, Hong Kong)");

    // Japanese (ISO 639-1: ja, ISO 639-3: jpn)
    public static readonly LanguageCodeInfo JaJp = new("JaJp", "ja-JP", "ja", "jpn", "Japanese (Japan)");

    // Korean (ISO 639-1: ko, ISO 639-3: kor)
    public static readonly LanguageCodeInfo KoKr = new("KoKr", "ko-KR", "ko", "kor", "Korean (South Korea)");

    // Italian (ISO 639-1: it, ISO 639-3: ita)
    public static readonly LanguageCodeInfo ItIt = new("ItIt", "it-IT", "it", "ita", "Italian (Italy)");

    // Russian (ISO 639-1: ru, ISO 639-3: rus)
    public static readonly LanguageCodeInfo RuRu = new("RuRu", "ru-RU", "ru", "rus", "Russian (Russia)");

    // Arabic variants (ISO 639-1: ar, ISO 639-3: ara)
    public static readonly LanguageCodeInfo ArSa = new("ArSa", "ar-SA", "ar", "ara", "Arabic (Saudi Arabia)");
    public static readonly LanguageCodeInfo ArAe = new("ArAe", "ar-AE", "ar", "ara", "Arabic (United Arab Emirates)");
    public static readonly LanguageCodeInfo ArEg = new("ArEg", "ar-EG", "ar", "ara", "Arabic (Egypt)");

    // Hindi (ISO 639-1: hi, ISO 639-3: hin)
    public static readonly LanguageCodeInfo HiIn = new("HiIn", "hi-IN", "hi", "hin", "Hindi (India)");

    // Dutch variants (ISO 639-1: nl, ISO 639-3: nld)
    public static readonly LanguageCodeInfo NlNl = new("NlNl", "nl-NL", "nl", "nld", "Dutch (Netherlands)");
    public static readonly LanguageCodeInfo NlBe = new("NlBe", "nl-BE", "nl", "nld", "Dutch (Belgium)");

    // Polish (ISO 639-1: pl, ISO 639-3: pol)
    public static readonly LanguageCodeInfo PlPl = new("PlPl", "pl-PL", "pl", "pol", "Polish (Poland)");

    // Turkish (ISO 639-1: tr, ISO 639-3: tur)
    public static readonly LanguageCodeInfo TrTr = new("TrTr", "tr-TR", "tr", "tur", "Turkish (Turkey)");

    // Vietnamese (ISO 639-1: vi, ISO 639-3: vie)
    public static readonly LanguageCodeInfo ViVn = new("ViVn", "vi-VN", "vi", "vie", "Vietnamese (Vietnam)");

    // Thai (ISO 639-1: th, ISO 639-3: tha)
    public static readonly LanguageCodeInfo ThTh = new("ThTh", "th-TH", "th", "tha", "Thai (Thailand)");

    // Swedish (ISO 639-1: sv, ISO 639-3: swe)
    public static readonly LanguageCodeInfo SvSe = new("SvSe", "sv-SE", "sv", "swe", "Swedish (Sweden)");

    // Norwegian (ISO 639-1: no, ISO 639-3: nor)
    public static readonly LanguageCodeInfo NoNo = new("NoNo", "no-NO", "no", "nor", "Norwegian (Norway)");

    // Danish (ISO 639-1: da, ISO 639-3: dan)
    public static readonly LanguageCodeInfo DaDk = new("DaDk", "da-DK", "da", "dan", "Danish (Denmark)");

    // Finnish (ISO 639-1: fi, ISO 639-3: fin)
    public static readonly LanguageCodeInfo FiFi = new("FiFi", "fi-FI", "fi", "fin", "Finnish (Finland)");

    // Czech (ISO 639-1: cs, ISO 639-3: ces)
    public static readonly LanguageCodeInfo CsCz = new("CsCz", "cs-CZ", "cs", "ces", "Czech (Czech Republic)");

    // Greek (ISO 639-1: el, ISO 639-3: ell)
    public static readonly LanguageCodeInfo ElGr = new("ElGr", "el-GR", "el", "ell", "Greek (Greece)");

    // Hebrew (ISO 639-1: he, ISO 639-3: heb)
    public static readonly LanguageCodeInfo HeIl = new("HeIl", "he-IL", "he", "heb", "Hebrew (Israel)");

    // Romanian (ISO 639-1: ro, ISO 639-3: ron)
    public static readonly LanguageCodeInfo RoRo = new("RoRo", "ro-RO", "ro", "ron", "Romanian (Romania)");

    // Hungarian (ISO 639-1: hu, ISO 639-3: hun)
    public static readonly LanguageCodeInfo HuHu = new("HuHu", "hu-HU", "hu", "hun", "Hungarian (Hungary)");

    // Indonesian (ISO 639-1: id, ISO 639-3: ind)
    public static readonly LanguageCodeInfo IdId = new("IdId", "id-ID", "id", "ind", "Indonesian (Indonesia)");

    // Malay (ISO 639-1: ms, ISO 639-3: zsm)
    public static readonly LanguageCodeInfo MsMy = new("MsMy", "ms-MY", "ms", "zsm", "Malay (Malaysia)");

    // Filipino (ISO 639-1: fil, ISO 639-3: fil)
    public static readonly LanguageCodeInfo FilPh = new("FilPh", "fil-PH", "fil", "fil", "Filipino (Philippines)");

    // Static registry with fast lookups
    private static readonly Dictionary<string, LanguageCodeInfo> _byBcp47 = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, LanguageCodeInfo> _byIso6391 = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, LanguageCodeInfo> _byIso6393 = new(StringComparer.OrdinalIgnoreCase);
    private static readonly List<LanguageCodeInfo> _allCodes = [];

    /// <summary>Gets all registered language codes.</summary>
    public static IReadOnlyList<LanguageCodeInfo> All => _allCodes;

    static LanguageCodeInfo()
    {
        // Register all codes using reflection to find static fields
        var type = typeof(LanguageCodeInfo);
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(LanguageCodeInfo))
            .Select(f => (LanguageCodeInfo)f.GetValue(null)!)
            .ToList();

        foreach (var langCode in fields) {
            _allCodes.Add(langCode);
            _byBcp47[langCode.Bcp47] = langCode;
            if (!string.IsNullOrWhiteSpace(langCode.Iso6391)) {
                // For ISO 639-1, store the first occurrence (base variant)
                var iso1 = langCode.Iso6391;
                if (iso1 != null && !_byIso6391.ContainsKey(iso1))
                    _byIso6391[iso1] = langCode;
            }

            if (!string.IsNullOrWhiteSpace(langCode.Iso6393)) {
                // For ISO 639-3, store the first occurrence (base variant)
                var iso3 = langCode.Iso6393;
                if (iso3 != null && !_byIso6393.ContainsKey(iso3))
                    _byIso6393[iso3] = langCode;
            }
        }
    }

    /// <summary>Finds a language code by its BCP 47 code.</summary>
    /// <param name="bcp47Code">The BCP 47 code (e.g., "en-US", "fr-FR").</param>
    /// <returns>The language code, or Unknown if not found.</returns>
    public static LanguageCodeInfo FromBcp47(string? bcp47Code)
    {
        if (string.IsNullOrWhiteSpace(bcp47Code))
            return Unknown;

        var trimmed = bcp47Code!.Trim();
        return _byBcp47.TryGetValue(trimmed, out var code) ? code : Unknown;
    }

    /// <summary>Finds a language code by its ISO 639-1 (2-letter) code.</summary>
    /// <param name="iso6391Code">The ISO 639-1 code (e.g., "en", "es", "fr").</param>
    /// <returns>The first matching language code (base variant), or Unknown if not found.</returns>
    /// <remarks>When multiple language codes share the same ISO 639-1 code, returns the first one (base variant) based on registration order.</remarks>
    public static LanguageCodeInfo FromIso6391(string? iso6391Code)
    {
        if (string.IsNullOrWhiteSpace(iso6391Code))
            return Unknown;

        var trimmed = iso6391Code!.Trim().ToLowerInvariant();
        return _byIso6391.TryGetValue(trimmed, out var code) ? code : Unknown;
    }

    /// <summary>Finds a language code by its ISO 639-3 (3-letter) code.</summary>
    /// <param name="iso6393Code">The ISO 639-3 code (e.g., "eng", "spa", "fra").</param>
    /// <returns>The first matching language code (base variant), or Unknown if not found.</returns>
    /// <remarks>When multiple language codes share the same ISO 639-3 code, returns the first one (base variant) based on registration order.</remarks>
    public static LanguageCodeInfo FromIso6393(string? iso6393Code)
    {
        if (string.IsNullOrWhiteSpace(iso6393Code))
            return Unknown;

        var trimmed = iso6393Code!.Trim().ToLowerInvariant();
        return _byIso6393.TryGetValue(trimmed, out var code) ? code : Unknown;
    }
}