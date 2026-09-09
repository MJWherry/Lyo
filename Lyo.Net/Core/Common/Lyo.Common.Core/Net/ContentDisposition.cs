namespace Lyo.Common.Core.Net;

/// <summary>
/// Builds <c>Content-Disposition</c> values that keep non-ASCII file names intact, emitting both the legacy quoted <c>filename</c> and the RFC 5987 <c>filename*</c> form so
/// old and new user agents each pick the parameter they understand.
/// </summary>
/// <remarks>
/// <para>The supplied name is reduced to its leaf segment, so a caller can pass a stored path without leaking directory structure into the download name.</para>
/// </remarks>
public static class ContentDisposition
{
    /// <summary>Download name used when the caller supplies nothing usable.</summary>
    public const string FallbackFileName = "download";

    /// <summary>Renders <c>attachment; filename="..."; filename*=UTF-8''...</c>, prompting the browser to save rather than display.</summary>
    /// <param name="fileName">Desired download name; only the leaf segment is used. Null, empty, or path-only values fall back to <see cref="FallbackFileName" />.</param>
    public static string Attachment(string? fileName) => Build(fileName, false);

    /// <summary>Renders <c>inline; filename="..."; filename*=UTF-8''...</c>, letting the browser show the payload when it can.</summary>
    /// <param name="fileName">Desired download name; only the leaf segment is used. Null, empty, or path-only values fall back to <see cref="FallbackFileName" />.</param>
    public static string Inline(string? fileName) => Build(fileName, true);

    /// <summary>Renders either disposition from <paramref name="inline" />, for callers that carry the choice as a flag.</summary>
    /// <param name="fileName">Desired download name; only the leaf segment is used. Null, empty, or path-only values fall back to <see cref="FallbackFileName" />.</param>
    /// <param name="inline">When <see langword="true" /> emits <c>inline</c>, otherwise <c>attachment</c>.</param>
    public static string Build(string? fileName, bool inline)
    {
        var leaf = Path.GetFileName((fileName ?? FallbackFileName).Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(leaf))
            leaf = FallbackFileName;

        // Quotes would end the quoted-string parameter early; RFC 6266 has no escape, so downgrade them.
        var ascii = leaf!.Replace("\"", "'");
        return $"{(inline ? "inline" : "attachment")}; filename=\"{ascii}\"; filename*=UTF-8''{Uri.EscapeDataString(leaf)}";
    }
}
