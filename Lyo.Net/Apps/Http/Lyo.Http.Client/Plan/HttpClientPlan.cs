using System.Diagnostics;
using System.Text.Json.Serialization;
using Lyo.Http.Client.Extract;
using Lyo.Http.Client.Session;

namespace Lyo.Http.Client.Plan;

/// <summary>Serializable HTTP plan: a name and an ordered list of steps.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record HttpClientPlan(string? Name, IReadOnlyList<HttpClientPlanStep> Steps)
{
    /// <inheritdoc />
    public override string ToString()
    {
        var label = string.IsNullOrWhiteSpace(Name) ? "(unnamed)" : Name!;
        return $"HttpClientPlan \"{label}\": {Steps.Count} step(s)";
    }
}

/// <summary>One step in an <see cref="HttpClientPlan" />. JSON uses polymorphic <c>type</c>.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(HttpRequestPlanStep), "request")]
[JsonDerivedType(typeof(HttpDelayPlanStep), "delay")]
[JsonDerivedType(typeof(HttpDownloadFilePlanStep), "downloadFile")]
[JsonDerivedType(typeof(HttpDownloadUrlsPlanStep), "downloadUrls")]
[JsonDerivedType(typeof(HttpExtractJsonPlanStep), "extractJson")]
[JsonDerivedType(typeof(HttpExtractJsonPathPlanStep), "extractJsonPath")]
[JsonDerivedType(typeof(HttpExtractJsonArrayPlanStep), "extractJsonArray")]
[JsonDerivedType(typeof(HttpExtractJsonRecordsPlanStep), "extractJsonRecords")]
[JsonDerivedType(typeof(HttpExtractHeaderPlanStep), "extractHeader")]
[JsonDerivedType(typeof(HttpExtractSourcesPlanStep), "extractSources")]
[JsonDerivedType(typeof(HttpExtractImagesPlanStep), "extractImages")]
[JsonDerivedType(typeof(HttpExtractLinksPlanStep), "extractLinks")]
[JsonDerivedType(typeof(HttpExtractAttributePlanStep), "extractAttribute")]
[JsonDerivedType(typeof(HttpExtractTextPlanStep), "extractText")]
[JsonDerivedType(typeof(HttpExtractHtmlPlanStep), "extractHtml")]
[JsonDerivedType(typeof(HttpExtractRegexPlanStep), "extractRegex")]
[JsonDerivedType(typeof(HttpExtractMetaPlanStep), "extractMeta")]
[JsonDerivedType(typeof(HttpExtractJsonLdPlanStep), "extractJsonLd")]
[JsonDerivedType(typeof(HttpExtractTablePlanStep), "extractTable")]
[JsonDerivedType(typeof(HttpFilterListPlanStep), "filterList")]
[JsonDerivedType(typeof(HttpMapListPlanStep), "mapList")]
[JsonDerivedType(typeof(HttpStoreLiteralPlanStep), "storeLiteral")]
[JsonDerivedType(typeof(HttpStoreTemplatePlanStep), "storeTemplate")]
[JsonDerivedType(typeof(HttpSetHeaderPlanStep), "setHeader")]
[JsonDerivedType(typeof(HttpRemoveHeaderPlanStep), "removeHeader")]
[JsonDerivedType(typeof(HttpSetCookiePlanStep), "setCookie")]
[JsonDerivedType(typeof(HttpRemoveCookiePlanStep), "removeCookie")]
[JsonDerivedType(typeof(HttpClearCookiesPlanStep), "clearCookies")]
[JsonDerivedType(typeof(HttpExportCookiesPlanStep), "exportCookies")]
[JsonDerivedType(typeof(HttpLoadCookiesPlanStep), "loadCookies")]
public abstract record HttpClientPlanStep(string? Name = null)
{
    /// <summary>Stable id assigned by the builder when omitted.</summary>
    public Guid? StepId { get; init; }
}

/// <summary>HTTP request (GET/POST/PUT/PATCH/DELETE).</summary>
public sealed record HttpRequestPlanStep(
    string Method,
    string Uri,
    string? Name = null) : HttpClientPlanStep(Name)
{
    /// <summary>Extra request headers.</summary>
    public Dictionary<string, string>? Headers { get; init; }

    /// <summary>Header names to omit even if the session bag has them.</summary>
    public List<string>? HeadersToRemove { get; init; }

    /// <summary>When true, do not copy session/default headers onto this request.</summary>
    public bool ClearHeaders { get; init; }

    /// <summary>Cookies to send on this request (in addition to the jar, unless <see cref="CookiesToRemove" />).</summary>
    public List<LyoHttpCookie>? Cookies { get; init; }

    /// <summary>Cookie names to omit on this request.</summary>
    public List<string>? CookiesToRemove { get; init; }

    /// <summary>JSON body (serialized object or raw JSON string).</summary>
    public string? JsonBody { get; init; }

    /// <summary>Application/x-www-form-urlencoded fields.</summary>
    public Dictionary<string, string>? Form { get; init; }

    /// <summary>Raw body text.</summary>
    public string? RawBody { get; init; }

    /// <summary>Query string template appended to <see cref="Uri" />.</summary>
    public string? Query { get; init; }

    /// <summary>Per-step timeout.</summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>Statuses treated as success in addition to 2xx.</summary>
    public int[]? AllowStatuses { get; init; }

    /// <summary>Override handler delay for this step.</summary>
    public TimeSpan? DelayMin { get; init; }

    /// <summary>Override handler delay max for this step.</summary>
    public TimeSpan? DelayMax { get; init; }

    /// <summary>Skip the timing handler for this step.</summary>
    public bool NoDelay { get; init; }

    /// <summary>Extracts to run against this response before the next step.</summary>
    public IReadOnlyList<HttpClientPlanStep>? Then { get; init; }

    /// <summary>Failure policy.</summary>
    public HttpStepFailure OnFailure { get; init; } = HttpStepFailure.Stop;

    /// <summary>Retry count on failure (linear backoff when <see cref="RetryBackoff" /> is set).</summary>
    public int RetryCount { get; init; }

    /// <summary>Backoff between retries.</summary>
    public TimeSpan? RetryBackoff { get; init; }

    /// <summary>When true, wait using Retry-After on 429 then retry this step.</summary>
    public bool OnRetryAfter { get; init; }

    /// <summary>Save the response body into this variable.</summary>
    public string? SaveBodyAs { get; init; }
}

/// <summary>Wait between steps (on top of the timing handler).</summary>
public sealed record HttpDelayPlanStep(TimeSpan Min, TimeSpan? Max = null, string? Name = null) : HttpClientPlanStep(Name);

/// <summary>Download one URL to a file.</summary>
public sealed record HttpDownloadFilePlanStep(string Uri, string Destination, string? Name = null) : HttpClientPlanStep(Name)
{
    /// <summary>Optional variable to store the destination path.</summary>
    public string? SavePathAs { get; init; }
}

/// <summary>Download every URL in a string-list variable into a directory.</summary>
public sealed record HttpDownloadUrlsPlanStep(string VariableName, string Directory, string? Prefix = null, string? Name = null) : HttpClientPlanStep(Name);

/// <summary>Parse the current (or named) body as JSON into a variable.</summary>
public sealed record HttpExtractJsonPlanStep(string VariableName, string? FromVariable = null, string? Name = null) : HttpClientPlanStep(Name);

/// <summary>JSON path into a variable.</summary>
public sealed record HttpExtractJsonPathPlanStep(string VariableName, string JsonPath, string? FromVariable = null, string? Name = null) : HttpClientPlanStep(Name);

/// <summary>JSON array into a string-list variable.</summary>
public sealed record HttpExtractJsonArrayPlanStep(string VariableName, string? JsonPath = null, string? FromVariable = null, string? Name = null) : HttpClientPlanStep(Name);

/// <summary>Array of objects into a JSON variable.</summary>
public sealed record HttpExtractJsonRecordsPlanStep(string VariableName, string? JsonPath = null, string? FromVariable = null, string? Name = null) : HttpClientPlanStep(Name);

/// <summary>Copy a response header into a variable.</summary>
public sealed record HttpExtractHeaderPlanStep(string HeaderName, string VariableName, string? Name = null) : HttpClientPlanStep(Name);

/// <summary>CSS + attributes → URL list.</summary>
public sealed record HttpExtractSourcesPlanStep(string VariableName, LyoHttpExtractOptions Options, string? Name = null) : HttpClientPlanStep(Name);

/// <summary>Image URLs.</summary>
public sealed record HttpExtractImagesPlanStep(string VariableName, LyoHttpExtractOptions? Options = null, string? Name = null) : HttpClientPlanStep(Name);

/// <summary>Link URLs.</summary>
public sealed record HttpExtractLinksPlanStep(string VariableName, LyoHttpExtractOptions? Options = null, string? Name = null) : HttpClientPlanStep(Name);

/// <summary>One attribute.</summary>
public sealed record HttpExtractAttributePlanStep(string VariableName, LyoHttpExtractOptions Options, string? Name = null) : HttpClientPlanStep(Name);

/// <summary>Inner text.</summary>
public sealed record HttpExtractTextPlanStep(string VariableName, LyoHttpExtractOptions Options, string? Name = null) : HttpClientPlanStep(Name);

/// <summary>Inner HTML.</summary>
public sealed record HttpExtractHtmlPlanStep(string VariableName, LyoHttpExtractOptions Options, string? Name = null) : HttpClientPlanStep(Name);

/// <summary>Regex.</summary>
public sealed record HttpExtractRegexPlanStep(string VariableName, string Pattern, string? Group = null, string? FromVariable = null, string? Name = null)
    : HttpClientPlanStep(Name);

/// <summary>Meta / OG / canonical. <paramref name="VariablePrefix" /> prefixes stored keys (e.g. <c>og</c>).</summary>
public sealed record HttpExtractMetaPlanStep(string VariablePrefix, LyoHttpExtractOptions? Options = null, string? Name = null) : HttpClientPlanStep(Name);

/// <summary>JSON-LD.</summary>
public sealed record HttpExtractJsonLdPlanStep(string VariableName, LyoHttpExtractOptions? Options = null, string? Name = null) : HttpClientPlanStep(Name);

/// <summary>HTML table.</summary>
public sealed record HttpExtractTablePlanStep(string VariableName, string Selector, LyoHttpExtractOptions? Options = null, string? Name = null) : HttpClientPlanStep(Name);

/// <summary>Filter a string list in bindings.</summary>
public sealed record HttpFilterListPlanStep(string SourceVariable, string TargetVariable, string? Name = null) : HttpClientPlanStep(Name)
{
    /// <summary>Keep items matching this regex.</summary>
    public string? IncludeRegex { get; init; }

    /// <summary>Drop items matching this regex.</summary>
    public string? ExcludeRegex { get; init; }

    /// <summary>Skip this many items.</summary>
    public int Skip { get; init; }

    /// <summary>Take this many items (0 = all remaining).</summary>
    public int Take { get; init; }

    /// <summary>Distinct after filters.</summary>
    public bool Distinct { get; init; } = true;
}

/// <summary>Map a string list (prefix/suffix/template).</summary>
public sealed record HttpMapListPlanStep(string SourceVariable, string TargetVariable, string? Prefix = null, string? Suffix = null, string? Name = null)
    : HttpClientPlanStep(Name);

/// <summary>Store a literal string.</summary>
public sealed record HttpStoreLiteralPlanStep(string VariableName, string Value, string? Name = null) : HttpClientPlanStep(Name);

/// <summary>Store an interpolated template.</summary>
public sealed record HttpStoreTemplatePlanStep(string VariableName, string Template, string? Name = null) : HttpClientPlanStep(Name);

/// <summary>Set a session default header for later steps.</summary>
public sealed record HttpSetHeaderPlanStep(string HeaderName, string Value, string? Name = null) : HttpClientPlanStep(Name);

/// <summary>Remove a session default header.</summary>
public sealed record HttpRemoveHeaderPlanStep(string HeaderName, string? Name = null) : HttpClientPlanStep(Name);

/// <summary>Add a cookie to the jar.</summary>
public sealed record HttpSetCookiePlanStep(LyoHttpCookie Cookie, string? Name = null) : HttpClientPlanStep(Name);

/// <summary>Remove a cookie from the jar.</summary>
public sealed record HttpRemoveCookiePlanStep(string CookieName, string? Domain = null, string? Name = null) : HttpClientPlanStep(Name);

/// <summary>Empty the jar.</summary>
public sealed record HttpClearCookiesPlanStep(string? Name = null) : HttpClientPlanStep(Name);

/// <summary>Export the jar into a JSON variable (not for committed secrets).</summary>
public sealed record HttpExportCookiesPlanStep(string VariableName, string? Name = null) : HttpClientPlanStep(Name);

/// <summary>Load cookies from a JSON variable or runtime cookie file.</summary>
public sealed record HttpLoadCookiesPlanStep(string? VariableName = null, string? CookieFile = null, string? Name = null) : HttpClientPlanStep(Name);
