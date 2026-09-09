using System.Text.Json;
using Lyo.Common.Core.Net;
using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;
using Lyo.Http.Client.Extract;
using Lyo.Http.Client.Session;
using Lyo.Result;

namespace Lyo.Http.Client.Plan;

/// <summary>
/// The only fluent HTTP API. <see cref="Build" /> serializes; <see cref="RunAsync(CancellationToken)" /> executes.
/// Parse/extract attach to the open request. <see cref="Commit" /> seals it. A new Get/Post/Download/Delay auto-commits.
/// </summary>
public sealed class HttpClientPlanBuilder
{
    private readonly List<HttpClientPlanStep> _steps = [];
    private readonly string? _name;
    private readonly ILyoHttpClient? _client;
    private HttpRequestPlanStep? _open;
    private readonly List<HttpClientPlanStep> _openThen = [];

    private HttpClientPlanBuilder(string? name, ILyoHttpClient? client)
    {
        _name = name;
        _client = client;
    }

    /// <summary>Starts a named (or unnamed) plan. Pass <paramref name="client" /> so <see cref="RunAsync(CancellationToken)" /> does not need a separate runner call.</summary>
    public static HttpClientPlanBuilder New(string? name = null, ILyoHttpClient? client = null) => new(name, client);

    /// <summary>GET <paramref name="uri" />. Auto-commits an open step.</summary>
    public HttpClientPlanBuilder Get(string uri) => Request("GET", uri);

    /// <summary>POST <paramref name="uri" />.</summary>
    public HttpClientPlanBuilder Post(string uri) => Request("POST", uri);

    /// <summary>PUT <paramref name="uri" />.</summary>
    public HttpClientPlanBuilder Put(string uri) => Request("PUT", uri);

    /// <summary>PATCH <paramref name="uri" />.</summary>
    public HttpClientPlanBuilder Patch(string uri) => Request("PATCH", uri);

    /// <summary>DELETE <paramref name="uri" />.</summary>
    public HttpClientPlanBuilder Delete(string uri) => Request("DELETE", uri);

    /// <summary>Arbitrary method.</summary>
    public HttpClientPlanBuilder Request(string method, string uri)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(method);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(uri);
        AutoCommit();
        _open = new(method, uri) { StepId = Guid.NewGuid() };
        return this;
    }

    /// <summary>Adds a request header on the open step.</summary>
    public HttpClientPlanBuilder WithHeader(string name, string value)
    {
        MutateOpen(s => s with { Headers = Copy(s.Headers, name, value) });
        return this;
    }

    /// <summary>Adds several headers.</summary>
    public HttpClientPlanBuilder WithHeaders(IEnumerable<KeyValuePair<string, string>> headers)
    {
        foreach (var pair in headers)
            WithHeader(pair.Key, pair.Value);

        return this;
    }

    /// <summary><c>Authorization: Bearer …</c>.</summary>
    public HttpClientPlanBuilder WithBearer(string token) => WithHeader(LyoHttpHeaders.Authorization, "Bearer " + token);

    /// <summary><c>X-Api-Key</c>.</summary>
    public HttpClientPlanBuilder WithApiKey(string apiKey) => WithHeader(LyoHttpHeaders.ApiKey, apiKey);

    /// <summary><c>Accept</c>.</summary>
    public HttpClientPlanBuilder Accept(string mediaType) => WithHeader(HttpHeaderInfo.Accept, mediaType);

    /// <summary><c>Accept: application/json</c>.</summary>
    public HttpClientPlanBuilder AcceptJson() => Accept("application/json");

    /// <summary><c>Accept-Language</c>.</summary>
    public HttpClientPlanBuilder AcceptLanguage(string value) => WithHeader(HttpHeaderInfo.AcceptLanguage, value);

    /// <summary>Omit this header even if the session bag has it.</summary>
    public HttpClientPlanBuilder WithoutHeader(string name)
    {
        MutateOpen(s => {
            var list = s.HeadersToRemove?.ToList() ?? [];
            list.Add(name);
            return s with { HeadersToRemove = list };
        });
        return this;
    }

    /// <summary>This request starts from options defaults only.</summary>
    public HttpClientPlanBuilder ClearHeaders()
    {
        MutateOpen(s => s with { ClearHeaders = true, Headers = null });
        return this;
    }

    /// <summary>Cookie on this request (also added to the jar at run time).</summary>
    public HttpClientPlanBuilder WithCookie(string name, string value, string? domain = null, string? path = null)
    {
        MutateOpen(s => {
            var list = s.Cookies?.ToList() ?? [];
            list.Add(new() { Name = name, Value = value, Domain = domain, Path = path });
            return s with { Cookies = list };
        });
        return this;
    }

    /// <summary>Adds cookies on this request.</summary>
    public HttpClientPlanBuilder WithCookies(IEnumerable<LyoHttpCookie> cookies)
    {
        foreach (var cookie in cookies)
            WithCookie(cookie.Name, cookie.Value, cookie.Domain, cookie.Path);

        return this;
    }

    /// <summary>Do not send this cookie on this request.</summary>
    public HttpClientPlanBuilder WithoutCookie(string name)
    {
        MutateOpen(s => {
            var list = s.CookiesToRemove?.ToList() ?? [];
            list.Add(name);
            return s with { CookiesToRemove = list };
        });
        return this;
    }

    /// <summary>Query string (may contain <c>{{var}}</c>).</summary>
    public HttpClientPlanBuilder WithQuery(string query)
    {
        MutateOpen(s => s with { Query = query });
        return this;
    }

    /// <summary>Per-step timeout.</summary>
    public HttpClientPlanBuilder WithTimeout(TimeSpan timeout)
    {
        MutateOpen(s => s with { Timeout = timeout });
        return this;
    }

    /// <summary>JSON body.</summary>
    public HttpClientPlanBuilder WithJsonBody(string json)
    {
        MutateOpen(s => s with { JsonBody = json });
        return this;
    }

    /// <summary>JSON body from an object.</summary>
    public HttpClientPlanBuilder WithJsonBody<T>(T body, JsonSerializerOptions? options = null)
        => WithJsonBody(JsonSerializer.Serialize(body, options));

    /// <summary>Form fields.</summary>
    public HttpClientPlanBuilder WithForm(IEnumerable<KeyValuePair<string, string>> fields)
    {
        MutateOpen(s => s with { Form = fields.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal) });
        return this;
    }

    /// <summary>Raw body.</summary>
    public HttpClientPlanBuilder WithRaw(string body)
    {
        MutateOpen(s => s with { RawBody = body });
        return this;
    }

    /// <summary>Override timing for this step.</summary>
    public HttpClientPlanBuilder WithDelay(TimeSpan min, TimeSpan? max = null)
    {
        MutateOpen(s => s with { DelayMin = min, DelayMax = max ?? min, NoDelay = false });
        return this;
    }

    /// <summary>Skip the timing handler for this step.</summary>
    public HttpClientPlanBuilder WithNoDelay()
    {
        MutateOpen(s => s with { NoDelay = true });
        return this;
    }

    /// <summary>Treat these statuses as success.</summary>
    public HttpClientPlanBuilder AllowStatuses(params int[] statuses)
    {
        MutateOpen(s => s with { AllowStatuses = statuses });
        return this;
    }

    /// <summary>Save the response body into a binding.</summary>
    public HttpClientPlanBuilder ThenSaveBody(string variableName)
    {
        MutateOpen(s => s with { SaveBodyAs = variableName });
        return this;
    }

    /// <summary>Extract sources on the open request.</summary>
    public HttpClientPlanBuilder ThenExtractSources(string variableName, string? selector = null, string[]? attributes = null)
        => Then(new HttpExtractSourcesPlanStep(variableName, new() { Selector = selector, Attributes = attributes ?? LyoHttpExtractOptions.DefaultSourceAttributes.ToArray() }));

    /// <summary>Extract images.</summary>
    public HttpClientPlanBuilder ThenExtractImages(string variableName, LyoHttpExtractOptions? options = null)
        => Then(new HttpExtractImagesPlanStep(variableName, options));

    /// <summary>Extract links.</summary>
    public HttpClientPlanBuilder ThenExtractLinks(string variableName, string[]? extensionFilter = null, string[]? hostFilter = null, LyoHttpExtractOptions? options = null)
    {
        options ??= new();
        if (extensionFilter != null)
            options.FileExtensionFilter = extensionFilter;
        if (hostFilter != null)
            options.HostFilter = hostFilter;

        return Then(new HttpExtractLinksPlanStep(variableName, options));
    }

    /// <summary>Extract one attribute.</summary>
    public HttpClientPlanBuilder ThenExtractAttribute(string variableName, string selector, string attribute)
        => Then(new HttpExtractAttributePlanStep(variableName, new() { Selector = selector, Attributes = [attribute] }));

    /// <summary>Extract inner text.</summary>
    public HttpClientPlanBuilder ThenExtractText(string variableName, string selector)
        => Then(new HttpExtractTextPlanStep(variableName, new() { Selector = selector }));

    /// <summary>Extract inner HTML.</summary>
    public HttpClientPlanBuilder ThenExtractHtml(string variableName, string selector)
        => Then(new HttpExtractHtmlPlanStep(variableName, new() { Selector = selector }));

    /// <summary>Extract with a regex.</summary>
    public HttpClientPlanBuilder ThenExtractRegex(string pattern, string variableName, string? group = null)
        => Then(new HttpExtractRegexPlanStep(variableName, pattern, group));

    /// <summary>Extract meta / OG. Prefix keys with <paramref name="variablePrefix" />.</summary>
    public HttpClientPlanBuilder ThenExtractMeta(string variablePrefix)
        => Then(new HttpExtractMetaPlanStep(variablePrefix));

    /// <summary>Extract JSON-LD.</summary>
    public HttpClientPlanBuilder ThenExtractJsonLd(string variableName)
        => Then(new HttpExtractJsonLdPlanStep(variableName));

    /// <summary>Extract a table.</summary>
    public HttpClientPlanBuilder ThenExtractTable(string selector, string variableName)
        => Then(new HttpExtractTablePlanStep(variableName, selector));

    /// <summary>Parse JSON body.</summary>
    public HttpClientPlanBuilder ThenExtractJson(string variableName)
        => Then(new HttpExtractJsonPlanStep(variableName));

    /// <summary>JSON path.</summary>
    public HttpClientPlanBuilder ThenExtractJsonPath(string variableName, string jsonPath)
        => Then(new HttpExtractJsonPathPlanStep(variableName, jsonPath));

    /// <summary>JSON array → string list.</summary>
    public HttpClientPlanBuilder ThenExtractJsonArray(string variableName, string? jsonPath = null)
        => Then(new HttpExtractJsonArrayPlanStep(variableName, jsonPath));

    /// <summary>JSON records.</summary>
    public HttpClientPlanBuilder ThenExtractJsonRecords(string variableName, string? jsonPath = null)
        => Then(new HttpExtractJsonRecordsPlanStep(variableName, jsonPath));

    /// <summary>Copy a response header.</summary>
    public HttpClientPlanBuilder ThenExtractHeader(string headerName, string variableName)
        => Then(new HttpExtractHeaderPlanStep(headerName, variableName));

    /// <summary>Failure policy on the open request.</summary>
    public HttpClientPlanBuilder OnFailure(HttpStepFailure failure)
    {
        MutateOpen(s => s with { OnFailure = failure });
        return this;
    }

    /// <summary>Retry the open request.</summary>
    public HttpClientPlanBuilder OnRetry(int count, TimeSpan? backoff = null)
    {
        MutateOpen(s => s with { RetryCount = count, RetryBackoff = backoff });
        return this;
    }

    /// <summary>On 429, wait using Retry-After then retry.</summary>
    public HttpClientPlanBuilder OnRetryAfter()
    {
        MutateOpen(s => s with { OnRetryAfter = true });
        return this;
    }

    /// <summary>Allowed status (alias of <see cref="AllowStatuses" /> for a single code).</summary>
    public HttpClientPlanBuilder OnStatus(int status, Action<HttpClientPlanBuilder>? configure = null)
    {
        AllowStatuses(status);
        configure?.Invoke(this);
        return this;
    }

    /// <summary>Seals the open request (and its Then* extracts). Does not mean “and then.”</summary>
    public HttpClientPlanBuilder Commit()
    {
        FlushOpen();
        return this;
    }

    /// <summary>Filter a string list. Auto-commits the open request so extract cannot attach here.</summary>
    public HttpClientPlanBuilder FilterList(string sourceVariable, string targetVariable, string? includeRegex = null, string? excludeRegex = null, int skip = 0, int take = 0, bool distinct = true)
    {
        AutoCommit();
        _steps.Add(new HttpFilterListPlanStep(sourceVariable, targetVariable) {
            StepId = Guid.NewGuid(),
            IncludeRegex = includeRegex,
            ExcludeRegex = excludeRegex,
            Skip = skip,
            Take = take,
            Distinct = distinct
        });
        return this;
    }

    /// <summary>Map a string list.</summary>
    public HttpClientPlanBuilder MapList(string sourceVariable, string targetVariable, string? prefix = null, string? suffix = null)
    {
        AutoCommit();
        _steps.Add(new HttpMapListPlanStep(sourceVariable, targetVariable, prefix, suffix) { StepId = Guid.NewGuid() });
        return this;
    }

    /// <summary>Download one file. Auto-commits.</summary>
    public HttpClientPlanBuilder DownloadFile(string uri, string destination, string? savePathAs = null)
    {
        AutoCommit();
        _steps.Add(new HttpDownloadFilePlanStep(uri, destination) { StepId = Guid.NewGuid(), SavePathAs = savePathAs });
        return this;
    }

    /// <summary>Download every URL in a list variable. Auto-commits.</summary>
    public HttpClientPlanBuilder DownloadUrls(string variableName, string directory, string? prefix = null)
    {
        AutoCommit();
        _steps.Add(new HttpDownloadUrlsPlanStep(variableName, directory, prefix) { StepId = Guid.NewGuid() });
        return this;
    }

    /// <summary>Delay step. Auto-commits the previous step.</summary>
    public HttpClientPlanBuilder Delay(int milliseconds) => Delay(TimeSpan.FromMilliseconds(milliseconds));

    /// <summary>Delay step with a range.</summary>
    public HttpClientPlanBuilder Delay(TimeSpan min, TimeSpan? max = null)
    {
        AutoCommit();
        _steps.Add(new HttpDelayPlanStep(min, max) { StepId = Guid.NewGuid() });
        return this;
    }

    /// <summary>Set a session header for later steps. Auto-commits.</summary>
    public HttpClientPlanBuilder ThenSetHeader(string name, string value)
    {
        AutoCommit();
        _steps.Add(new HttpSetHeaderPlanStep(name, value) { StepId = Guid.NewGuid() });
        return this;
    }

    /// <summary>Remove a session header. Auto-commits.</summary>
    public HttpClientPlanBuilder ThenRemoveHeader(string name)
    {
        AutoCommit();
        _steps.Add(new HttpRemoveHeaderPlanStep(name) { StepId = Guid.NewGuid() });
        return this;
    }

    /// <summary>Add a cookie to the jar. Auto-commits.</summary>
    public HttpClientPlanBuilder ThenSetCookie(string name, string value, string? domain = null, string? path = null)
    {
        AutoCommit();
        _steps.Add(new HttpSetCookiePlanStep(new() { Name = name, Value = value, Domain = domain, Path = path }) { StepId = Guid.NewGuid() });
        return this;
    }

    /// <summary>Remove a cookie from the jar.</summary>
    public HttpClientPlanBuilder ThenRemoveCookie(string name, string? domain = null)
    {
        AutoCommit();
        _steps.Add(new HttpRemoveCookiePlanStep(name, domain) { StepId = Guid.NewGuid() });
        return this;
    }

    /// <summary>Empty the jar.</summary>
    public HttpClientPlanBuilder ThenClearCookies()
    {
        AutoCommit();
        _steps.Add(new HttpClearCookiesPlanStep { StepId = Guid.NewGuid() });
        return this;
    }

    /// <summary>Export cookies into a JSON variable.</summary>
    public HttpClientPlanBuilder ThenExportCookies(string variableName)
    {
        AutoCommit();
        _steps.Add(new HttpExportCookiesPlanStep(variableName) { StepId = Guid.NewGuid() });
        return this;
    }

    /// <summary>Load cookies from a variable or runtime cookie file.</summary>
    public HttpClientPlanBuilder ThenLoadCookies(string? variableName = null, string? cookieFile = null)
    {
        AutoCommit();
        _steps.Add(new HttpLoadCookiesPlanStep(variableName, cookieFile) { StepId = Guid.NewGuid() });
        return this;
    }

    /// <summary>Store a literal.</summary>
    public HttpClientPlanBuilder StoreLiteral(string variableName, string value)
    {
        AutoCommit();
        _steps.Add(new HttpStoreLiteralPlanStep(variableName, value) { StepId = Guid.NewGuid() });
        return this;
    }

    /// <summary>Store an interpolated template.</summary>
    public HttpClientPlanBuilder StoreTemplate(string variableName, string template)
    {
        AutoCommit();
        _steps.Add(new HttpStoreTemplatePlanStep(variableName, template) { StepId = Guid.NewGuid() });
        return this;
    }

    /// <summary>Seals a dangling last step and returns the immutable plan.</summary>
    public HttpClientPlan Build()
    {
        FlushOpen();
        return new(_name, _steps.ToArray());
    }

    /// <summary>Builds and runs against the client passed to <see cref="New" />.</summary>
    public Task<HttpClientPlanRunResult> RunAsync(CancellationToken ct = default)
        => RunAsync(_client ?? throw new InvalidOperationException("Pass a client to HttpClientPlanBuilder.New or use ILyoHttpClient.RunAsync."), null, ct);

    /// <summary>Builds and runs against <paramref name="client" />.</summary>
    public Task<HttpClientPlanRunResult> RunAsync(ILyoHttpClient client, HttpClientPlanRuntime? runtime = null, CancellationToken ct = default)
        => new HttpClientPlanRunner().RunAsync(client, Build(), runtime, ct: ct);

    /// <summary>One-step run that deserializes the last body as <typeparamref name="T" />.</summary>
    public async Task<T?> RunAsAsync<T>(CancellationToken ct = default)
    {
        var result = await RunAsync(ct).ConfigureAwait(false);
        if (string.IsNullOrEmpty(result.LastBody))
            return default;

        return JsonSerializer.Deserialize<T>(result.LastBody!, _client?.GetSerializerOptions());
    }

    /// <summary>One-step run wrapped in <see cref="Result{T}" />.</summary>
    public async Task<Result<T>> RunAsResultAsync<T>(CancellationToken ct = default)
    {
        try {
            var value = await RunAsAsync<T>(ct).ConfigureAwait(false);
            return value is null ? Result<T>.Failure("Deserialization returned null.", "http.client.null") : Result<T>.Success(value);
        }
        catch (Exception ex) {
            return Result<T>.Failure(ex);
        }
    }

    /// <summary>One-step run returning the last body string.</summary>
    public async Task<string> RunAsStringAsync(CancellationToken ct = default)
    {
        var result = await RunAsync(ct).ConfigureAwait(false);
        return result.LastBody ?? "";
    }

    /// <summary>Builds, then downloads the last GET to a file (use <see cref="DownloadFile" /> for plan steps).</summary>
    public async Task<string> DownloadToFileAsync(string destinationPath, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(destinationPath);
        var plan = Build();
        var request = plan.Steps.OfType<HttpRequestPlanStep>().LastOrDefault()
            ?? throw new InvalidOperationException("DownloadToFileAsync needs a request step.");
        var client = _client ?? throw new InvalidOperationException("Pass a client to HttpClientPlanBuilder.New.");
        return await client.DownloadToFileAsync(request.Uri, destinationPath, ct: ct).ConfigureAwait(false);
    }

    private HttpClientPlanBuilder Then(HttpClientPlanStep step)
    {
        if (_open == null)
            throw new InvalidOperationException("ThenExtract*/ThenParse* attach to the open request. Call Get/Post first, or Commit() and use ExtractFrom via FromVariable.");

        _openThen.Add(step);
        return this;
    }

    private void AutoCommit() => FlushOpen();

    private void FlushOpen()
    {
        if (_open == null)
            return;

        var sealedStep = _openThen.Count == 0 ? _open : _open with { Then = _openThen.ToArray() };
        _steps.Add(sealedStep);
        _open = null;
        _openThen.Clear();
    }

    private void MutateOpen(Func<HttpRequestPlanStep, HttpRequestPlanStep> mutate)
    {
        if (_open == null)
            throw new InvalidOperationException("With*/Without* modifiers apply to the open request. Call Get/Post first.");

        _open = mutate(_open);
    }

    private static Dictionary<string, string> Copy(Dictionary<string, string>? existing, string name, string value)
    {
        var copy = existing != null ? new Dictionary<string, string>(existing, StringComparer.OrdinalIgnoreCase) : new(StringComparer.OrdinalIgnoreCase);
        copy[name] = value;
        return copy;
    }
}
