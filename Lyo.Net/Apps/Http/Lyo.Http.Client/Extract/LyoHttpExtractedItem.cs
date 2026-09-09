using System.Text.Json.Serialization;

namespace Lyo.Http.Client.Extract;

/// <summary>One extract hit: a URL and/or text, plus the selector and raw token.</summary>
public sealed class LyoHttpExtractedItem
{
    /// <summary>Resolved URL when the token looks like one.</summary>
    public string? Url { get; init; }

    /// <summary>Inner text, alt, or title.</summary>
    public string? Text { get; init; }

    /// <summary>Attribute that was read (src, href, …).</summary>
    public string? Attribute { get; init; }

    /// <summary>Attribute value before URL resolution.</summary>
    public string? AttributeValue { get; init; }

    /// <summary>CSS selector that matched.</summary>
    public string? Selector { get; init; }

    /// <summary>Original token (full srcset candidate, and so on).</summary>
    public string? Raw { get; init; }

    /// <summary>Extra facts (srcset width, JSON-LD @type, og:image:width, …).</summary>
    [JsonExtensionData]
    public Dictionary<string, object?>? Metadata { get; init; }
}
