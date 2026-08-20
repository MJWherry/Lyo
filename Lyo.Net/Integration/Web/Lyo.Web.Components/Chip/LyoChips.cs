namespace Lyo.Web.Components;

/// <summary>Builds <see cref="LyoChipSpec" /> values for grid cells and status chips.</summary>
public static class LyoChips
{
    /// <summary>Blank or whitespace labels become an em dash.</summary>
    public static LyoChipSpec Of(string? label, Color color = Color.Default, string? icon = null, Variant variant = Variant.Filled)
        => new(string.IsNullOrWhiteSpace(label) ? "—" : label, color, icon, variant);

    /// <summary>
    /// Parses a projected enum name. Unknown or blank values keep the raw text (or an em dash) and <see cref="Color.Default" />.
    /// </summary>
    public static LyoChipSpec FromEnum<T>(string? text, Func<T, Color> color, Func<T, string>? icon = null)
        where T : struct, Enum
    {
        if (Enum.TryParse<T>(text, ignoreCase: true, out var value))
            return new(value.ToString(), color(value), icon?.Invoke(value));

        return Of(text);
    }

    /// <summary>Maps a nullable bool to labels and colors. Null is an em dash with <see cref="Color.Default" />.</summary>
    public static LyoChipSpec FromBool(bool? value, string trueLabel, string falseLabel, Color trueColor, Color falseColor)
        => value switch {
            true => Of(trueLabel, trueColor),
            false => Of(falseLabel, falseColor),
            var _ => Of("—")
        };
}
