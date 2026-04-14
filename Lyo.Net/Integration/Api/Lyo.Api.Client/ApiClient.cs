using System.Collections;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lyo.Api.Models.Error;
using Lyo.Common.Records;
using Lyo.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Api.Client;

public class ApiClient : IApiClient
{
    private const string Get = "GET";
    private const string Post = "POST";
    private const string Put = "PUT";
    private const string Patch = "PATCH";
    private const string Delete = "DELETE";

    //Save type:property converters based on attribute so we don't need to recreate
    private readonly ConcurrentDictionary<string, JsonConverter> _savedConverters = new();
    protected readonly ApiClientOptions BaseOptions;

    protected readonly HttpClient HttpClient;
    protected readonly ILogger Logger;
    public readonly JsonSerializerOptions SerializerOptions;

    public ApiClient(ILogger? logger = null, HttpClient? httpClient = null, JsonSerializerOptions? serializerOptions = null, ApiClientOptions? options = null)
    {
        Logger = logger ?? NullLogger<ApiClient>.Instance;
        BaseOptions = options ?? new();
        HttpClient = httpClient ?? CreateHttpClient(BaseOptions);
        //todo should be moved to each implementation and pull in version from library version
        HttpClient.DefaultRequestHeaders.UserAgent.Add(new("Lyo", "1.0"));
        ConfigureAcceptEncodingHeaders();
        SerializerOptions = serializerOptions ?? new JsonSerializerOptions();
        if (HttpClient.BaseAddress == null && !string.IsNullOrWhiteSpace(BaseOptions.BaseUrl))
            HttpClient.BaseAddress = new(BaseOptions.BaseUrl!.TrimEnd('/') + "/");
    }

    public JsonSerializerOptions GetSerializerOptions() => SerializerOptions;

    public HttpClient GetClient() => HttpClient;

    //todo - add try/catch around serialization, add a way to handle or a flag to enable throwing on exception types, add options for cancelled, etc or add events
    public async Task<TResult?> GetAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        if (HttpClient.BaseAddress == null)
            UriHelpers.ThrowIfInvalidAbsoluteUri(uri, nameof(uri));

        using (Logger.BeginScope("{RequestMethodName} {Uri} {ResultTypeName}", Get, uri, typeof(TResult).FullName)) {
            Logger.LogDebug("Sending request");
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            before?.Invoke(request);
            using var response = await HttpClient.SendAsync(request, ct).ConfigureAwait(false);
            Logger.LogDebug("{ResponseStatusCode} Response received", response.StatusCode);
            await EnsureSuccessStatusCodeOrThrowApiExceptionAsync(response, ct).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NoContent)
                return default;

            return await DeserializeResponseNullableAsync<TResult>(response, ct).ConfigureAwait(false);
        }
    }

    public async Task<TResult?> GetAsAsync<TRequest, TResult>(
        string uri,
        TRequest? query,
        string? enumerableDelimiter = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
    {
        if (HttpClient.BaseAddress == null)
            UriHelpers.ThrowIfInvalidAbsoluteUri(uri, nameof(uri));

        using (Logger.BeginScope("{RequestMethodName} {Uri} {QueryTypeName} {ResultTypeName}", Get, uri, typeof(TRequest).FullName, typeof(TResult).FullName)) {
            var queryParams = ToQueryString(query, enumerableDelimiter);
            uri = string.IsNullOrEmpty(queryParams) ? uri : UriHelpers.AppendQueryString(uri, queryParams);
            OperationHelpers.ThrowIf(uri.Length > 4096, "Uri length too long");
            Logger.LogDebug("Sending request");
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            before?.Invoke(request);
            using var response = await HttpClient.SendAsync(request, ct).ConfigureAwait(false);
            Logger.LogDebug("{ResponseStatusCode} Response received", response.StatusCode);
            await EnsureSuccessStatusCodeOrThrowApiExceptionAsync(response, ct).ConfigureAwait(false);
            return await DeserializeResponseNullableAsync<TResult>(response, ct).ConfigureAwait(false);
        }
    }

    public async Task<byte[]> GetFileAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        if (HttpClient.BaseAddress == null)
            UriHelpers.ThrowIfInvalidAbsoluteUri(uri, nameof(uri));

        using (Logger.BeginScope("{RequestMethodName} {Uri}", HttpMethod.Get, uri)) {
            Logger.LogDebug("Sending GET request for file");
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            before?.Invoke(request);
            using var response = await HttpClient.SendAsync(request, ct).ConfigureAwait(false);
            Logger.LogDebug("{ResponseStatusCode} Response received", response.StatusCode);
            await EnsureSuccessStatusCodeOrThrowApiExceptionAsync(response, ct).ConfigureAwait(false);
            return await ReadResponseBytesAsync(response, ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<(Stream Content, string? FileName, long? ContentLength)> GetFileStreamAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        if (HttpClient.BaseAddress == null)
            UriHelpers.ThrowIfInvalidAbsoluteUri(uri, nameof(uri));

        Logger.LogDebug("Sending streaming GET request for file: {Uri}", uri);
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        before?.Invoke(request);
        var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        try {
            await EnsureSuccessStatusCodeOrThrowApiExceptionAsync(response, ct).ConfigureAwait(false);
#if NET5_0_OR_GREATER
            var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
#else
            var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
            var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"');
            var contentLength = response.Content.Headers.ContentLength;
            return (new HttpResponseStream(stream, response, request), fileName, contentLength);
        }
        catch {
            response.Dispose();
            request.Dispose();
            throw;
        }
    }

    /// <summary>Downloads a file and returns the content with its <see cref="FileTypeInfo" /> derived from the response Content-Type header.</summary>
    public async Task<(byte[] Content, FileTypeInfo FileType)> GetFileWithTypeAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        if (HttpClient.BaseAddress == null)
            UriHelpers.ThrowIfInvalidAbsoluteUri(uri, nameof(uri));

        using (Logger.BeginScope("{RequestMethodName} {Uri}", HttpMethod.Get, uri)) {
            Logger.LogDebug("Sending GET request for file");
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            before?.Invoke(request);
            using var response = await HttpClient.SendAsync(request, ct).ConfigureAwait(false);
            Logger.LogDebug("{ResponseStatusCode} Response received", response.StatusCode);
            await EnsureSuccessStatusCodeOrThrowApiExceptionAsync(response, ct).ConfigureAwait(false);
            var fileType = FileTypeInfo.FromMimeType(response.Content.Headers.ContentType?.MediaType);
            var content = await ReadResponseBytesAsync(response, ct).ConfigureAwait(false);
            return (content, fileType);
        }
    }

    public async Task<TResult> PatchAsAsync<TRequest, TResult>(string uri, TRequest? body, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        if (HttpClient.BaseAddress == null)
            UriHelpers.ThrowIfInvalidAbsoluteUri(uri, nameof(uri));

        using (Logger.BeginScope("{RequestMethodName} {Uri} {BodyTypeName} {ResultTypeName}", Patch, uri, typeof(TRequest).FullName, typeof(TResult).FullName)) {
            Logger.LogDebug("Sending request");
            using var content = CreateJsonContent(body);
#if NETSTANDARD2_0
            using var request = new HttpRequestMessage(new(Patch), uri);
#else
            using var request = new HttpRequestMessage(HttpMethod.Patch, uri);
#endif
            request.Content = content;
            before?.Invoke(request);
            using var response = await HttpClient.SendAsync(request, ct).ConfigureAwait(false);
            Logger.LogDebug("{ResponseStatusCode} Response received", response.StatusCode);
            await EnsureSuccessStatusCodeOrThrowApiExceptionAsync(response, ct).ConfigureAwait(false);
            return await DeserializeResponseAsync<TResult>(response, ct).ConfigureAwait(false);
        }
    }

    public async Task<TResult> PutAsAsync<TRequest, TResult>(string uri, TRequest? body, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        if (HttpClient.BaseAddress == null)
            UriHelpers.ThrowIfInvalidAbsoluteUri(uri, nameof(uri));

        using (Logger.BeginScope("{RequestMethodName} {Uri} {BodyTypeName} {ResultTypeName}", Put, uri, typeof(TRequest).FullName, typeof(TResult).FullName)) {
            Logger.LogDebug("Sending request");
            using var content = CreateJsonContent(body);
            using var request = new HttpRequestMessage(HttpMethod.Put, uri);
            request.Content = content;
            before?.Invoke(request);
            using var response = await HttpClient.SendAsync(request, ct).ConfigureAwait(false);
            Logger.LogDebug("{ResponseStatusCode} Response received", response.StatusCode);
            await EnsureSuccessStatusCodeOrThrowApiExceptionAsync(response, ct).ConfigureAwait(false);
            return await DeserializeResponseAsync<TResult>(response, ct).ConfigureAwait(false);
        }
    }

    public async Task<TResult> PostAsAsync<TRequest, TResult>(string uri, TRequest? body, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        if (HttpClient.BaseAddress == null)
            UriHelpers.ThrowIfInvalidAbsoluteUri(uri, nameof(uri));

        using (Logger.BeginScope("{RequestMethodName} {Uri} {BodyTypeName} {ResultTypeName}", Post, uri, typeof(TRequest).FullName, typeof(TResult).FullName)) {
            Logger.LogDebug("Sending request");
            using var content = CreateJsonContent(body);
            using var request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Content = content;
            before?.Invoke(request);
            using var response = await HttpClient.SendAsync(request, ct).ConfigureAwait(false);
            Logger.LogDebug("{ResponseStatusCode} Response received", response.StatusCode);
            await EnsureSuccessStatusCodeOrThrowApiExceptionAsync(response, ct).ConfigureAwait(false);
            return await DeserializeResponseAsync<TResult>(response, ct).ConfigureAwait(false);
        }
    }

    public async Task<TResult> PostAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        if (HttpClient.BaseAddress == null)
            UriHelpers.ThrowIfInvalidAbsoluteUri(uri, nameof(uri));

        using (Logger.BeginScope("{RequestMethodName} {Uri} {ResultTypeName}", Post, uri, typeof(TResult).FullName)) {
            Logger.LogDebug("Sending request");
            using var request = new HttpRequestMessage(HttpMethod.Post, uri);
            before?.Invoke(request);
            using var response = await HttpClient.SendAsync(request, ct).ConfigureAwait(false);
            Logger.LogDebug("{ResponseStatusCode} Response received", response.StatusCode);
            await EnsureSuccessStatusCodeOrThrowApiExceptionAsync(response, ct).ConfigureAwait(false);
            var responseJson = await ReadDecodedResponseStringAsync(response, ct).ConfigureAwait(false);
            var result = JsonSerializer.Deserialize<TResult>(responseJson, SerializerOptions);
            OperationHelpers.ThrowIfNull(result, "Deserialization returned null");
            return result;
        }
    }

    public async Task<byte[]> PostAsBinaryAsync<TRequest>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        if (HttpClient.BaseAddress == null)
            UriHelpers.ThrowIfInvalidAbsoluteUri(uri, nameof(uri));

        using (Logger.BeginScope("{RequestMethodName} {Uri} {RequestTypeName}", Post, uri, typeof(TRequest).FullName)) {
            Logger.LogDebug("Sending request");
            using var content = CreateJsonContent(request);
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, uri);
            httpRequest.Content = content;
            before?.Invoke(httpRequest);
            using var response = await HttpClient.SendAsync(httpRequest, ct).ConfigureAwait(false);
            Logger.LogDebug("{ResponseStatusCode} Response received", response.StatusCode);
            await EnsureSuccessStatusCodeOrThrowApiExceptionAsync(response, ct).ConfigureAwait(false);
            return await ReadResponseBytesAsync(response, ct).ConfigureAwait(false);
        }
    }

    public async Task<TResult> PostFileAsAsync<TResult>(
        string uri,
        Stream stream,
        FileTypeInfo fileType,
        string? fileName = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
    {
        if (HttpClient.BaseAddress == null)
            UriHelpers.ThrowIfInvalidAbsoluteUri(uri, nameof(uri));

        OperationHelpers.ThrowIfNotReadable(stream, $"Stream '{nameof(stream)}' must be readable.");
        var safeFileName = !string.IsNullOrWhiteSpace(fileName) ? FileHelpers.GetValidFileName(fileName, nameof(fileName)) : $"file{fileType.DefaultExtension}";
        return await PostFileAsAsyncCore<TResult>(uri, stream, fileType, safeFileName, before, ct).ConfigureAwait(false);
    }

    public async Task<TResult> PostFileAsAsync<TResult>(string uri, Stream stream, string fileName, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        if (HttpClient.BaseAddress == null)
            UriHelpers.ThrowIfInvalidAbsoluteUri(uri, nameof(uri));

        OperationHelpers.ThrowIfNotReadable(stream, $"Stream '{nameof(stream)}' must be readable.");
        FileHelpers.ThrowIfFileNameInvalid(fileName, nameof(fileName));
        var fileType = FileTypeInfo.FromFilePath(fileName);
        return await PostFileAsAsyncCore<TResult>(uri, stream, fileType, fileName, before, ct).ConfigureAwait(false);
    }

    public async Task<TResult> PostFileAsAsync<TResult>(
        string uri,
        byte[] data,
        FileTypeInfo fileType,
        string? fileName = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(data, nameof(data));
        using var stream = new MemoryStream(data);
        return await PostFileAsAsync<TResult>(uri, stream, fileType, fileName, before, ct).ConfigureAwait(false);
    }

    public async Task<TResult> PostFileAsAsync<TResult>(string uri, byte[] data, string fileName, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrEmpty(data, nameof(data));
        var fileType = FileTypeInfo.FromFilePath(fileName);
        using var stream = new MemoryStream(data);
        return await PostFileAsAsync<TResult>(uri, stream, fileType, fileName, before, ct).ConfigureAwait(false);
    }

    public async Task<TResult> PostFileAsAsync<TResult>(string uri, string filePath, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        if (HttpClient.BaseAddress == null)
            UriHelpers.ThrowIfInvalidAbsoluteUri(uri, nameof(uri));

        ArgumentHelpers.ThrowIfFileNotFound(filePath, nameof(filePath));
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

    public async Task<TResult> DeleteAsAsync<TRequest, TResult>(string uri, TRequest? body = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        if (HttpClient.BaseAddress == null)
            UriHelpers.ThrowIfInvalidAbsoluteUri(uri, nameof(uri));

        using (Logger.BeginScope("{RequestMethodName} {Uri} {BodyTypeName} {ResultTypeName}", Delete, uri, typeof(TRequest).FullName, typeof(TResult).FullName)) {
            Logger.LogDebug("Sending request");
            using var request = new HttpRequestMessage(HttpMethod.Delete, uri);
            if (body != null)
                request.Content = CreateJsonContent(body);

            before?.Invoke(request);
            using var response = await HttpClient.SendAsync(request, ct).ConfigureAwait(false);
            Logger.LogDebug("{ResponseStatusCode} Response received", response.StatusCode);
            await EnsureSuccessStatusCodeOrThrowApiExceptionAsync(response, ct).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NoContent)
                return default!;

            return await DeserializeResponseAsync<TResult>(response, ct).ConfigureAwait(false);
        }
    }

    public async Task<TResult> DeleteAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        if (HttpClient.BaseAddress == null)
            UriHelpers.ThrowIfInvalidAbsoluteUri(uri, nameof(uri));

        using (Logger.BeginScope("{RequestMethodName} {Uri} {ResultTypeName}", Delete, uri, typeof(TResult).FullName)) {
            Logger.LogDebug("Sending request");
            using var request = new HttpRequestMessage(HttpMethod.Delete, uri);
            before?.Invoke(request);
            using var response = await HttpClient.SendAsync(request, ct).ConfigureAwait(false);
            Logger.LogDebug("{ResponseStatusCode} Response received", response.StatusCode);
            await EnsureSuccessStatusCodeOrThrowApiExceptionAsync(response, ct).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NoContent)
                return default!;

            return await DeserializeResponseAsync<TResult>(response, ct).ConfigureAwait(false);
        }
    }

    public void Dispose() => HttpClient.Dispose();

    private async Task EnsureSuccessStatusCodeOrThrowApiExceptionAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return;

        if (!BaseOptions.EnsureStatusCode)
            return;

        LyoProblemDetails? problemDetails = null;
        var message = $"Request failed: {(int)response.StatusCode} {response.ReasonPhrase}";
        if (response.Content.Headers.ContentType?.MediaType?.Contains("json", StringComparison.OrdinalIgnoreCase) != true)
            throw new ApiException((int)response.StatusCode, message, problemDetails);

        try {
            var json = await ReadDecodedResponseStringAsync(response, ct).ConfigureAwait(false);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } };
            problemDetails = JsonSerializer.Deserialize<LyoProblemDetails>(json, options);
            if (problemDetails != null && !string.IsNullOrEmpty(problemDetails.Detail))
                message = problemDetails.Detail;
        }
        catch {
            // Fall back to generic message if body cannot be parsed
        }

        throw new ApiException((int)response.StatusCode, message, problemDetails);
    }

    private static HttpClient CreateHttpClient(ApiClientOptions options)
    {
        if (!options.EnableAutoResponseDecompression)
            return new();

        var handler = new HttpClientHandler { AutomaticDecompression = ToDecompressionMethods(options.AcceptEncodings) };
        return new(handler);
    }

    private void ConfigureAcceptEncodingHeaders()
    {
        if (BaseOptions.AcceptEncodings.Length == 0)
            return;

        foreach (var encoding in BaseOptions.AcceptEncodings.Where(i => !string.IsNullOrWhiteSpace(i)).Select(i => i.Trim().ToLowerInvariant()).Distinct()) {
            if (!IsSupportedResponseEncoding(encoding))
                continue;

            if (HttpClient.DefaultRequestHeaders.AcceptEncoding.All(i => !string.Equals(i.Value, encoding, StringComparison.OrdinalIgnoreCase)))
                HttpClient.DefaultRequestHeaders.AcceptEncoding.Add(new(encoding));
        }
    }

    private ByteArrayContent CreateJsonContent<TRequest>(TRequest body)
    {
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(body, SerializerOptions);
        var shouldCompress = BaseOptions.RequestCompression != ApiRequestCompressionType.None && jsonBytes.Length >= Math.Max(0, BaseOptions.RequestCompressionMinBytes);
        var payload = shouldCompress ? CompressBytes(jsonBytes, BaseOptions.RequestCompression) : jsonBytes;
        var content = new ByteArrayContent(payload);
        content.Headers.ContentType = new(FileTypeInfo.Json.MimeType);
        if (shouldCompress)
            content.Headers.ContentEncoding.Add(ToContentEncodingValue(BaseOptions.RequestCompression));

        return content;
    }

    private static byte[] CompressBytes(byte[] data, ApiRequestCompressionType compressionType)
    {
        using var output = new MemoryStream();
        Stream compressionStream = compressionType switch {
            ApiRequestCompressionType.Gzip => new GZipStream(output, CompressionLevel.Fastest, true),
            ApiRequestCompressionType.Deflate => new DeflateStream(output, CompressionLevel.Fastest, true),
#if NETSTANDARD2_0
            ApiRequestCompressionType.Brotli => throw new NotSupportedException("Brotli request compression requires a newer target framework."),
#else
            ApiRequestCompressionType.Brotli => new BrotliStream(output, CompressionLevel.Fastest, true),
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

    private static string ToContentEncodingValue(ApiRequestCompressionType compressionType)
        => compressionType switch {
            ApiRequestCompressionType.Gzip => "gzip",
            ApiRequestCompressionType.Deflate => "deflate",
            ApiRequestCompressionType.Brotli => "br",
            var _ => throw new ArgumentOutOfRangeException(nameof(compressionType), compressionType, null)
        };

    private static DecompressionMethods ToDecompressionMethods(IEnumerable<string>? encodings)
    {
        var methods = DecompressionMethods.None;
        if (encodings == null)
            return methods;

        foreach (var raw in encodings) {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var encoding = raw.Trim().ToLowerInvariant();
            if (encoding == "gzip")
                methods |= DecompressionMethods.GZip;
            else if (encoding == "deflate")
                methods |= DecompressionMethods.Deflate;
#if !NETSTANDARD2_0
            else if (encoding == "br")
                methods |= DecompressionMethods.Brotli;
#endif
        }

        return methods;
    }

    private static bool IsSupportedResponseEncoding(string encoding)
    {
        if (encoding == "gzip" || encoding == "deflate")
            return true;
#if !NETSTANDARD2_0
        if (encoding == "br")
            return true;
#endif
        return false;
    }

    private async Task<TResult> PostFileAsAsyncCore<TResult>(
        string uri,
        Stream stream,
        FileTypeInfo fileType,
        string fileName,
        Action<HttpRequestMessage>? before,
        CancellationToken ct)
    {
        using (Logger.BeginScope("{RequestMethodName} {Uri} {ResultTypeName}", Post, uri, typeof(TResult).FullName)) {
            Logger.LogDebug("Sending request");
            using var form = new MultipartFormDataContent();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new(fileType.MimeType);
            form.Add(fileContent, "file", fileName);
            using var request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Content = form;
            before?.Invoke(request);
            using var response = await HttpClient.SendAsync(request, ct).ConfigureAwait(false);
            Logger.LogDebug("{ResponseStatusCode} Response received", response.StatusCode);
            await EnsureSuccessStatusCodeOrThrowApiExceptionAsync(response, ct).ConfigureAwait(false);
            return await DeserializeResponseAsync<TResult>(response, ct).ConfigureAwait(false);
        }
    }

    private async Task<TResult?> DeserializeResponseNullableAsync<TResult>(HttpResponseMessage response, CancellationToken ct)
    {
        var content = await ReadDecodedResponseBytesAsync(response, ct).ConfigureAwait(false);
        if (content.Length == 0)
            return default;

        return JsonSerializer.Deserialize<TResult>(content, SerializerOptions);
    }

    private async Task<TResult> DeserializeResponseAsync<TResult>(HttpResponseMessage response, CancellationToken ct)
    {
        var result = await DeserializeResponseNullableAsync<TResult>(response, ct).ConfigureAwait(false);
        OperationHelpers.ThrowIfNull(result, "Deserialization returned null");
        return result;
    }

    private static async Task<byte[]> ReadResponseBytesAsync(HttpResponseMessage response, CancellationToken ct)
    {
#if NET9_0_OR_GREATER
        return await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
#else
        return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
#endif
    }

    private static async Task<string> ReadResponseStringAsync(HttpResponseMessage response, CancellationToken ct)
    {
#if NET9_0_OR_GREATER
        return await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
#else
        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
    }

    private static async Task<byte[]> ReadDecodedResponseBytesAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var content = await ReadResponseBytesAsync(response, ct).ConfigureAwait(false);
        return DecodeJsonPayload(content, response.Content.Headers.ContentEncoding);
    }

    private static async Task<string> ReadDecodedResponseStringAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var content = await ReadDecodedResponseBytesAsync(response, ct).ConfigureAwait(false);
        return Encoding.UTF8.GetString(content);
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
            if (encoding == "gzip")
                decoded = DecompressGzip(decoded);
            else if (encoding == "deflate")
                decoded = DecompressDeflate(decoded);
#if !NETSTANDARD2_0
            else if (encoding == "br")
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

    /// <summary>
    /// Creates a query string for an object. If specified, it will create 1 parameter for the enumerable type using the delimiter to separate, <br />Ex: ',' is specified and the
    /// object's Ids field is enumerable, the result would be ids=1,2,3 instead of ids=1 ids=2
    /// </summary>
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

            // Determine parameter name
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

    /// <summary>Gets the matching converter for the property type, precedence order follows: <br />1. Property Attribute <br />2. JsonSerializerOptions</summary>
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
}