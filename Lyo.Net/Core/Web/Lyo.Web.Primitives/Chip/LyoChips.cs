using Lyo.Exceptions;

namespace Lyo.Web.Primitives;

/// <summary>Builds <see cref="LyoChipSpec" /> values for grid cells and status chips.</summary>
public static class LyoChips
{
    /// <summary>Blank or whitespace labels become an em dash.</summary>
    public static LyoChipSpec Of(string? label, Color color = Color.Default, string? icon = null, Variant variant = Variant.Filled, string? style = null)
        => new(string.IsNullOrWhiteSpace(label) ? "—" : label, color, icon, variant, style);

    /// <summary>Identity chip using a named <see cref="LyoChipHue" /> instead of a theme Color.</summary>
    public static LyoChipSpec Of(string? label, LyoChipHue hue, string? icon = null, Variant variant = Variant.Filled)
    {
        ArgumentHelpers.ThrowIfNull(hue);
        return Of(label, Color.Default, icon, variant, hue.Style);
    }

    /// <summary>CSS variable for a custom chip hue. Prefer a named <see cref="LyoChipHue" /> over a raw degree.</summary>
    public static string HueStyle(int hue) => new LyoChipHue(hue).Style;

    /// <summary>CSS variable from a named hue.</summary>
    public static string HueStyle(LyoChipHue hue)
    {
        ArgumentHelpers.ThrowIfNull(hue);
        return hue.Style;
    }

    /// <summary>
    /// Parses a projected enum name. Unknown or blank values keep the raw text (or an em dash) plus <see cref="Color.Default" />.
    /// </summary>
    public static LyoChipSpec FromEnum<T>(string? text, Func<T, Color> color, Func<T, string>? icon = null)
        where T : struct, Enum
    {
        if (Enum.TryParse<T>(text, ignoreCase: true, out var value))
            return new(value.ToString(), color(value), icon?.Invoke(value));

        return Of(text);
    }

    /// <summary>Parses a projected enum name into a full spec (custom hue, icon, variant).</summary>
    public static LyoChipSpec FromEnum<T>(string? text, Func<T, LyoChipSpec> spec)
        where T : struct, Enum
    {
        if (Enum.TryParse<T>(text, ignoreCase: true, out var value))
            return spec(value);

        return Of(text);
    }

    /// <summary>Maps a nullable bool onto labels and colors. Null is an em dash with <see cref="Color.Default" />.</summary>
    public static LyoChipSpec FromBool(bool? value, string trueLabel, string falseLabel, Color trueColor, Color falseColor)
        => value switch {
            true => Of(trueLabel, trueColor),
            false => Of(falseLabel, falseColor),
            var _ => Of("—")
        };
}
