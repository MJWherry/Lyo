namespace Lyo.Http.Client.Extract;

/// <summary>Flags for <see cref="LyoHttpDocumentExtractor" />.</summary>
public sealed class LyoHttpExtractOptions
{
    /// <summary>Default attributes probed for sources and images.</summary>
    public static readonly string[] DefaultSourceAttributes = ["src", "href", "data-src", "data-lazy-src", "data-srcset", "data-original", "srcset"];

    /// <summary>CSS selector. Required for sources/attribute/text/html/table.</summary>
    public string? Selector { get; set; }

    /// <summary>Attributes to read. Defaults to <see cref="DefaultSourceAttributes" />.</summary>
    public string[] Attributes { get; set; } = DefaultSourceAttributes.ToArray();

    /// <summary>Resolve relative URLs against the document or <see cref="LinkResolutionBaseUri" />.</summary>
    public bool ResolveRelativeUrls { get; set; } = true;

    /// <summary>Drop duplicate URLs / strings.</summary>
    public bool Deduplicate { get; set; } = true;

    /// <summary>Split comma-separated attribute values (srcset-like lists).</summary>
    public bool SplitCommaSeparatedValues { get; set; } = true;

    /// <summary>How to expand srcset candidates.</summary>
    public LyoHttpSrcsetMode SrcsetMode { get; set; } = LyoHttpSrcsetMode.First;

    /// <summary>When set, extract from this string variable instead of the current response body.</summary>
    public string? FromVariable { get; set; }

    /// <summary>Base URI for relative resolution when the document has no <c>&lt;base&gt;</c>.</summary>
    public string? LinkResolutionBaseUri { get; set; }

    /// <summary>File-extension filter for links (e.g. <c>.zip</c>, <c>.pdf</c>).</summary>
    public string[]? FileExtensionFilter { get; set; }

    /// <summary>Host filter for links (suffix match).</summary>
    public string[]? HostFilter { get; set; }

    /// <summary>When set, skip <c>a[rel]</c> tokens in this list (e.g. <c>nofollow</c>).</summary>
    public string[]? RelFilter { get; set; }

    /// <summary>Regex pattern for <c>extractRegex</c>.</summary>
    public string? Pattern { get; set; }

    /// <summary>Regex group name or index (default 1, or 0 when unnamed).</summary>
    public string? Group { get; set; }

    /// <summary>JSON path like <c>$.items[*].url</c> (dot + optional <c>[n]</c> / <c>[*]</c>).</summary>
    public string? JsonPath { get; set; }

    /// <summary>Optional variable to store the full item list JSON (<c>variableName + "Items"</c> when null).</summary>
    public string? SaveItemsAs { get; set; }
}
