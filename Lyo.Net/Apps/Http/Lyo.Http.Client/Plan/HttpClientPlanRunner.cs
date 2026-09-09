using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Lyo.Common.Json;
using Lyo.Exceptions;
using Lyo.Http.Client.Extract;
using Lyo.Http.Client.Pipeline;
using Lyo.Http.Client.Session;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Http.Client.Plan;

/// <summary>Linear plan runner (stop / continue / retry). No branch graph.</summary>
public sealed class HttpClientPlanRunner : IHttpClientPlanRunner
{
    /// <inheritdoc />
    public async Task<HttpClientPlanRunResult> RunAsync(
        ILyoHttpClient client,
        HttpClientPlan plan,
        HttpClientPlanRuntime? runtime = null,
        ILogger? logger = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(client);
        ArgumentHelpers.ThrowIfNull(plan);
        runtime ??= new();
        logger ??= NullLogger.Instance;
        var lastBody = "";
        int? lastStatus = null;
        HttpResponseMessage? lastResponse = null;
        var sessionHeaders = new LyoHttpHeaderBag();

        if (!string.IsNullOrWhiteSpace(runtime.CookieFile) && File.Exists(runtime.CookieFile))
            LoadCookieFile(client, runtime.CookieFile!);

        try {
            foreach (var step in plan.Steps) {
                ct.ThrowIfCancellationRequested();
                var result = await ExecuteStepAsync(client, step, runtime, sessionHeaders, lastBody, lastResponse, logger, ct).ConfigureAwait(false);
                lastBody = result.Body ?? lastBody;
                lastStatus = result.Status ?? lastStatus;
                lastResponse = result.Response ?? lastResponse;
                if (!result.Ok)
                    return new() {
                        Success = false, Runtime = runtime, LastBody = lastBody, LastStatusCode = lastStatus, Error = result.Error, Exception = result.Exception
                    };
            }

            return new() { Success = true, Runtime = runtime, LastBody = lastBody, LastStatusCode = lastStatus };
        }
        finally {
            lastResponse?.Dispose();
        }
    }

    private async Task<StepExec> ExecuteStepAsync(
        ILyoHttpClient client,
        HttpClientPlanStep step,
        HttpClientPlanRuntime runtime,
        LyoHttpHeaderBag sessionHeaders,
        string lastBody,
        HttpResponseMessage? lastResponse,
        ILogger logger,
        CancellationToken ct)
    {
        switch (step) {
            case HttpRequestPlanStep request:
                return await ExecuteRequestAsync(client, request, runtime, sessionHeaders, logger, ct).ConfigureAwait(false);
            case HttpDelayPlanStep delay:
                var wait = delay.Max is { } max && max > delay.Min
                    ? TimeSpan.FromMilliseconds(delay.Min.TotalMilliseconds + new Random().NextDouble() * (max - delay.Min).TotalMilliseconds)
                    : delay.Min;
                await Task.Delay(wait, ct).ConfigureAwait(false);
                return StepExec.Succeeded();
            case HttpDownloadFilePlanStep download:
                var dest = ResolvePath(HttpClientPlanInterpolation.Interpolate(download.Destination, runtime.Bindings), runtime, client);
                Directory.CreateDirectory(Path.GetDirectoryName(dest) ?? ".");
                var saved = await client.DownloadToFileAsync(Interpolate(download.Uri, runtime), dest, ct: ct).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(download.SavePathAs))
                    runtime.Bindings[download.SavePathAs!] = saved;

                return StepExec.Succeeded();
            case HttpDownloadUrlsPlanStep urls:
                await DownloadUrlsAsync(client, urls, runtime, ct).ConfigureAwait(false);
                return StepExec.Succeeded();
            case HttpSetHeaderPlanStep setHeader:
                sessionHeaders.Set(setHeader.HeaderName, Interpolate(setHeader.Value, runtime));
                return StepExec.Succeeded();
            case HttpRemoveHeaderPlanStep removeHeader:
                sessionHeaders.Remove(removeHeader.HeaderName);
                return StepExec.Succeeded();
            case HttpSetCookiePlanStep setCookie:
                client.Session.CookieJar.Add(setCookie.Cookie);
                return StepExec.Succeeded();
            case HttpRemoveCookiePlanStep removeCookie:
                client.Session.CookieJar.Remove(removeCookie.CookieName, removeCookie.Domain);
                return StepExec.Succeeded();
            case HttpClearCookiesPlanStep:
                client.Session.CookieJar.Clear();
                return StepExec.Succeeded();
            case HttpExportCookiesPlanStep export:
                runtime.JsonBindings[export.VariableName] = JsonSerializer.Serialize(client.Session.CookieJar.Export(), runtime.SerializerOptions ?? LyoJsonSerializerOptions.Create());
                return StepExec.Succeeded();
            case HttpLoadCookiesPlanStep load:
                if (!string.IsNullOrWhiteSpace(load.CookieFile))
                    LoadCookieFile(client, Interpolate(load.CookieFile!, runtime));
                else if (!string.IsNullOrWhiteSpace(load.VariableName) && runtime.JsonBindings.TryGetValue(load.VariableName!, out var json)) {
                    var cookies = JsonSerializer.Deserialize<List<LyoHttpCookie>>(json, runtime.SerializerOptions ?? LyoJsonSerializerOptions.Create());
                    if (cookies != null)
                        client.Session.CookieJar.Import(cookies);
                }

                return StepExec.Succeeded();
            case HttpStoreLiteralPlanStep literal:
                runtime.Bindings[literal.VariableName] = literal.Value;
                return StepExec.Succeeded();
            case HttpStoreTemplatePlanStep templ:
                runtime.Bindings[templ.VariableName] = Interpolate(templ.Template, runtime);
                return StepExec.Succeeded();
            case HttpFilterListPlanStep filter:
                FilterList(filter, runtime);
                return StepExec.Succeeded();
            case HttpMapListPlanStep map:
                MapList(map, runtime);
                return StepExec.Succeeded();
            default:
                return await ExecuteExtractAsync(step, runtime, lastBody, lastResponse, client.GetSerializerOptions()).ConfigureAwait(false);
        }
    }

    private async Task<StepExec> ExecuteRequestAsync(
        ILyoHttpClient client,
        HttpRequestPlanStep step,
        HttpClientPlanRuntime runtime,
        LyoHttpHeaderBag sessionHeaders,
        ILogger logger,
        CancellationToken ct)
    {
        var attempts = 0;
        var retries = Math.Max(0, step.RetryCount);
        while (true) {
            attempts++;
            try {
                var uri = Interpolate(step.Uri, runtime);
                if (!string.IsNullOrWhiteSpace(step.Query))
                    uri = UriHelpers.AppendQueryString(uri, Interpolate(step.Query!, runtime));

                using var request = new HttpRequestMessage(new(step.Method), uri);
                if (!step.ClearHeaders)
                    sessionHeaders.ApplyTo(request, step.HeadersToRemove);

                if (step.Headers != null) {
                    foreach (var pair in step.Headers)
                        request.Headers.TryAddWithoutValidation(pair.Key, Interpolate(pair.Value, runtime));
                }

                if (step.Cookies != null) {
                    foreach (var cookie in step.Cookies)
                        client.Session.CookieJar.Add(cookie, request.RequestUri);
                }

                if (step.CookiesToRemove != null) {
                    foreach (var name in step.CookiesToRemove)
                        client.Session.CookieJar.Remove(name);
                }

                AssignBody(request, step, runtime);

                HttpResponseMessage response;
                if (client is LyoHttpClient typed)
                    response = await typed.SendPreparedAsync(request, ct).ConfigureAwait(false);
                else {
                    response = await client.GetClient().SendAsync(request, ct).ConfigureAwait(false);
                    request.Dispose();
                }

                var status = (int)response.StatusCode;
                var allowed = response.IsSuccessStatusCode || (step.AllowStatuses != null && step.AllowStatuses.Contains(status));
                var body = await LyoHttpClient.ReadDecodedResponseStringAsync(response, ct).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(step.SaveBodyAs))
                    runtime.Bindings[step.SaveBodyAs!] = body;

                if (!allowed) {
                    if (step.OnRetryAfter && status == 429) {
                        var wait = LyoHttpRateLimitHandler.ParseRetryAfter(response) ?? TimeSpan.FromSeconds(1);
                        response.Dispose();
                        await Task.Delay(wait, ct).ConfigureAwait(false);
                        if (attempts <= retries + 1)
                            continue;
                    }

                    if (attempts <= retries) {
                        response.Dispose();
                        if (step.RetryBackoff is { } backoff)
                            await Task.Delay(backoff, ct).ConfigureAwait(false);

                        continue;
                    }

                    if (step.OnFailure == HttpStepFailure.Continue) {
                        logger.LogWarning("Plan request {Uri} failed with {Status}; continuing", uri, status);
                        if (step.Then != null)
                            await RunThenAsync(client, step.Then, runtime, sessionHeaders, body, response, logger, ct).ConfigureAwait(false);

                        return new() { Ok = true, Body = body, Status = status, Response = response };
                    }

                    response.Dispose();
                    return new() { Ok = false, Body = body, Status = status, Error = $"HTTP {status} for {uri}" };
                }

                if (step.Then != null)
                    await RunThenAsync(client, step.Then, runtime, sessionHeaders, body, response, logger, ct).ConfigureAwait(false);

                return new() { Ok = true, Body = body, Status = status, Response = response };
            }
            catch (Exception) when (attempts <= retries) {
                if (step.RetryBackoff is { } backoff)
                    await Task.Delay(backoff, ct).ConfigureAwait(false);
            }
            catch (Exception ex) {
                if (step.OnFailure == HttpStepFailure.Continue) {
                    logger.LogWarning(ex, "Plan request failed; continuing");
                    return StepExec.Succeeded();
                }

                logger.LogError(ex, "Plan request failed");
                return new() { Ok = false, Error = FormatException(ex), Exception = ex };
            }
        }
    }

    private async Task RunThenAsync(
        ILyoHttpClient client,
        IEnumerable<HttpClientPlanStep> then,
        HttpClientPlanRuntime runtime,
        LyoHttpHeaderBag sessionHeaders,
        string body,
        HttpResponseMessage? response,
        ILogger logger,
        CancellationToken ct)
    {
        foreach (var child in then)
            await ExecuteStepAsync(client, child, runtime, sessionHeaders, body, response, logger, ct).ConfigureAwait(false);
    }

    private static void AssignBody(HttpRequestMessage request, HttpRequestPlanStep step, HttpClientPlanRuntime runtime)
    {
        if (!string.IsNullOrEmpty(step.JsonBody))
            request.Content = new StringContent(Interpolate(step.JsonBody!, runtime), Encoding.UTF8, "application/json");
        else if (step.Form is { Count: > 0 }) {
            var pairs = step.Form.Select(p => new KeyValuePair<string, string>(p.Key, Interpolate(p.Value, runtime)));
            request.Content = new FormUrlEncodedContent(pairs);
        }
        else if (!string.IsNullOrEmpty(step.RawBody))
            request.Content = new StringContent(Interpolate(step.RawBody!, runtime), Encoding.UTF8, "text/plain");
    }

    private async Task DownloadUrlsAsync(ILyoHttpClient client, HttpDownloadUrlsPlanStep step, HttpClientPlanRuntime runtime, CancellationToken ct)
    {
        if (!runtime.ListBindings.TryGetValue(step.VariableName, out var urls) || urls.Count == 0)
            return;

        var dir = ResolvePath(Interpolate(step.Directory, runtime), runtime, client);
        Directory.CreateDirectory(dir);
        var prefix = step.Prefix ?? "file";
        var index = 0;
        foreach (var url in urls) {
            index++;
            var leaf = Path.GetFileName(new Uri(url, UriKind.RelativeOrAbsolute).IsAbsoluteUri ? new Uri(url).AbsolutePath : url);
            if (string.IsNullOrWhiteSpace(leaf))
                leaf = $"{prefix}-{index}";

            var dest = Path.Combine(dir, leaf);
            await client.DownloadToFileAsync(url, dest, ct: ct).ConfigureAwait(false);
        }
    }

    private static Task<StepExec> ExecuteExtractAsync(
        HttpClientPlanStep step,
        HttpClientPlanRuntime runtime,
        string lastBody,
        HttpResponseMessage? lastResponse,
        JsonSerializerOptions serializerOptions)
    {
        var source = lastBody;
        LyoHttpExtractOptions? options = null;
        string? variable = null;
        IReadOnlyList<LyoHttpExtractedItem>? items = null;
        Uri? baseUri = lastResponse?.RequestMessage?.RequestUri;

        switch (step) {
            case HttpExtractSourcesPlanStep s:
                options = s.Options;
                source = ResolveSource(runtime, options.FromVariable, lastBody);
                items = LyoHttpDocumentExtractor.ExtractSources(source, options, baseUri);
                variable = s.VariableName;
                break;
            case HttpExtractImagesPlanStep s:
                options = s.Options ?? new();
                source = ResolveSource(runtime, options.FromVariable, lastBody);
                items = LyoHttpDocumentExtractor.ExtractImages(source, options, baseUri);
                variable = s.VariableName;
                break;
            case HttpExtractLinksPlanStep s:
                options = s.Options ?? new();
                source = ResolveSource(runtime, options.FromVariable, lastBody);
                items = LyoHttpDocumentExtractor.ExtractLinks(source, options, baseUri);
                variable = s.VariableName;
                break;
            case HttpExtractAttributePlanStep s:
                options = s.Options;
                source = ResolveSource(runtime, options.FromVariable, lastBody);
                items = LyoHttpDocumentExtractor.ExtractAttributes(source, options.Selector ?? "*", options, baseUri);
                variable = s.VariableName;
                break;
            case HttpExtractTextPlanStep s:
                options = s.Options;
                source = ResolveSource(runtime, options.FromVariable, lastBody);
                items = LyoHttpDocumentExtractor.ExtractText(source, options, baseUri);
                variable = s.VariableName;
                break;
            case HttpExtractHtmlPlanStep s:
                options = s.Options;
                source = ResolveSource(runtime, options.FromVariable, lastBody);
                items = LyoHttpDocumentExtractor.ExtractHtml(source, options, baseUri);
                variable = s.VariableName;
                break;
            case HttpExtractRegexPlanStep s:
                source = ResolveSource(runtime, s.FromVariable, lastBody);
                items = LyoHttpDocumentExtractor.ExtractRegex(source, new() { Pattern = s.Pattern, Group = s.Group });
                variable = s.VariableName;
                break;
            case HttpExtractMetaPlanStep s:
                options = s.Options ?? new();
                source = ResolveSource(runtime, options.FromVariable, lastBody);
                items = LyoHttpDocumentExtractor.ExtractMeta(source, options, baseUri);
                foreach (var item in items) {
                    var key = item.Text ?? "";
                    var prefix = s.VariablePrefix;
                    var name = string.IsNullOrEmpty(prefix) ? key : prefix + ":" + key;
                    var value = item.Url ?? item.Raw ?? "";
                    if (string.IsNullOrEmpty(value))
                        continue;

                    if (!runtime.ListBindings.TryGetValue(name, out var list))
                        runtime.ListBindings[name] = list = [];

                    list.Add(value);
                    runtime.Bindings[name] = value;
                }

                StoreItems(runtime, s.VariablePrefix + "Items", items, serializerOptions);
                return Task.FromResult(StepExec.Succeeded());
            case HttpExtractJsonLdPlanStep s:
                options = s.Options ?? new();
                source = ResolveSource(runtime, options.FromVariable, lastBody);
                items = LyoHttpDocumentExtractor.ExtractJsonLd(source, options);
                variable = s.VariableName;
                break;
            case HttpExtractTablePlanStep s:
                options = s.Options ?? new() { Selector = s.Selector };
                options.Selector = s.Selector;
                source = ResolveSource(runtime, options.FromVariable, lastBody);
                items = LyoHttpDocumentExtractor.ExtractTable(source, options, baseUri);
                variable = s.VariableName;
                break;
            case HttpExtractHeaderPlanStep s:
                var header = "";
                if (lastResponse?.Headers.TryGetValues(s.HeaderName, out var values) == true)
                    header = string.Join(",", values);
                else if (lastResponse?.Content.Headers.TryGetValues(s.HeaderName, out var contentValues) == true)
                    header = string.Join(",", contentValues);

                runtime.Bindings[s.VariableName] = header;
                return Task.FromResult(StepExec.Succeeded());
            case HttpExtractJsonPlanStep s:
                source = ResolveSource(runtime, s.FromVariable, lastBody);
                runtime.JsonBindings[s.VariableName] = source;
                runtime.Bindings[s.VariableName] = source;
                return Task.FromResult(StepExec.Succeeded());
            case HttpExtractJsonPathPlanStep s:
                source = ResolveSource(runtime, s.FromVariable, lastBody);
                runtime.Bindings[s.VariableName] = ReadJsonPath(source, s.JsonPath) ?? "";
                return Task.FromResult(StepExec.Succeeded());
            case HttpExtractJsonArrayPlanStep s:
                source = ResolveSource(runtime, s.FromVariable, lastBody);
                runtime.ListBindings[s.VariableName] = ReadJsonArray(source, s.JsonPath);
                return Task.FromResult(StepExec.Succeeded());
            case HttpExtractJsonRecordsPlanStep s:
                source = ResolveSource(runtime, s.FromVariable, lastBody);
                runtime.JsonBindings[s.VariableName] = ReadJsonPath(source, s.JsonPath ?? "$") ?? source;
                return Task.FromResult(StepExec.Succeeded());
            default:
                return Task.FromResult(new StepExec { Ok = false, Error = $"Unsupported step {step.GetType().Name}" });
        }

        if (variable != null && items != null)
            StoreExtract(runtime, variable, items, options, serializerOptions);

        return Task.FromResult(StepExec.Succeeded());
    }

    private static void StoreExtract(
        HttpClientPlanRuntime runtime,
        string variable,
        IReadOnlyList<LyoHttpExtractedItem> items,
        LyoHttpExtractOptions? options,
        JsonSerializerOptions serializerOptions)
    {
        var urls = LyoHttpDocumentExtractor.ToUrlList(items);
        runtime.ListBindings[variable] = urls.ToList();
        if (urls.Count > 0)
            runtime.Bindings[variable] = urls[0];

        var itemsName = options?.SaveItemsAs ?? variable + "Items";
        StoreItems(runtime, itemsName, items, serializerOptions);
    }

    private static void StoreItems(HttpClientPlanRuntime runtime, string name, IReadOnlyList<LyoHttpExtractedItem> items, JsonSerializerOptions serializerOptions)
        => runtime.JsonBindings[name] = JsonSerializer.Serialize(items, serializerOptions);

    private static string ResolveSource(HttpClientPlanRuntime runtime, string? fromVariable, string lastBody)
    {
        if (string.IsNullOrWhiteSpace(fromVariable))
            return lastBody;

        if (runtime.Bindings.TryGetValue(fromVariable!, out var s))
            return s;

        return runtime.JsonBindings.TryGetValue(fromVariable!, out var json) ? json : lastBody;
    }

    private static void FilterList(HttpFilterListPlanStep step, HttpClientPlanRuntime runtime)
    {
        if (!runtime.ListBindings.TryGetValue(step.SourceVariable, out var source))
            return;

        IEnumerable<string> q = source;
        if (!string.IsNullOrWhiteSpace(step.IncludeRegex))
            q = q.Where(s => Regex.IsMatch(s, step.IncludeRegex!));
        if (!string.IsNullOrWhiteSpace(step.ExcludeRegex))
            q = q.Where(s => !Regex.IsMatch(s, step.ExcludeRegex!));
        if (step.Skip > 0)
            q = q.Skip(step.Skip);
        if (step.Take > 0)
            q = q.Take(step.Take);
        if (step.Distinct)
            q = q.Distinct(StringComparer.Ordinal);

        runtime.ListBindings[step.TargetVariable] = q.ToList();
    }

    private static void MapList(HttpMapListPlanStep step, HttpClientPlanRuntime runtime)
    {
        if (!runtime.ListBindings.TryGetValue(step.SourceVariable, out var source))
            return;

        runtime.ListBindings[step.TargetVariable] = source.Select(s => (step.Prefix ?? "") + s + (step.Suffix ?? "")).ToList();
    }

    private static void LoadCookieFile(ILyoHttpClient client, string path)
    {
        var json = File.ReadAllText(path);
        var cookies = JsonSerializer.Deserialize<List<LyoHttpCookie>>(json);
        if (cookies != null)
            client.Session.CookieJar.Import(cookies);
    }

    private static string Interpolate(string template, HttpClientPlanRuntime runtime)
        => HttpClientPlanInterpolation.Interpolate(template, runtime.Bindings);

    private static string ResolvePath(string path, HttpClientPlanRuntime runtime, ILyoHttpClient client)
    {
        if (Path.IsPathRooted(path))
            return path;

        var root = runtime.DownloadDirectory ?? (client as LyoHttpClient)?.ResolveDownloadDirectory() ?? Directory.GetCurrentDirectory();
        return Path.Combine(root, path);
    }

    internal static string? ReadJsonPath(string json, string path)
    {
        try {
            var node = JsonNode.Parse(json);
            var current = Walk(node, path);
            return current?.ToJsonString() is { } s ? TrimJsonString(s) : current?.ToString();
        }
        catch {
            return null;
        }
    }

    internal static List<string> ReadJsonArray(string json, string? path)
    {
        try {
            var node = Walk(JsonNode.Parse(json), path ?? "$");
            if (node is JsonArray array)
                return array.Select(n => TrimJsonString(n?.ToJsonString() ?? "")).Where(s => s.Length > 0).ToList();
        }
        catch {
            // ignore
        }

        return [];
    }

    private static JsonNode? Walk(JsonNode? node, string path)
    {
        if (node == null)
            return null;

        var trimmed = path.StartsWith("$", StringComparison.Ordinal) ? path[1..] : path;
        if (trimmed.StartsWith(".", StringComparison.Ordinal))
            trimmed = trimmed[1..];

        if (trimmed.Length == 0)
            return node;

        var parts = trimmed.Split(['.'], StringSplitOptions.RemoveEmptyEntries);
        var current = node;
        foreach (var raw in parts) {
            var name = raw;
            var index = (int?)null;
            var star = false;
            var bracket = raw.IndexOf('[');
            if (bracket >= 0) {
                name = raw[..bracket];
                var inside = raw[(bracket + 1)..].TrimEnd(']');
                if (inside == "*")
                    star = true;
                else if (int.TryParse(inside, out var i))
                    index = i;
            }

            if (name.Length > 0)
                current = current is JsonObject obj ? obj[name] : null;

            if (current == null)
                return null;

            if (star && current is JsonArray arr)
                return arr;

            if (index is { } idx && current is JsonArray array)
                current = idx < array.Count ? array[idx] : null;
        }

        return current;
    }

    private static string FormatException(Exception ex)
    {
        var text = ex.Message;
        for (var inner = ex.InnerException; inner != null; inner = inner.InnerException) {
            if (string.IsNullOrWhiteSpace(inner.Message) || text.IndexOf(inner.Message, StringComparison.Ordinal) >= 0)
                continue;

            text += " ---> " + inner.Message;
        }

        return text;
    }

    private static string TrimJsonString(string json)
        => json.Length >= 2 && json[0] == '"' && json[^1] == '"' ? JsonSerializer.Deserialize<string>(json) ?? json : json;

    private sealed class StepExec
    {
        public bool Ok { get; init; } = true;

        public string? Body { get; init; }

        public int? Status { get; init; }

        public string? Error { get; init; }

        public Exception? Exception { get; init; }

        public HttpResponseMessage? Response { get; init; }

        public static StepExec Succeeded() => new();
    }
}
