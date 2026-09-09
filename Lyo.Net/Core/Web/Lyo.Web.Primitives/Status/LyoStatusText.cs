using System.Globalization;

namespace Lyo.Web.Primitives;

/// <summary>Status string handling shared by every <see cref="ILyoStatusPalette" />, so each one does not reinvent casing and separator rules.</summary>
public static class LyoStatusText
{
    /// <summary>
    /// Lowercases <paramref name="status" /> and collapses underscores, hyphens, and runs of whitespace to a single space, so <c>Partially_Delivered</c>,
    /// <c>partially-delivered</c>, and <c>Partially Delivered</c> collapse to one key. Returns an empty string for null or blank input.
    /// </summary>
    public static string Normalize(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return string.Empty;

        var builder = new System.Text.StringBuilder(status.Length);
        var pendingSeparator = false;
        foreach (var character in status) {
            if (character is '_' or '-' || char.IsWhiteSpace(character)) {
                pendingSeparator = builder.Length > 0;
                continue;
            }

            if (pendingSeparator) {
                builder.Append(' ');
                pendingSeparator = false;
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }

    /// <summary>
    /// Title-cases a normalized key for display (<c>partially delivered</c> becomes <c>Partially Delivered</c>). Use it when a palette has a color for a status but no
    /// nicer label than the stored text.
    /// </summary>
    public static string Humanize(string? status)
    {
        var normalized = Normalize(status);
        return normalized.Length == 0 ? string.Empty : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(normalized);
    }
}
