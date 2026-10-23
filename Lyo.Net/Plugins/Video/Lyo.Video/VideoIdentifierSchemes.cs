namespace Lyo.Video;

/// <summary>Well-known video identifier schemes and normalization.</summary>
public static class VideoIdentifierSchemes
{
    public const string Imdb = "imdb";

    public const string Tmdb = "tmdb";

    public const string Tvdb = "tvdb";

    public const string Eidr = "eidr";

    public const string Custom = "custom";

    /// <summary>Trims the value. <see cref="Imdb" />, <see cref="Tmdb" />, and <see cref="Tvdb" /> are also lowercased.</summary>
    public static string Normalize(string scheme, string value)
    {
        if (value is null)
            return string.Empty;

        var trimmed = value.Trim();
        if (trimmed.Length == 0)
            return string.Empty;

        if (string.Equals(scheme, Imdb, StringComparison.OrdinalIgnoreCase) || string.Equals(scheme, Tmdb, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(scheme, Tvdb, StringComparison.OrdinalIgnoreCase))
            return trimmed.ToLowerInvariant();

        return trimmed;
    }
}