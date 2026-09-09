namespace Lyo.Comic.Web.Components;

/// <summary>Wires opaque comic cover refs (typically stored file IDs) to URLs usable as image sources.</summary>
public static class ComicCoverUrls
{
    /// <summary>
    /// Gives a display URL for a cover ref. When <paramref name="coverImageRef" /> parses as a GUID, returns <c>/comic-files/{id}</c>; otherwise returns the ref as-is (e.g.
    /// complete URL).
    /// </summary>
    public static string? Resolve(string? coverImageRef)
    {
        if (string.IsNullOrWhiteSpace(coverImageRef))
            return null;

        return Guid.TryParse(coverImageRef, out var id) ? $"/comic-files/{id:D}" : coverImageRef;
    }
}