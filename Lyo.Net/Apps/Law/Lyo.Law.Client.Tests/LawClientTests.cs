using System.Text.Json;
using Lyo.Api.Client;
using Lyo.Common.Metadata.Records;
using Lyo.Http.Client;
using Lyo.Http.Client.Plan;
using Lyo.Http.Client.Session;
using Lyo.Law.Client;
using Lyo.Law.Models;
using Lyo.Law.Models.Request;
using Lyo.Query.Models.Common.Request;

namespace Lyo.Law.Client.Tests;

[Trait("Category", "Fast")]
public sealed class LawClientTests
{
    [Fact]
    public async Task UpsertBySource_PostsJurisdictionCodeAndSourceKey()
    {
        var api = new RecordingApiClient();
        var client = new LawClient(api);
        var request = new LawNodeUpsertReq { JurisdictionKey = "US-PA", CodeKey = "consolidated-statutes", SourceKey = "ttl=67&div=0&chpt=11&sctn=5&subsctn=0" };
        await client.UpsertBySourceAsync(request, TestCt);
        Assert.Equal(Constants.Rest.Law.NodesBySource, api.LastUri);
        Assert.Same(request, api.LastBody);
    }

    [Fact]
    public async Task GetByCitation_HitsCitationRoute()
    {
        var api = new RecordingApiClient();
        var client = new LawClient(api);
        await client.GetByCitationAsync("US-PA", "consolidated-statutes", "67 Pa.C.S. § 1105", TestCt);
        Assert.Equal(Constants.Rest.Law.NodeByCitation("US-PA", "consolidated-statutes", "67 Pa.C.S. § 1105"), api.LastUri);
    }
}

internal sealed class RecordingApiClient : IApiClient
{
    private readonly HttpClient _http = new();

    public string? LastUri { get; private set; }

    public object? LastBody { get; private set; }

    public void Dispose() => _http.Dispose();

    public JsonSerializerOptions GetSerializerOptions() => new();

    public HttpClient GetClient() => _http;

    public ILyoHttpSession Session { get; } = new LyoHttpSession();

    public LyoHttpClientHooks Hooks { get; } = new();

    public HttpClientPlanBuilder Get(string uri) => throw new NotImplementedException(uri);

    public HttpClientPlanBuilder Post(string uri) => throw new NotImplementedException(uri);

    public Task<HttpClientPlanRunResult> RunAsync(HttpClientPlan plan, HttpClientPlanRuntime? runtime = null, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<TResult> PostAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        LastUri = uri;
        LastBody = request;
        return Task.FromResult(default(TResult)!);
    }

    public Task<TResult?> GetAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        LastUri = uri;
        return Task.FromResult(default(TResult));
    }

    public Task<TResult> PatchAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<TResult?> GetAsAsync<TRequest, TResult>(
        string uri,
        TRequest? query = default,
        string? enumerableDelimiter = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<TResult> PostAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        LastUri = uri;
        return Task.FromResult(default(TResult)!);
    }

    public Task<TResult> PutAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        LastUri = uri;
        LastBody = request;
        return Task.FromResult(default(TResult)!);
    }

    public Task<byte[]> GetFileAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default) => throw new NotImplementedException(uri);

    public Task<(Stream Content, string? FileName, long? ContentLength)> GetFileStreamAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<(byte[] Content, FileTypeInfo FileType)> GetFileWithTypeAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<string> DownloadToFileAsync(
        string uri,
        string destinationPath,
        IProgress<LyoHttpDownloadProgress>? progress = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<byte[]> PostAsBinaryAsync<TRequest>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<TResult> PostFileAsAsync<TResult>(
        string uri,
        Stream stream,
        FileTypeInfo fileType,
        string? fileName = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
        => Task.FromResult(default(TResult)!);

    public Task<TResult> PostFileAsAsync<TResult>(string uri, Stream stream, string fileName, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => Task.FromResult(default(TResult)!);

    public Task<TResult> PostFileAsAsync<TResult>(
        string uri,
        byte[] data,
        FileTypeInfo fileType,
        string? fileName = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
        => Task.FromResult(default(TResult)!);

    public Task<TResult> PostFileAsAsync<TResult>(string uri, byte[] data, string fileName, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => Task.FromResult(default(TResult)!);

    public Task<TResult> PostFileAsAsync<TResult>(string uri, string filePath, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => Task.FromResult(default(TResult)!);

    public Task<TResult> DeleteAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<TResult> DeleteAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        LastUri = uri;
        return Task.FromResult(default(TResult)!);
    }

    public string ToQueryString<T>(T obj, string? enumerableDelimiter = null) => throw new NotImplementedException();

    public Task<TResult?> QueryProjectAsync<TResult>(string route, ProjectionQueryReq request, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(route);

    public Task<TResult?> QueryConcreteAsync<TResult>(string route, QueryConcreteReq request, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(route);

    public Task<TResult?> QuerySqlAsync<TResult>(string route, QueryRequestBase request, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(route);
}
