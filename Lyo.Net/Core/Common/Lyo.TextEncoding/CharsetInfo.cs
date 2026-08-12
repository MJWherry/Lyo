using System.Reflection;
using System.Text;
using Lyo.Exceptions;
#if NET5_0_OR_GREATER
using System.Collections.Frozen;
#endif

namespace Lyo.TextEncoding;

/// <summary>
/// Curated charset metadata (well-known statics + custom instances). Does not replace <see cref="Encoding" /> — use <see cref="ToEncoding" /> / service resolve for the BCL
/// type.
/// </summary>
public sealed record CharsetInfo(string Name, string WebName, int? CodePage, string Description, IReadOnlyList<string>? Aliases = null)
{
    /// <summary>Unicode UTF-8.</summary>
    public static readonly CharsetInfo Utf8 = new("Utf8", "utf-8", 65001, "Unicode UTF-8", ["utf8"]);

    /// <summary>Unicode UTF-16 little-endian.</summary>
    public static readonly CharsetInfo Utf16Le = new("Utf16Le", "utf-16", 1200, "Unicode UTF-16 little-endian", ["utf-16le", "unicode"]);

    /// <summary>Unicode UTF-16 big-endian.</summary>
    public static readonly CharsetInfo Utf16Be = new("Utf16Be", "utf-16BE", 1201, "Unicode UTF-16 big-endian", ["utf-16be"]);

    /// <summary>Unicode UTF-32 little-endian.</summary>
    public static readonly CharsetInfo Utf32Le = new("Utf32Le", "utf-32", 12000, "Unicode UTF-32 little-endian", ["utf-32le"]);

    /// <summary>Unicode UTF-32 big-endian.</summary>
    public static readonly CharsetInfo Utf32Be = new("Utf32Be", "utf-32BE", 12001, "Unicode UTF-32 big-endian", ["utf-32be"]);

    /// <summary>US-ASCII.</summary>
    public static readonly CharsetInfo Ascii = new("Ascii", "us-ascii", 20127, "US-ASCII", ["ascii"]);

    /// <summary>Western European (ISO).</summary>
    public static readonly CharsetInfo Iso88591 = new("Iso88591", "iso-8859-1", 28591, "Western European (ISO)", ["latin1", "iso8859-1"]);

    /// <summary>Western European (Windows).</summary>
    public static readonly CharsetInfo Windows1252 = new("Windows1252", "windows-1252", 1252, "Western European (Windows)", ["cp1252", "1252"]);

    /// <summary>Japanese (Shift-JIS).</summary>
    public static readonly CharsetInfo ShiftJis = new("ShiftJis", "shift_jis", 932, "Japanese (Shift-JIS)", ["shift-jis", "sjis"]);

    /// <summary>Japanese (EUC).</summary>
    public static readonly CharsetInfo EucJp = new("EucJp", "euc-jp", 51932, "Japanese (EUC)");

    /// <summary>Chinese Simplified (GB18030).</summary>
    public static readonly CharsetInfo Gb18030 = new("Gb18030", "GB18030", 54936, "Chinese Simplified (GB18030)");

    /// <summary>Chinese Traditional (Big5).</summary>
    public static readonly CharsetInfo Big5 = new("Big5", "big5", 950, "Chinese Traditional (Big5)");

    /// <summary>Korean (EUC).</summary>
    public static readonly CharsetInfo EucKr = new("EucKr", "euc-kr", 51949, "Korean (EUC)");

    /// <summary>Cyrillic (Windows).</summary>
    public static readonly CharsetInfo Windows1251 = new("Windows1251", "windows-1251", 1251, "Cyrillic (Windows)", ["cp1251"]);

    /// <summary>Central European (Windows).</summary>
    public static readonly CharsetInfo Windows1250 = new("Windows1250", "windows-1250", 1250, "Central European (Windows)", ["cp1250"]);

#if NET5_0_OR_GREATER
    private static readonly FrozenDictionary<string, CharsetInfo> ByName = BuildNameMap();
    private static readonly FrozenDictionary<int, CharsetInfo> ByCodePage = BuildCodePageMap();
#else
    private static readonly Dictionary<string, CharsetInfo> ByName = BuildNameMap();
    private static readonly Dictionary<int, CharsetInfo> ByCodePage = BuildCodePageMap();
#endif

    /// <summary>All well-known static entries for UI/config pickers. Custom instances are not included.</summary>
    public static IReadOnlyList<CharsetInfo> WellKnown { get; } = BuildWellKnown();

    /// <summary>Create a custom charset entry (not limited to the well-known set). Resolve at runtime via <see cref="ToEncoding" />.</summary>
    public static CharsetInfo Custom(string name, string webName, int? codePage = null, string? description = null, IReadOnlyList<string>? aliases = null)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(name);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(webName);
        return new(name, webName, codePage, description ?? name, aliases);
    }

    /// <summary>Resolve the BCL encoding for this entry (no fallback mutation).</summary>
    public Encoding ToEncoding()
    {
        try {
            if (CodePage is { } cp)
                return Encoding.GetEncoding(cp);

            return Encoding.GetEncoding(WebName);
        }
        catch (ArgumentException ex) {
            throw new EncodingException($"Unknown charset '{WebName}' (code page {CodePage?.ToString() ?? "n/a"}).", ex);
        }
        catch (NotSupportedException ex) {
            throw new EncodingException($"Unsupported charset '{WebName}' (code page {CodePage?.ToString() ?? "n/a"}).", ex);
        }
    }

    /// <summary>Lookup a well-known entry by web name or alias (case-insensitive). Returns null if not in the catalog.</summary>
    public static CharsetInfo? FromWebName(string nameOrAlias)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(nameOrAlias);
        return ByName.TryGetValue(Normalize(nameOrAlias), out var info) ? info : null;
    }

    /// <summary>Lookup a well-known entry by code page. Returns null if not in the catalog.</summary>
    public static CharsetInfo? FromCodePage(int codePage) => ByCodePage.TryGetValue(codePage, out var info) ? info : null;

    /// <summary>Map a BCL encoding to a well-known entry when possible; otherwise a <see cref="Custom" /> instance.</summary>
    public static CharsetInfo FromEncoding(Encoding encoding)
    {
        ArgumentHelpers.ThrowIfNull(encoding);
        if (FromCodePage(encoding.CodePage) is { } byCp)
            return byCp;

        if (FromWebName(encoding.WebName) is { } byWeb)
            return byWeb;

        return Custom(encoding.EncodingName, encoding.WebName, encoding.CodePage, encoding.EncodingName);
    }

    private static IReadOnlyList<CharsetInfo> BuildWellKnown()
        => typeof(CharsetInfo).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(CharsetInfo))
            .Select(f => (CharsetInfo)f.GetValue(null)!)
            .ToArray();

#if NET5_0_OR_GREATER
    private static FrozenDictionary<string, CharsetInfo> BuildNameMap()
#else
    private static Dictionary<string, CharsetInfo> BuildNameMap()
#endif
    {
        var map = new Dictionary<string, CharsetInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var info in WellKnown) {
            map[Normalize(info.Name)] = info;
            map[Normalize(info.WebName)] = info;
            if (info.Aliases is null)
                continue;

            foreach (var alias in info.Aliases)
                map[Normalize(alias)] = info;
        }

#if NET5_0_OR_GREATER
        return map.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
#else
        return map;
#endif
    }

#if NET5_0_OR_GREATER
    private static FrozenDictionary<int, CharsetInfo> BuildCodePageMap()
#else
    private static Dictionary<int, CharsetInfo> BuildCodePageMap()
#endif
    {
        var map = new Dictionary<int, CharsetInfo>();
        foreach (var info in WellKnown) {
            if (info.CodePage is { } cp)
                map[cp] = info;
        }

#if NET5_0_OR_GREATER
        return map.ToFrozenDictionary();
#else
        return map;
#endif
    }

    private static string Normalize(string value) => value.Trim();
}