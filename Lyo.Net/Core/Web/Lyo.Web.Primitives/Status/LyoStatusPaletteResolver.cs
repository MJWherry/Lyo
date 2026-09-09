using Lyo.Exceptions;

namespace Lyo.Web.Primitives;

/// <summary>
/// Picks the <see cref="ILyoStatusPalette" /> that should colour a status. Registered by <c>AddLyoStatusPalette</c>; <see cref="LyoStatusChip" /> resolves it
/// optionally and uses <see cref="Default" /> when a host registered none.
/// </summary>
public sealed class LyoStatusPaletteResolver
{
    private readonly Dictionary<string, List<ILyoStatusPalette>> _byName;

    /// <summary>Builds a resolver over the registered palettes. The built-in vocabulary is always available regardless of what is passed.</summary>
    public LyoStatusPaletteResolver(IEnumerable<ILyoStatusPalette> palettes)
    {
        ArgumentHelpers.ThrowIfNull(palettes);
        _byName = new Dictionary<string, List<ILyoStatusPalette>>(StringComparer.OrdinalIgnoreCase);
        foreach (var palette in palettes) {
            if (string.IsNullOrWhiteSpace(palette.Name))
                continue;

            if (!_byName.TryGetValue(palette.Name, out var bucket))
                _byName[palette.Name] = bucket = [];

            bucket.Add(palette);
        }
    }

    /// <summary>Resolver holding only the built-in vocabulary, for hosts and unit tests that never registered a palette.</summary>
    public static LyoStatusPaletteResolver Default { get; } = new([]);

    /// <summary>
    /// Appearance for <paramref name="status" />. Tries every palette registered under <paramref name="paletteName" /> in registration order, then the built-in
    /// vocabulary, then falls back to a neutral chip carrying the raw text so an unmapped status stays readable rather than blank.
    /// </summary>
    /// <param name="status">Raw status text.</param>
    /// <param name="paletteName">Domain palette to try first. Null or blank consults only the built-in vocabulary.</param>
    public LyoChipSpec Resolve(string? status, string? paletteName = null)
    {
        if (!string.IsNullOrWhiteSpace(paletteName) && _byName.TryGetValue(paletteName, out var bucket)) {
            foreach (var palette in bucket) {
                if (palette.Resolve(status) is { } match)
                    return match;
            }
        }

        return LyoDefaultStatusPalette.Instance.Resolve(status)
            ?? new LyoChipSpec(string.IsNullOrWhiteSpace(status) ? "—" : status, Color.Default);
    }
}
