using System.Collections;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lyo.Common.Core.Net;
using Lyo.Common.Json;
using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;
using Lyo.Http.Client.Plan;
using Lyo.Http.Client.Session;
using Lyo.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Http.Client;

/// <summary>
/// Generic HTTP client. JSON verbs, query-string GET DTOs, files, compression, session cookies, and plan entry points.
/// Non-success statuses throw <see cref="LyoHttpException" />. Lyo API problem-details live on <c>ApiClient</c>.
/// </summary>
public class LyoHttpClient : ILyoHttpClient
{
    private const string GetVerb = "GET";
    private const string PostVerb = "POST";
    private const string PutVerb = "PUT";
    private const string PatchVerb = "PATCH";
    private const string DeleteVerb = "DELETE";

    private readonly bool _ownsHttpClient;
    private readonly ConcurrentDictionary<string, JsonConverter> _savedConverters = new();
    private readonly int _clientUaSeed = Environment.TickCount;

    /// <summary>Transport and JSON options for this instance.</summary>
    protected readonly LyoHttpClientOptions BaseOptions;

    /// <summary>Underlying HTTP client. Do not dispose when this instance came from <c>IHttpClientFactory</c>.</summary>
    protected readonly HttpClient HttpClient;

    /// <summary>Logger used for request scopes.</summary>
    protected readonly ILogger Logger;

    /// <summary>JSON options used for request and response bodies.</summary>
    public readonly JsonSerializerOptions SerializerOptions;

    /// <summary>
    /// Factory constructor. <paramref name="httpClient" /> is injected by <c>IHttpClientFactory</c>. This is the only public constructor so typed-client activation
    /// is unambiguous.
    /// </summary>
    public LyoHttpClient(
        HttpClient httpClient,
        LyoHttpClientOptions? options = null,
        ILogger? logger = null,
        JsonSerializerOptions? serializerOptions = null,
        IMetrics? metrics = null,
        ILyoHttpSession? session = null,
        LyoHttpClientHooks? hooks = null)
        : this(logger, httpClient, serializerOptions, options, metrics, session, hooks) { }

    /// <summary>Compatibility constructor matching the historical <c>ApiClient</c> parameter order so vendor subclasses keep compiling.</summary>
    protected LyoHttpClient(
        ILogger? logger = null,
        HttpClient? httpClient = null,
        JsonSerializerOptions? serializerOptions = null,
        LyoHttpClientOptions? options = null,
        IMetrics? metrics = null,
        ILyoHttpSession? session = null,
        LyoHttpClientHooks? hooks = null)
    {
        Logger = logger ?? NullLogger<LyoHttpClient>.Instance;
        BaseOptions = options ?? new();
        BaseOptions.Validate();
        Metrics = metrics ?? NullMetrics.Instance;
        Session = session ?? new LyoHttpSession();
        Hooks = hooks ?? new();
        _ownsHttpClient = httpClient == null;
        HttpClient = httpClient ?? new HttpClient(new LyoHttpClientHandler(BaseOptions));
        SerializerOptions = serializerOptions ?? LyoJsonSerializerOptions.Create();
        if (HttpClient.BaseAddress == null && !string.IsNullOrWhiteSpace(BaseOptions.BaseUrl))
            HttpClient.BaseAddress = new(BaseOptions.BaseUrl!.TrimEnd('/') + "/");

        ApplyDefaultHeaders();
        ConfigureAcceptEncodingHeaders();
        EnsureSessionUserAgent();
    }

    /// <summary>Metrics sink (null-object when unused).</summary>
    protected IMetrics Metrics { get; }

    /// <inheritdoc />
    public ILyoHttpSession Session { get; }

    /// <inheritdoc />
    public LyoHttpClientHooks Hooks { get; }

    /// <inheritdoc />
    public JsonSerializerOptions GetSerializerOptions() => SerializerOptions;

    /// <inheritdoc />
    public HttpClient GetClient() => HttpClient;

    /// <summary>Stamps default headers (User-Agent). Api.Client overrides this to use <c>Lyo/{version}</c> without rotation.</summary>
    protected virtual void ApplyDefaultHeaders()
    {
        if (!BaseOptions.UserAgent.Enabled)
            return;

        var agent = Session.UserAgent ?? ResolveUserAgent();
        Session.UserAgent = agent;
        SetUserAgent(agent);
    }

    /// <summary>Builds an <see cref="HttpRequestMessage" />. Vendors override to add auth headers.</summary>
    protected virtual HttpRequestMessage CreateRequest(HttpMethod method, string uri)
        => new(method, uri);

    /// <summary>Throws <see cref="LyoHttpException" /> (or a mapped type) when the status is not success and <see cref="LyoHttpClientOptions.EnsureStatusCode" /> is true.</summary>
    protected virtual async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode || !BaseOptions.EnsureStatusCode)
            return;

        var body = await TryReadBodyPreviewAsync(response, ct).ConfigureAwait(false);
        var message = $"Request failed: {(int)response.StatusCode} {response.ReasonPhrase}";
        throw MapException(new LyoHttpException((int)response.StatusCode, message, body));
    }

    /// <summary>Lets subclasses wrap or replace the exception thrown for a failed call.</summary>
    protected virtual Exception MapException(Exception exception) => exception;

    /// <summary>Deserializes a JSON response. Override to change converters or null handling.</summary>
    protected virtual async Task<TResult?> DeserializeAsync<TResult>(HttpResponseMessage response, CancellationToken ct)
        => await DeserializeResponseNullableAsync<TResult>(response, ct).ConfigureAwait(false);

    /// <summary>Serializes a JSON request body, optionally compressing it.</summary>
    protected virtual HttpContent SerializeBody<TRequest>(TRequest body) => CreateJsonContent(body);

    /// <inheritdoc />
    public HttpClientPlanBuilder Get(string uri) => HttpClientPlanBuilder.New(null, this).Get(uri);

    /// <inheritdoc />
    public HttpClientPlanBuilder Post(string uri) => HttpClientPlanBuilder.New(null, this).Post(uri);

    /// <inheritdoc />
    public Task<HttpClientPlanRunResult> RunAsync(HttpClientPlan plan, HttpClientPlanRuntime? runtime = null, CancellationToken ct = default)
        => new HttpClientPlanRunner().RunAsync(this, plan, runtime, Logger, ct);

    /// <inheritdoc />
    public async Task<TResult?> GetAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        EnsureUri(uri);
        using (Logger.BeginScope("{RequestMethodName} {Uri} {ResultTypeName}", GetVerb, uri, typeof(TResult).FullName)) {
            using var request = CreateRequest(HttpMethod.Get, uri);
            using var response = await SendAsync(request, before, ct).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NoContent)
                return default;

            return await DeserializeAsync<TResult>(response, ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<TResult?> GetAsAsync<TRequest, TResult>(
        string uri,
        TRequest? query,
        string? enumerableDelimiter = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
    {
        EnsureUri(uri);
        using (Logger.BeginScope("{RequestMethodName} {Uri} {QueryTypeName} {ResultTypeName}", GetVerb, uri, typeof(TRequest).FullName, typeof(TResult).FullName)) {
            var queryParams = ToQueryString(query, enumerableDelimiter);
            uri = string.IsNullOrEmpty(queryParams) ? uri : UriHelpers.AppendQueryString(uri, queryParams);
            OperationHelpers.ThrowIf(uri.Length > 4096, "Uri length too long");
            using var request = CreateRequest(HttpMethod.Get, uri);
            using var response = await SendAsync(request, before, ct).ConfigureAwait(false);
            return await DeserializeAsync<TResult>(response, ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<byte[]> GetFileAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        EnsureUri(uri);
        using (Logger.BeginScope("{RequestMethodName} {Uri}", HttpMethod.Get, uri)) {
            using var request = CreateRequest(HttpMethod.Get, uri);
            LyoHttpRequestMarkers.SetStreamBinary(request);
            using var response = await SendAsync(request, before, ct).ConfigureAwait(false);
            return await ReadResponseBytesAsync(response, ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<(Stream Content, string? FileName, long? ContentLength)> GetFileStreamAsync(
        string uri,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
    {
        EnsureUri(uri);
        Logger.LogDebug("Sending streaming GET request for file: {Uri}", uri);
        var previous = LyoHttpSessionAccessor.Session;
        LyoHttpSessionAccessor.Session = Session;
        var request = CreateRequest(HttpMethod.Get, uri);
        LyoHttpRequestMarkers.SetStreamBinary(request);
        ApplySessionToRequest(request);
        before?.Invoke(request);
        await InvokeBeforeAsync(request, ct).ConfigureAwait(false);
        HttpResponseMessage? response = null;
        try {
            response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            MergeSetCookie(response, request.RequestUri);
            await EnsureSuccessAsync(response, ct).ConfigureAwait(false);
#if NET5_0_OR_GREATER
            var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
#else
            var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
            var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"');
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = TryFileNameFromUri(request.RequestUri) ?? ContentDisposition.FallbackFileName;

            return (new HttpResponseStream(stream, response, request), fileName, response.Content.Headers.ContentLength);
        }
        catch {
            response?.Dispose();
            request.Dispose();
            throw;
        }
        finally {
            LyoHttpSessionAccessor.Session = previous;
        }
    }

    /// <inheritdoc />
    public async Task<(byte[] Content, FileTypeInfo FileType)> GetFileWithTypeAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        EnsureUri(uri);
        using (Logger.BeginScope("{RequestMethodName} {Uri}", HttpMethod.Get, uri)) {
            using var request = CreateRequest(HttpMethod.Get, uri);
            LyoHttpRequestMarkers.SetStreamBinary(request);
            using var response = await SendAsync(request, before, ct).ConfigureAwait(false);
            var fileType = FileTypeInfo.FromMimeType(response.Content.Headers.ContentType?.MediaType);
            var content = await ReadResponseBytesAsync(response, ct).ConfigureAwait(false);
            return (content, fileType);
        }
    }

    /// <inheritdoc />
    public async Task<string> DownloadToFileAsync(
        string uri,
        string destinationPath,
        IProgress<LyoHttpDownloadProgress>? progress = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(destinationPath);
        var (stream, _, length) = await GetFileStreamAsync(uri, before, ct).ConfigureAwait(false);
        try {
            var dir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            using var output = File.Create(destinationPath);
            var buffer = new byte[81920];
            long received = 0;
            while (true) {
#if NET5_0_OR_GREATER
                var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct).ConfigureAwait(false);
#else
                var read = await stream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false);
#endif
                if (read == 0)
                    break;

#if NET5_0_OR_GREATER
                await output.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
#else
                await output.WriteAsync(buffer, 0, read, ct).ConfigureAwait(false);
#endif
                received += read;
                progress?.Report(new(received, length));
            }
        }
        finally {
            stream.Dispose();
        }

        return destinationPath;
    }

    /// <inheritdoc />
    public async Task<TResult> PatchAsAsync<TRequest, TResult>(string uri, TRequest? body, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        EnsureUri(uri);
        using (Logger.BeginScope("{RequestMethodName} {Uri} {BodyTypeName} {ResultTypeName}", PatchVerb, uri, typeof(TRequest).FullName, typeof(TResult).FullName)) {
            using var content = SerializeBody(body);
#if NETSTANDARD2_0
            using var request = CreateRequest(new(PatchVerb), uri);
#else
            using var request = CreateRequest(HttpMethod.Patch, uri);
#endif
            request.Content = content;
            using var response = await SendAsync(request, before, ct).ConfigureAwait(false);
            return await DeserializeRequiredAsync<TResult>(response, ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<TResult> PutAsAsync<TRequest, TResult>(string uri, TRequest? body, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        EnsureUri(uri);
        using (Logger.BeginScope("{RequestMethodName} {Uri} {BodyTypeName} {ResultTypeName}", PutVerb, uri, typeof(TRequest).FullName, typeof(TResult).FullName)) {
            using var content = SerializeBody(body);
            using var request = CreateRequest(HttpMethod.Put, uri);
            request.Content = content;
            using var response = await SendAsync(request, before, ct).ConfigureAwait(false);
            return await DeserializeRequiredAsync<TResult>(response, ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<TResult> PostAsAsync<TRequest, TResult>(string uri, TRequest? body, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        EnsureUri(uri);
        using (Logger.BeginScope("{RequestMethodName} {Uri} {BodyTypeName} {ResultTypeName}", PostVerb, uri, typeof(TRequest).FullName, typeof(TResult).FullName)) {
            using var content = SerializeBody(body);
            using var request = CreateRequest(HttpMethod.Post, uri);
            request.Content = content;
            using var response = await SendAsync(request, before, ct).ConfigureAwait(false);
            return await DeserializeRequiredAsync<TResult>(response, ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<TResult> PostAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        EnsureUri(uri);
        using (Logger.BeginScope("{RequestMethodName} {Uri} {ResultTypeName}", PostVerb, uri, typeof(TResult).FullName)) {
            using var request = CreateRequest(HttpMethod.Post, uri);
            using var response = await SendAsync(request, before, ct).ConfigureAwait(false);
            return await DeserializeRequiredAsync<TResult>(response, ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<byte[]> PostAsBinaryAsync<TRequest>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        EnsureUri(uri);
        using (Logger.BeginScope("{RequestMethodName} {Uri} {RequestTypeName}", PostVerb, uri, typeof(TRequest).FullName)) {
            using var content = SerializeBody(request);
            using var httpRequest = CreateRequest(HttpMethod.Post, uri);
            httpRequest.Content = content;
            using var response = await SendAsync(httpRequest, before, ct).ConfigureAwait(false);
            return await ReadResponseBytesAsync(response, ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<TResult> PostFileAsAsync<TResult>(
        string uri,
        Stream stream,
        FileTypeInfo fileType,
        string? fileName = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
    {
        EnsureUri(uri);
        OperationHelpers.ThrowIfNotReadable(stream, $"Stream '{nameof(stream)}' must be readable.");
        var safeFileName = !string.IsNullOrWhiteSpace(fileName) ? FileHelpers.GetValidFileName(fileName) : $"file{fileType.DefaultExtension}";
        return await PostFileAsAsyncCore<TResult>(uri, stream, fileType, safeFileName, before, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TResult> PostFileAsAsync<TResult>(string uri, Stream stream, string fileName, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        EnsureUri(uri);
        OperationHelpers.ThrowIfNotReadable(stream, $"Stream '{nameof(stream)}' must be readable.");
        FileHelpers.ThrowIfFileNameInvalid(fileName);
        var fileType = FileTypeInfo.FromFilePath(fileName);
        return await PostFileAsAsyncCore<TResult>(uri, stream, fileType, fileName, before, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TResult> PostFileAsAsync<TResult>(
        string uri,
        byte[] data,
        FileTypeInfo fileType,
        string? fileName = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(data);
        using var stream = new MemoryStream(data);
        return await PostFileAsAsync<TResult>(uri, stream, fileType, fileName, before, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TResult> PostFileAsAsync<TResult>(string uri, byte[] data, string fileName, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(data);
        var fileType = FileTypeInfo.FromFilePath(fileName);
        using var stream = new MemoryStream(data);
        return await PostFileAsAsync<TResult>(uri, stream, fileType, fileName, before, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TResult> PostFileAsAsync<TResult>(string uri, string filePath, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        EnsureUri(uri);
        ArgumentHelpers.ThrowIfFileNotFound(filePath);
        var fileName = Path.GetFileName(filePath);
        var stream = File.OpenRead(filePath);
        try {
            return await PostFileAsAsync<TResult>(uri, stream, fileName, before, ct).ConfigureAwait(false);
        }
        finally {
#if NET9_0_OR_GREATER
            await stream.DisposeAsync().ConfigureAwait(false);
#else
            stream.Dispose();
#endif
        }
    }

    /// <inheritdoc />
    public async Task<TResult> DeleteAsAsync<TRequest, TResult>(string uri, TRequest? body = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        EnsureUri(uri);
        using (Logger.BeginScope("{RequestMethodName} {Uri} {BodyTypeName} {ResultTypeName}", DeleteVerb, uri, typeof(TRequest).FullName, typeof(TResult).FullName)) {
            using var request = CreateRequest(HttpMethod.Delete, uri);
            if (body != null)
                request.Content = SerializeBody(body);

            using var response = await SendAsync(request, before, ct).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NoContent)
                return default!;

            return await DeserializeRequiredAsync<TResult>(response, ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<TResult> DeleteAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        EnsureUri(uri);
        using (Logger.BeginScope("{RequestMethodName} {Uri} {ResultTypeName}", DeleteVerb, uri, typeof(TResult).FullName)) {
            using var request = CreateRequest(HttpMethod.Delete, uri);
            using var response = await SendAsync(request, before, ct).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NoContent)
                return default!;

            return await DeserializeRequiredAsync<TResult>(response, ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_ownsHttpClient)
            HttpClient.Dispose();
    }

    /// <summary>POSTs a JSON body when the API sends no response body (for example 204 No Content).</summary>
    protected async Task PostExpectingNoContentAsync<TRequest>(string uri, TRequest? body, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        EnsureUri(uri);
        using (Logger.BeginScope("{RequestMethodName} {Uri} {BodyTypeName}", PostVerb, uri, typeof(TRequest).FullName)) {
            using var content = SerializeBody(body);
            using var request = CreateRequest(HttpMethod.Post, uri);
            request.Content = content;
            using var response = await SendAsync(request, before, ct).ConfigureAwait(false);
        }
    }

    /// <summary>Sends <paramref name="request" /> through hooks, session cookies, and <see cref="EnsureSuccessAsync" />.</summary>
    protected async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, Action<HttpRequestMessage>? before, CancellationToken ct)
    {
        var previous = LyoHttpSessionAccessor.Session;
        LyoHttpSessionAccessor.Session = Session;
        try {
            ApplySessionToRequest(request);
            before?.Invoke(request);
            await InvokeBeforeAsync(request, ct).ConfigureAwait(false);
            Logger.LogDebug("Sending request");
            var response = await HttpClient.SendAsync(request, ct).ConfigureAwait(false);
            Logger.LogDebug("{ResponseStatusCode} Response received", response.StatusCode);
            MergeSetCookie(response, request.RequestUri);
            var context = new LyoHttpCallContext(request) { Response = response };
            if (Hooks.AfterResponseAsync != null)
                await Hooks.AfterResponseAsync(context, ct).ConfigureAwait(false);
            if (Hooks.OnResponseObserved != null)
                await Hooks.OnResponseObserved(context, ct).ConfigureAwait(false);

            try {
                await EnsureSuccessAsync(response, ct).ConfigureAwait(false);
            }
            catch (Exception ex) {
                context.Exception = ex;
                if (Hooks.OnFailureAsync != null)
                    await Hooks.OnFailureAsync(context, ct).ConfigureAwait(false);

                throw;
            }

            return response;
        }
        finally {
            LyoHttpSessionAccessor.Session = previous;
        }
    }

    /// <summary>Internal send used by the plan runner (same pipeline as JSON verbs).</summary>
    internal Task<HttpResponseMessage> SendPreparedAsync(HttpRequestMessage request, CancellationToken ct)
        => SendAsync(request, null, ct);

    internal string? ResolveDownloadDirectory() => BaseOptions.DownloadDirectory;

    /// <summary>Pins <see cref="ILyoHttpSession.UserAgent" /> for Flared after a solver round-trip.</summary>
    public void PinUserAgent(string userAgent)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(userAgent);
        Session.UserAgent = userAgent;
        SetUserAgent(userAgent);
    }

    private async Task InvokeBeforeAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var context = new LyoHttpCallContext(request);
        if (Hooks.BeforeSendAsync != null)
            await Hooks.BeforeSendAsync(context, ct).ConfigureAwait(false);
        if (Hooks.OnRequestObserved != null)
            await Hooks.OnRequestObserved(context, ct).ConfigureAwait(false);
    }

    private void ApplySessionToRequest(HttpRequestMessage request)
    {
        if (request.RequestUri != null) {
            var cookie = Session.CookieJar.ToCookieHeader(request.RequestUri);
            if (!string.IsNullOrEmpty(cookie) && !request.Headers.Contains(HttpHeaderInfo.Cookie))
                request.Headers.TryAddWithoutValidation(HttpHeaderInfo.Cookie, cookie);
        }

        var ua = Session.UserAgent;
        if (!string.IsNullOrWhiteSpace(ua) && !request.Headers.Contains(HttpHeaderInfo.UserAgent))
            request.Headers.TryAddWithoutValidation(HttpHeaderInfo.UserAgent, ua);
    }

    private void MergeSetCookie(HttpResponseMessage response, Uri? requestUri)
    {
        if (requestUri == null || !response.Headers.TryGetValues(HttpHeaderInfo.SetCookie, out var values))
            return;

        foreach (var raw in values) {
            var parsed = TryParseSetCookie(raw, requestUri);
            if (parsed != null)
                Session.CookieJar.Add(parsed, requestUri);
        }
    }

    private static LyoHttpCookie? TryParseSetCookie(string raw, Uri requestUri)
    {
        var parts = raw.Split(';');
        if (parts.Length == 0)
            return null;

        var nv = parts[0].Split(['='], 2);
        if (nv.Length == 0 || string.IsNullOrWhiteSpace(nv[0]))
            return null;

        var cookie = new LyoHttpCookie {
            Name = nv[0].Trim(),
            Value = nv.Length > 1 ? nv[1].Trim() : "",
            Domain = requestUri.Host,
            Path = "/"
        };
        bool? secure = null, httpOnly = null;
        string? domain = cookie.Domain, path = cookie.Path;
        DateTimeOffset? expiry = null;
        foreach (var attr in parts.Skip(1)) {
            var trimmed = attr.Trim();
            if (trimmed.Equals("Secure", StringComparison.OrdinalIgnoreCase))
                secure = true;
            else if (trimmed.Equals("HttpOnly", StringComparison.OrdinalIgnoreCase))
                httpOnly = true;
            else if (trimmed.StartsWith("Domain=", StringComparison.OrdinalIgnoreCase))
                domain = trimmed[7..].Trim();
            else if (trimmed.StartsWith("Path=", StringComparison.OrdinalIgnoreCase))
                path = trimmed[5..].Trim();
            else if (trimmed.StartsWith("Expires=", StringComparison.OrdinalIgnoreCase) && DateTimeOffset.TryParse(trimmed[8..], out var exp))
                expiry = exp;
        }

        return new() {
            Name = cookie.Name,
            Value = cookie.Value,
            Domain = domain,
            Path = path,
            Secure = secure,
            HttpOnly = httpOnly,
            Expiry = expiry
        };
    }

    private void EnsureUri(string uri)
    {
        if (HttpClient.BaseAddress != null) {
            UriHelpers.ThrowIfInvalidUri(uri);
            return;
        }

        UriHelpers.ThrowIfInvalidAbsoluteUri(uri);
    }

    private void EnsureSessionUserAgent()
    {
        if (!BaseOptions.UserAgent.Enabled)
            return;

        if (string.IsNullOrWhiteSpace(Session.UserAgent))
            Session.UserAgent = ResolveUserAgent();
    }

    private string ResolveUserAgent()
    {
        var ua = BaseOptions.UserAgent;
        return ua.Rotation switch {
            LyoHttpUserAgentRotation.PerRequest => ua.Resolve(Environment.TickCount ^ Guid.NewGuid().GetHashCode()),
            LyoHttpUserAgentRotation.PerSession => ua.Resolve(Session.SessionId.GetHashCode()),
            LyoHttpUserAgentRotation.PerClient => ua.Resolve(_clientUaSeed),
            var _ => ua.Resolve(0)
        };
    }

    private void SetUserAgent(string agent)
    {
        HttpClient.DefaultRequestHeaders.Remove(HttpHeaderInfo.UserAgent);
        HttpClient.DefaultRequestHeaders.TryAddWithoutValidation(HttpHeaderInfo.UserAgent, agent);
    }

    private void ConfigureAcceptEncodingHeaders()
        => ApplyAcceptEncodingHeaders(HttpClient, BaseOptions.AcceptEncodings);

    /// <summary>Copies supported values from <paramref name="encodings" /> onto <see cref="HttpClient.DefaultRequestHeaders" /> <c>Accept-Encoding</c>.</summary>
    public static void ApplyAcceptEncodingHeaders(HttpClient client, IEnumerable<string>? encodings)
    {
        ArgumentHelpers.ThrowIfNull(client);
        if (encodings == null)
            return;

        foreach (var encoding in encodings.Where(i => !string.IsNullOrWhiteSpace(i)).Select(i => i.Trim().ToLowerInvariant()).Distinct()) {
            if (!LyoHttpClientHandler.IsSupportedResponseEncoding(encoding))
                continue;

            if (client.DefaultRequestHeaders.AcceptEncoding.All(i => !string.Equals(i.Value, encoding, StringComparison.OrdinalIgnoreCase)))
                client.DefaultRequestHeaders.AcceptEncoding.Add(new(encoding));
        }
    }

    private ByteArrayContent CreateJsonContent<TRequest>(TRequest body)
    {
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(body, SerializerOptions);
        var shouldCompress = BaseOptions.RequestCompression != LyoHttpRequestCompressionType.None && jsonBytes.Length >= Math.Max(0, BaseOptions.RequestCompressionMinBytes);
        var payload = shouldCompress ? CompressBytes(jsonBytes, BaseOptions.RequestCompression) : jsonBytes;
        var content = new ByteArrayContent(payload);
        content.Headers.ContentType = new(FileTypeInfo.Json.MimeType);
        if (shouldCompress)
            content.Headers.ContentEncoding.Add(ToContentEncodingValue(BaseOptions.RequestCompression));

        return content;
    }

    private static byte[] CompressBytes(byte[] data, LyoHttpRequestCompressionType compressionType)
    {
        using var output = new MemoryStream();
        Stream compressionStream = compressionType switch {
            LyoHttpRequestCompressionType.Gzip => new GZipStream(output, CompressionLevel.Fastest, true),
            LyoHttpRequestCompressionType.Deflate => new DeflateStream(output, CompressionLevel.Fastest, true),
#if NETSTANDARD2_0
            LyoHttpRequestCompressionType.Brotli => throw new NotSupportedException("Brotli request compression requires a newer target framework."),
#else
            LyoHttpRequestCompressionType.Brotli => new BrotliStream(output, CompressionLevel.Fastest, true),
#endif
            var _ => throw new ArgumentOutOfRangeException(nameof(compressionType), compressionType, null)
        };

        using (compressionStream) {
#if NET9_0_OR_GREATER
            compressionStream.Write(data);
#else
            compressionStream.Write(data, 0, data.Length);
#endif
        }

        return output.ToArray();
    }

    private static string ToContentEncodingValue(LyoHttpRequestCompressionType compressionType)
        => compressionType switch {
            LyoHttpRequestCompressionType.Gzip => LyoContentEncodings.GZip,
            LyoHttpRequestCompressionType.Deflate => LyoContentEncodings.Deflate,
            LyoHttpRequestCompressionType.Brotli => LyoContentEncodings.Brotli,
            var _ => throw new ArgumentOutOfRangeException(nameof(compressionType), compressionType, null)
        };

    private async Task<TResult> PostFileAsAsyncCore<TResult>(
        string uri,
        Stream stream,
        FileTypeInfo fileType,
        string fileName,
        Action<HttpRequestMessage>? before,
        CancellationToken ct)
    {
        using (Logger.BeginScope("{RequestMethodName} {Uri} {ResultTypeName}", PostVerb, uri, typeof(TResult).FullName)) {
            using var form = new MultipartFormDataContent();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new(fileType.MimeType);
            form.Add(fileContent, "file", fileName);
            using var request = CreateRequest(HttpMethod.Post, uri);
            request.Content = form;
            using var response = await SendAsync(request, before, ct).ConfigureAwait(false);
            return await DeserializeRequiredAsync<TResult>(response, ct).ConfigureAwait(false);
        }
    }

    /// <summary>Reads and optionally decompresses a JSON payload.</summary>
    protected async Task<TResult?> DeserializeResponseNullableAsync<TResult>(HttpResponseMessage response, CancellationToken ct)
    {
        var content = await ReadDecodedResponseBytesAsync(response, ct).ConfigureAwait(false);
        if (content.Length == 0)
            return default;

        try {
            return JsonSerializer.Deserialize<TResult>(content, SerializerOptions);
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Couldn't deserialize type {ResponseType} from response", typeof(TResult).FullName);
        }

        return default;
    }

    private async Task<TResult> DeserializeRequiredAsync<TResult>(HttpResponseMessage response, CancellationToken ct)
    {
        var result = await DeserializeAsync<TResult>(response, ct).ConfigureAwait(false);
        OperationHelpers.ThrowIfNull(result, "Deserialization returned null");
        return result;
    }

    internal static async Task<byte[]> ReadResponseBytesAsync(HttpResponseMessage response, CancellationToken ct)
    {
#if NET9_0_OR_GREATER
        return await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
#else
        return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
#endif
    }

    internal static async Task<string> ReadResponseStringAsync(HttpResponseMessage response, CancellationToken ct)
    {
#if NET9_0_OR_GREATER
        return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
#else
        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
    }

    internal static async Task<byte[]> ReadDecodedResponseBytesAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var content = await ReadResponseBytesAsync(response, ct).ConfigureAwait(false);
        return DecodeJsonPayload(content, response.Content.Headers.ContentEncoding);
    }

    /// <summary>Reads the response body as UTF-8, applying Content-Encoding and JSON preamble stripping used by Lyo APIs.</summary>
    protected internal static async Task<string> ReadDecodedResponseStringAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var content = await ReadDecodedResponseBytesAsync(response, ct).ConfigureAwait(false);
        return Encoding.UTF8.GetString(content);
    }

    private static async Task<string?> TryReadBodyPreviewAsync(HttpResponseMessage response, CancellationToken ct)
    {
        try {
            var text = await ReadDecodedResponseStringAsync(response, ct).ConfigureAwait(false);
            return LyoHttpException.Truncate(text);
        }
        catch {
            return null;
        }
    }

    private static byte[] DecodeJsonPayload(byte[] content, IEnumerable<string>? contentEncodings)
    {
        if (content.Length == 0)
            return content;

        var decoded = DecodeByContentEncoding(content, contentEncodings);
        decoded = StripJsonPreamble(decoded);
        if (IsGzip(decoded))
            decoded = DecompressGzip(decoded);
        else if (IsDeflate(decoded))
            decoded = DecompressDeflate(decoded);

        return StripJsonPreamble(decoded);
    }

    private static byte[] DecodeByContentEncoding(byte[] content, IEnumerable<string>? contentEncodings)
    {
        if (contentEncodings == null)
            return content;

        var decoded = content;
        foreach (var raw in contentEncodings.Reverse()) {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var encoding = raw.Trim().ToLowerInvariant();
            if (encoding == LyoContentEncodings.GZip)
                decoded = DecompressGzip(decoded);
            else if (encoding == LyoContentEncodings.Deflate)
                decoded = DecompressDeflate(decoded);
#if !NETSTANDARD2_0
            else if (encoding == LyoContentEncodings.Brotli)
                decoded = DecompressBrotli(decoded);
#endif
        }

        return decoded;
    }

    private static byte[] StripJsonPreamble(byte[] content)
    {
        if (content.Length == 0)
            return content;

        var offset = 0;
        while (offset < content.Length) {
            if (content.Length >= offset + 3 && content[offset] == 0xEF && content[offset + 1] == 0xBB && content[offset + 2] == 0xBF) {
                offset += 3;
                continue;
            }

            if (IsGzip(content, offset) || IsDeflate(content, offset))
                break;

            var current = content[offset];
            if (current == '{' || current == '[' || current == '"' || current == '-' || current is >= (byte)'0' and <= (byte)'9')
                break;

            if (current is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n' || current < 0x20) {
                offset++;
                continue;
            }

            break;
        }

        return offset == 0 ? content : content.AsSpan(offset).ToArray();
    }

    private static bool IsGzip(byte[] content, int offset = 0)
        => content.Length >= offset + 3 && content[offset] == 0x1F && content[offset + 1] == 0x8B && content[offset + 2] == 0x08;

    private static bool IsDeflate(byte[] content, int offset = 0) => content.Length >= offset + 2 && content[offset] == 0x78 && content[offset + 1] is 0x01 or 0x5E or 0x9C or 0xDA;

    private static byte[] DecompressGzip(byte[] content)
    {
        using var input = new MemoryStream(content, false);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }

    private static byte[] DecompressDeflate(byte[] content)
    {
        using var input = new MemoryStream(content, false);
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        deflate.CopyTo(output);
        return output.ToArray();
    }

#if !NETSTANDARD2_0
    private static byte[] DecompressBrotli(byte[] content)
    {
        using var input = new MemoryStream(content, false);
        using var brotli = new BrotliStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        brotli.CopyTo(output);
        return output.ToArray();
    }
#endif

    /// <inheritdoc />
    public string ToQueryString<T>(T obj, string? enumerableDelimiter = null)
    {
        if (obj == null)
            return string.Empty;

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var queryParameters = new List<string>();
        foreach (var prop in properties) {
            var value = prop.GetValue(obj);
            if (value == null)
                continue;

            var jsonAttr = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
            var paramName = jsonAttr?.Name ?? SerializerOptions.PropertyNamingPolicy?.ConvertName(prop.Name) ?? prop.Name;
            if (value is string strVal)
                queryParameters.Add($"{WebUtility.UrlEncode(paramName)}={WebUtility.UrlEncode(strVal)}");
            else if (value is IEnumerable enumerable and not string) {
                var serializedItems = enumerable.Cast<object>().Where(x => x != null).Select(x => SerializeValue(x, prop));
                if (string.IsNullOrEmpty(enumerableDelimiter))
                    queryParameters.AddRange(serializedItems.Select(item => $"{WebUtility.UrlEncode(paramName)}={WebUtility.UrlEncode(item)}"));
                else
                    queryParameters.Add($"{WebUtility.UrlEncode(paramName)}={WebUtility.UrlEncode(string.Join(enumerableDelimiter, serializedItems))}");
            }
            else {
                var serializedValue = SerializeValue(value, prop);
                queryParameters.Add($"{WebUtility.UrlEncode(paramName)}={WebUtility.UrlEncode(serializedValue)}");
            }
        }

        return string.Join("&", queryParameters);
    }

    private JsonConverter? GetMatchingConverter(Type type, PropertyInfo prop)
    {
        if (_savedConverters.TryGetValue($"{type.FullName}:{prop.Name}", out var c))
            return c;

        var attr = prop.GetCustomAttribute<JsonConverterAttribute>();
        if (attr?.ConverterType != null) {
            var converter = (JsonConverter?)Activator.CreateInstance(attr.ConverterType);
            _savedConverters.TryAdd($"{type.FullName}:{prop.Name}", converter!);
            return converter;
        }

        foreach (var converter in SerializerOptions.Converters) {
            if (converter.CanConvert(type))
                return converter;
        }

        return null;
    }

    private string? SerializeValue(object val, PropertyInfo prop)
    {
        var converter = GetMatchingConverter(val.GetType(), prop);
        if (converter == null)
            return val.ToString();

        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        var converterType = converter.GetType();
        var method = converterType.GetMethod("Write", BindingFlags.Instance | BindingFlags.Public);
        if (method == null)
            return val.ToString();

        method.Invoke(converter, [writer, val, SerializerOptions]);
        writer.Flush();
        var json = Encoding.UTF8.GetString(stream.ToArray());
        return json.Trim('"');
    }

    private static string? TryFileNameFromUri(Uri? uri)
    {
        if (uri == null)
            return null;

        var leaf = Path.GetFileName(uri.AbsolutePath);
        return string.IsNullOrWhiteSpace(leaf) ? null : leaf;
    }
}
