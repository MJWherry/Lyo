namespace Lyo.Web.Automation.Playwright.Record;

/// <summary>One captured browser request, for the studio network list.</summary>
public sealed class AutomationStudioNetworkRow
{
    /// <summary>Id used to open the detail and to replay.</summary>
    public int Id { get; init; }

    /// <summary>HTTP method.</summary>
    public string Method { get; init; } = "";

    /// <summary>Request URL.</summary>
    public string Url { get; init; } = "";

    /// <summary>Response status, when the response has arrived.</summary>
    public int? Status { get; init; }

    /// <summary>Browser resource type, such as <c>fetch</c>, <c>document</c>, or <c>image</c>.</summary>
    public string ResourceType { get; init; } = "";
}

/// <summary>Headers and bodies for one captured request.</summary>
public sealed class AutomationStudioNetworkDetail
{
    /// <summary>Id of the captured request.</summary>
    public int Id { get; init; }

    /// <summary>HTTP method.</summary>
    public string Method { get; init; } = "";

    /// <summary>Request URL.</summary>
    public string Url { get; init; } = "";

    /// <summary>Response status, when the response has arrived.</summary>
    public int? Status { get; init; }

    /// <summary>Request body text.</summary>
    public string? RequestBody { get; init; }

    /// <summary>Response body text.</summary>
    public string? ResponseBody { get; init; }

    /// <summary>Request headers.</summary>
    public IReadOnlyDictionary<string, string> RequestHeaders { get; init; } = new Dictionary<string, string>();

    /// <summary>Response headers.</summary>
    public IReadOnlyDictionary<string, string> ResponseHeaders { get; init; } = new Dictionary<string, string>();
}

/// <summary>The element under the cursor, or the last one the grabber described.</summary>
public sealed class AutomationStudioElementInfo
{
    /// <summary>Tag name.</summary>
    public string? Tag { get; set; }

    /// <summary>Visible text.</summary>
    public string? Text { get; set; }

    /// <summary>Attributes on the element.</summary>
    public Dictionary<string, string>? Attributes { get; set; }

    /// <summary>Link URLs inside the element.</summary>
    public List<string>? Links { get; set; }

    /// <summary>Image URLs inside the element.</summary>
    public List<string>? Images { get; set; }

    /// <summary>Page URL.</summary>
    public string? Url { get; set; }

    /// <summary>Page title.</summary>
    public string? Title { get; set; }

    /// <summary>Locator the studio can use again to highlight or act on this element.</summary>
    public AutomationStudioLocator? Locator { get; set; }
}

/// <summary>Kind and value of a locator returned by the page tool.</summary>
public sealed class AutomationStudioLocator
{
    /// <summary><see cref="Lyo.Web.Automation.Models.Enums.ElementLocatorKind" /> name.</summary>
    public string? Kind { get; set; }

    /// <summary>Locator value.</summary>
    public string? Value { get; set; }
}