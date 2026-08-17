using System.Globalization;

namespace Lyo.Formatter.Web.Components;

/// <summary>Stable per-key hues so editor tokens and preview replacements share a color. Fill/contrast is applied in CSS against the MudBlazor surface so light and dark themes both stay readable.</summary>
public static class LyoFormatterPlaceholderPalette
{
    /// <summary>Inline CSS variables for a token or chip. Unresolved keys omit a hue so CSS can use a muted surface mix.</summary>
    public static string CssVariables(string? key, bool unresolved)
    {
        if (unresolved || string.IsNullOrEmpty(key))
            return string.Empty;

        return string.Create(CultureInfo.InvariantCulture, $"--lyo-fmt-hue:{HashHue(key)}");
    }

    private static int HashHue(string key)
    {
        var hash = (uint)StringComparer.OrdinalIgnoreCase.GetHashCode(key);
        return (int)(hash % 360);
    }
}
