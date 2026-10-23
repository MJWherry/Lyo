namespace Lyo.Podcast;

/// <summary>Well-known podcast identifier schemes and normalization.</summary>
public static class PodcastIdentifierSchemes
{
    public const string Itunes = "itunes";

    public const string PodcastGuid = "podcast-guid";

    public const string EpisodeGuid = "episode-guid";

    public const string Custom = "custom";

    /// <summary>Trims the value. <see cref="Itunes" />, <see cref="PodcastGuid" />, and <see cref="EpisodeGuid" /> are lowercased. <see cref="Custom" /> stays trim-only.</summary>
    public static string Normalize(string scheme, string value)
    {
        if (value is null)
            return string.Empty;

        var trimmed = value.Trim();
        if (trimmed.Length == 0)
            return string.Empty;

        if (string.Equals(scheme, Itunes, StringComparison.OrdinalIgnoreCase) || string.Equals(scheme, PodcastGuid, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(scheme, EpisodeGuid, StringComparison.OrdinalIgnoreCase))
            return trimmed.ToLowerInvariant();

        return trimmed;
    }
}