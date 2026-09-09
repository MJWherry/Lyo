using System.Text.Json;
using Lyo.Common.Metadata.Records;
using Lyo.Http.Client.Plan;
using Lyo.Http.Client.Session;

namespace Lyo.Http.Client;

/// <summary>Generic HTTP client: JSON verbs, files, plans, and a session cookie jar. Non-success throws <see cref="LyoHttpException" /> (no problem-details).</summary>
public interface ILyoHttpClient : IDisposable
{
    /// <summary>Effective JSON options for this client.</summary>
    JsonSerializerOptions GetSerializerOptions();

    /// <summary>Underlying <see cref="HttpClient" />. Do not dispose it when the client came from <c>IHttpClientFactory</c>.</summary>
    HttpClient GetClient();

    /// <summary>Logical session (cookies, pinned UA, items).</summary>
    ILyoHttpSession Session { get; }

    /// <summary>Optional compose-without-subclassing hooks.</summary>
    LyoHttpClientHooks Hooks { get; }

    /// <summary>Starts an unnamed plan with GET <paramref name="uri" />.</summary>
    HttpClientPlanBuilder Get(string uri);

    /// <summary>Starts an unnamed plan with POST <paramref name="uri" />.</summary>
    HttpClientPlanBuilder Post(string uri);

    /// <summary>Runs a previously built plan.</summary>
    Task<HttpClientPlanRunResult> RunAsync(HttpClientPlan plan, HttpClientPlanRuntime? runtime = null, CancellationToken ct = default);

    /// <summary>GET and deserialize JSON.</summary>
    Task<TResult?> GetAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default);

    /// <summary>GET with a DTO flattened to a query string, then deserialize JSON.</summary>
    Task<TResult?> GetAsAsync<TRequest, TResult>(
        string uri,
        TRequest? query = default,
        string? enumerableDelimiter = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default);

    /// <summary>Downloads a file and returns bytes already decoded by the handler. Marks the request <c>StreamBinary</c> so Flared skips ThroughSolver.</summary>
    Task<byte[]> GetFileAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default);

    /// <summary>Downloads a file as a stream without buffering the whole response. Dispose the stream to release the response.</summary>
    Task<(Stream Content, string? FileName, long? ContentLength)> GetFileStreamAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default);

    /// <summary>
    /// Downloads a file and returns the bytes plus <see cref="FileTypeInfo" /> taken from the response Content-Type header. Marks the request <c>StreamBinary</c> so Flared skips
    /// ThroughSolver.
    /// </summary>
    Task<(byte[] Content, FileTypeInfo FileType)> GetFileWithTypeAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default);

    /// <summary>Streams a download to <paramref name="destinationPath" /> with optional progress.</summary>
    Task<string> DownloadToFileAsync(
        string uri,
        string destinationPath,
        IProgress<LyoHttpDownloadProgress>? progress = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default);

    /// <summary>PUT JSON and deserialize.</summary>
    Task<TResult> PutAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default);

    /// <summary>PATCH JSON and deserialize.</summary>
    Task<TResult> PatchAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default);

    /// <summary>POST JSON and deserialize.</summary>
    Task<TResult> PostAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default);

    /// <summary>POST with no JSON body and deserialize.</summary>
    Task<TResult> PostAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default);

    /// <summary>POST JSON and return raw bytes.</summary>
    Task<byte[]> PostAsBinaryAsync<TRequest>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default);

    /// <summary>POST a file stream.</summary>
    Task<TResult> PostFileAsAsync<TResult>(
        string uri,
        Stream stream,
        FileTypeInfo fileType,
        string? fileName = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default);

    /// <summary>POST a file stream, inferring type from <paramref name="fileName" />.</summary>
    Task<TResult> PostFileAsAsync<TResult>(string uri, Stream stream, string fileName, Action<HttpRequestMessage>? before = null, CancellationToken ct = default);

    /// <summary>POST file bytes.</summary>
    Task<TResult> PostFileAsAsync<TResult>(
        string uri,
        byte[] data,
        FileTypeInfo fileType,
        string? fileName = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default);

    /// <summary>POST file bytes, inferring type from <paramref name="fileName" />.</summary>
    Task<TResult> PostFileAsAsync<TResult>(string uri, byte[] data, string fileName, Action<HttpRequestMessage>? before = null, CancellationToken ct = default);

    /// <summary>POST a file from disk.</summary>
    Task<TResult> PostFileAsAsync<TResult>(string uri, string filePath, Action<HttpRequestMessage>? before = null, CancellationToken ct = default);

    /// <summary>DELETE with an optional JSON body.</summary>
    Task<TResult> DeleteAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default);

    /// <summary>DELETE with no body.</summary>
    Task<TResult> DeleteAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default);

    /// <summary>Builds a query string from an object.</summary>
    string ToQueryString<T>(T obj, string? enumerableDelimiter = null);
}
