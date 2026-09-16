using System.Globalization;

namespace Lyo.Web.Primitives;

/// <summary>
/// Named HSL hue for identity chips (formats, kinds). <see cref="LyoChip" /> mixes <see cref="Style" /> into
/// <c>--mud-palette-surface</c> so light and dark themes keep contrast. Prefer these over Mud Primary/Secondary/Tertiary —
/// those slots are green, gray, and surface in the default Lyo palette.
/// </summary>
/// <param name="Degrees">Hue on the color wheel. Values outside 0–359 wrap via <see cref="Normalized" />.</param>
public sealed record LyoChipHue(int Degrees)
{
    /// <summary>Full turn of the HSL wheel.</summary>
    public const int CircleDegrees = 360;

    /// <summary>Inline CSS variable <see cref="LyoChip" /> reads for the mix fill.</summary>
    public const string CssVariable = "--lyo-chip-hue";

    /// <summary>Class applied when <see cref="Style" /> is present.</summary>
    public const string CssClass = "lyo-chip-hue";

    /// <summary>Rose, away from theme Error (~4°).</summary>
    public static LyoChipHue Rose { get; } = new(332);

    /// <summary>Coral, between Error and Warning.</summary>
    public static LyoChipHue Coral { get; } = new(18);

    /// <summary>Warm amber, past Warning (~36°).</summary>
    public static LyoChipHue Amber { get; } = new(52);

    /// <summary>Lime, before theme Success (~122°).</summary>
    public static LyoChipHue Lime { get; } = new(84);

    /// <summary>Green, past theme Success.</summary>
    public static LyoChipHue Green { get; } = new(152);

    /// <summary>Teal.</summary>
    public static LyoChipHue Teal { get; } = new(174);

    /// <summary>Cyan, before theme Info (~207°).</summary>
    public static LyoChipHue Cyan { get; } = new(192);

    /// <summary>Blue, past theme Info.</summary>
    public static LyoChipHue Blue { get; } = new(228);

    /// <summary>Indigo.</summary>
    public static LyoChipHue Indigo { get; } = new(252);

    /// <summary>Violet.</summary>
    public static LyoChipHue Violet { get; } = new(274);

    /// <summary>Purple.</summary>
    public static LyoChipHue Purple { get; } = new(292);

    /// <summary>Magenta.</summary>
    public static LyoChipHue Magenta { get; } = new(312);

    /// <summary>Named hues in increasing wheel order, from <see cref="Coral" /> through <see cref="Rose" />.</summary>
    public static IReadOnlyList<LyoChipHue> All { get; } = [
        Coral, Amber, Lime, Green, Teal, Cyan, Blue, Indigo, Violet, Purple, Magenta, Rose
    ];

    /// <summary><see cref="Degrees" /> wrapped onto 0–359.</summary>
    public int Normalized
    {
        get
        {
            var hue = Degrees % CircleDegrees;
            if (hue < 0)
                hue += CircleDegrees;
            return hue;
        }
    }

    /// <summary>
    /// Inline style for the chip. Sets <see cref="CssVariable" /> and mixes that hue into the Mud surface, so the same style follows light and dark themes.
    /// </summary>
    public string Style
    {
        get
        {
            var color = string.Create(CultureInfo.InvariantCulture, $"hsl(var({CssVariable}) 72% 50%)");
            var fill = string.Create(CultureInfo.InvariantCulture, $"color-mix(in srgb, {color} 62%, var(--mud-palette-surface))");
            var edge = string.Create(CultureInfo.InvariantCulture, $"color-mix(in srgb, {color} 80%, var(--mud-palette-lines-default))");
            return string.Create(CultureInfo.InvariantCulture, $"{CssVariable}:{Normalized};background-color:{fill};color:var(--mud-palette-text-primary);border-color:{edge}");
        }
    }
}
