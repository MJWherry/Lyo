using SixLabors.Fonts;

namespace Lyo.Images.Decoration;

/// <summary>Process-wide LRU cache for resolved <see cref="Font" /> instances keyed by family + half-point size, shared across the decoration primitives.</summary>
internal static class FontCache
{
    private const int MaxFontCacheEntries = 64;

    private static readonly object Lock = new();

    private static readonly Dictionary<string, Font> Cache = new(StringComparer.Ordinal);

    private static readonly Queue<string> Order = new();

    /// <summary>Returns a cached font, creating it (and evicting the oldest entry on overflow) when absent.</summary>
    public static Font GetOrCreate(float sizePx, string? family)
    {
        var key = CacheKey(sizePx, family);
        lock (Lock) {
            if (Cache.TryGetValue(key, out var cached))
                return cached;

            while (Cache.Count >= MaxFontCacheEntries && Order.Count > 0) {
                var evictKey = Order.Dequeue();
                Cache.Remove(evictKey);
            }

            var font = CreateFont(sizePx, family);
            Cache[key] = font;
            Order.Enqueue(key);
            return font;
        }
    }

    private static string CacheKey(float sizePx, string? family)
    {
        var halfPoints = Math.Clamp((int)Math.Round(sizePx * 2), 1, 20000);
        var fam = string.IsNullOrWhiteSpace(family) ? "" : family.Trim();
        return $"{fam}|{halfPoints}";
    }

    private static Font CreateFont(float sizePx, string? family)
    {
        var names = string.IsNullOrWhiteSpace(family)
            ? new[] { "DejaVu Sans", "Liberation Sans", "Arial", "Helvetica" }
            : new[] { family, "DejaVu Sans", "Liberation Sans", "Arial" };

        foreach (var n in names) {
            try {
                return SystemFonts.CreateFont(n, sizePx, FontStyle.Regular);
            }
            catch {
                /* try next */
            }
        }

        return SystemFonts.CreateFont(SystemFonts.Families.First().Name, sizePx, FontStyle.Regular);
    }
}