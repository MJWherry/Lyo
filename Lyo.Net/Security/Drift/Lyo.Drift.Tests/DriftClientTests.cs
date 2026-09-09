using Lyo.Api.Client;
using Lyo.Api.Models.Common.Response;
using Lyo.Drift.Client;
using Lyo.Drift.Models.Request;
using Lyo.Drift.Models.Response;
using Lyo.Http.Client;
using Lyo.Http.Client.Plan;
using Lyo.Http.Client.Session;
using Lyo.Query.Models.Common.Request;
using System.Text.Json;
using Lyo.Common.Metadata.Records;

namespace Lyo.Drift.Tests;

public sealed class DriftClientTests
{
    [Fact]
    public async Task UpsertInstanceAsync_PostsToUpsertRoute()
    {
        var api = new RecordingApiClient();
        var client = new DriftClient(api, new() { RoutePrefix = "https://api.test" });
        await client.UpsertInstanceAsync(new() { InstanceKey = "k" }, TestContext.Current.CancellationToken);
        Assert.Equal("https://api.test/Drift/Instance/Upsert", api.LastUri);
        Assert.IsType<DriftInstanceReq>(api.LastBody);
    }

    [Fact]
    public async Task HeartbeatAsync_PatchesHeartbeatRoute()
    {
        var api = new RecordingApiClient();
        var client = new DriftClient(api);
        var id = Guid.NewGuid();
        await client.HeartbeatAsync(id, new() { LastHeartbeatUtc = DateTime.UtcNow }, TestContext.Current.CancellationToken);
        Assert.Equal($"Drift/Instance/{id}/Heartbeat", api.LastUri);
    }

    [Fact]
    public async Task DiffAgainstAsync_PostsDiffAgainstRoute()
    {
        var api = new RecordingApiClient();
        var client = new DriftClient(api);
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        await client.DiffAgainstAsync(a, b, TestContext.Current.CancellationToken);
        Assert.Equal($"Drift/Snapshot/{a}/DiffAgainst/{b}", api.LastUri);
    }
}

internal sealed class RecordingApiClient : IApiClient
{
    public string? LastUri { get; private set; }

    public object? LastBody { get; private set; }

    public void Dispose() { }

    public JsonSerializerOptions GetSerializerOptions() => new();

    public HttpClient GetClient() => throw new NotImplementedException();

    public ILyoHttpSession Session { get; } = new LyoHttpSession();

    public LyoHttpClientHooks Hooks { get; } = new();

    public HttpClientPlanBuilder Get(string uri) => throw new NotImplementedException(uri);

    public HttpClientPlanBuilder Post(string uri) => throw new NotImplementedException(uri);

    public Task<HttpClientPlanRunResult> RunAsync(HttpClientPlan plan, HttpClientPlanRuntime? runtime = null, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<TResult?> GetAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        LastUri = uri;
        return Task.FromResult(default(TResult));
    }

    public Task<TResult?> GetAsAsync<TRequest, TResult>(
        string uri,
        TRequest? query = default,
        string? enumerableDelimiter = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
    {
        LastUri = uri;
        return Task.FromResult(default(TResult));
    }

    public Task<TResult> PatchAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        LastUri = uri;
        LastBody = request;
        return Task.FromResult(default(TResult)!);
    }

    public Task<TResult> PostAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        LastUri = uri;
        LastBody = request;
        return Task.FromResult(default(TResult)!);
    }

    public Task<TResult> PostAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        LastUri = uri;
        return Task.FromResult(default(TResult)!);
    }

    public Task<TResult> PutAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<byte[]> GetFileAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default) => throw new NotImplementedException();

    public Task<(Stream Content, string? FileName, long? ContentLength)> GetFileStreamAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<(byte[] Content, FileTypeInfo FileType)> GetFileWithTypeAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<string> DownloadToFileAsync(
        string uri,
        string destinationPath,
        IProgress<LyoHttpDownloadProgress>? progress = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<byte[]> PostAsBinaryAsync<TRequest>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<TResult> PostFileAsAsync<TResult>(
        string uri,
        Stream stream,
        FileTypeInfo fileType,
        string? fileName = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<TResult> PostFileAsAsync<TResult>(string uri, Stream stream, string fileName, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<TResult> PostFileAsAsync<TResult>(
        string uri,
        byte[] data,
        FileTypeInfo fileType,
        string? fileName = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<TResult> PostFileAsAsync<TResult>(string uri, byte[] data, string fileName, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<TResult> PostFileAsAsync<TResult>(string uri, string filePath, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<TResult> DeleteAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<TResult> DeleteAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException();

    public string ToQueryString<T>(T obj, string? enumerableDelimiter = null) => throw new NotImplementedException();

    public Task<TResult?> QueryProjectAsync<TResult>(
        string route,
        ProjectionQueryReq request,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<TResult?> QueryConcreteAsync<TResult>(
        string route,
        QueryConcreteReq request,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
        => throw new NotImplementedException();
}
