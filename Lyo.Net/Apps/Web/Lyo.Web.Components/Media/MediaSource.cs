namespace Lyo.Web.Components.Media;

/// <summary>Normalizes viewer URLs before they are written onto <c>img</c>, <c>audio</c>, or <c>video</c>.</summary>
public static class MediaSource
{
    /// <summary>Trims <paramref name="src" />. Null, empty, and whitespace are absent.</summary>
    public static string? Normalize(string? src)
    {
        if (string.IsNullOrWhiteSpace(src))
            return null;

        return src.Trim();
    }

    /// <summary>Image URL: <paramref name="src" /> when it is present, otherwise <paramref name="poster" />. Both blank means there is nothing to show.</summary>
    public static string? ImageSrc(string? src, string? poster) => Normalize(src) ?? Normalize(poster);
}