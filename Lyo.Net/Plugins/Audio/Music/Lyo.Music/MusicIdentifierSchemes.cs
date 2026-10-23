using System.Text;

namespace Lyo.Music;

/// <summary>Well-known music identifier schemes and normalization.</summary>
public static class MusicIdentifierSchemes
{
    public const string Barcode = "barcode";

    public const string CatalogNumber = "catalog-number";

    public const string MusicBrainzRelease = "musicbrainz-release";

    public const string MusicBrainzReleaseGroup = "musicbrainz-release-group";

    public const string Custom = "custom";

    /// <summary>Digits-only for <see cref="Barcode" />; otherwise the trimmed value.</summary>
    public static string Normalize(string scheme, string value)
    {
        if (value is null)
            return string.Empty;

        var trimmed = value.Trim();
        if (trimmed.Length == 0)
            return string.Empty;

        if (!string.Equals(scheme, Barcode, StringComparison.OrdinalIgnoreCase))
            return trimmed;

        var buffer = new StringBuilder(trimmed.Length);
        for (var i = 0; i < trimmed.Length; i++) {
            var c = trimmed[i];
            if (char.IsDigit(c))
                buffer.Append(c);
        }

        return buffer.ToString();
    }
}