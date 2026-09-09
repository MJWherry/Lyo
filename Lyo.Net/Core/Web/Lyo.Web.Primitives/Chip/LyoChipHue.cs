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
    public static LyoChipHue Rose { get; } = new(338);

    /// <summary>Warm amber.</summary>
    public static LyoChipHue Amber { get; } = new(42);

    /// <summary>Lime, away from theme Success (~122°).</summary>
    public static LyoChipHue Lime { get; } = new(88);

    /// <summary>Teal.</summary>
    public static LyoChipHue Teal { get; } = new(172);

    /// <summary>Cyan.</summary>
    public static LyoChipHue Cyan { get; } = new(188);

    /// <summary>Blue.</summary>
    public static LyoChipHue Blue { get; } = new(205);

    /// <summary>Indigo.</summary>
    public static LyoChipHue Indigo { get; } = new(230);

    /// <summary>Violet.</summary>
    public static LyoChipHue Violet { get; } = new(268);

    /// <summary>Magenta.</summary>
    public static LyoChipHue Magenta { get; } = new(300);

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

    /// <summary>Inline style assigning <see cref="CssVariable" />.</summary>
    public string Style => string.Create(CultureInfo.InvariantCulture, $"{CssVariable}:{Normalized}");
}
