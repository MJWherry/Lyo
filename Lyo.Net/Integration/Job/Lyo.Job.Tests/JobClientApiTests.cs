using System.Text.Json;
using Lyo.Api.Client;
using Lyo.Api.Models.Common.Request;
using Lyo.Common.Records;
using Lyo.Job.Client;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;

namespace Lyo.Job.Tests;

public class JobRunClientTests
{
    private readonly RecordingApiClient _api = new();
    private readonly JobRunClient _relativeClient;
    private readonly JobRunClient _prefixedClient;

    public JobRunClientTests()
    {
        _relativeClient = new JobRunClient(_api);
        _prefixedClient = new JobRunClient(_api, "https://localhost:5074");
    }

    [Fact]
    public async Task StartAsync_UsesRelativeRouteByDefault()
    {
        var runId = Guid.NewGuid();
        await _relativeClient.StartAsync(runId, ["JobRunParameters"]);

        Assert.Equal($"Job/Run/{runId}/Started?include=JobRunParameters", _api.LastUri);
    }

    [Fact]
    public async Task StartAsync_UsesPrefixedRouteWhenConfigured()
    {
        var runId = Guid.NewGuid();
        await _prefixedClient.StartAsync(runId);

        Assert.Equal($"https://localhost:5074/Job/Run/{runId}/Started", _api.LastUri);
    }

    [Fact]
    public async Task FinishAsync_PostsToFinishedRoute()
    {
        var runId = Guid.NewGuid();
        var results = new[] { new JobRunResultReq("Result", JobRunResult.Success) };

        await _relativeClient.FinishAsync(runId, results);

        Assert.Equal($"Job/Run/{runId}/Finished", _api.LastUri);
        Assert.NotNull(_api.LastBody);
    }

    [Fact]
    public async Task CreateAsync_PostsToRunsCreateNotRunsRoot()
    {
        await _relativeClient.CreateAsync(new JobRunReq(Guid.NewGuid(), "tester", false));

        Assert.Equal("Job/Run/Create", _api.LastUri);
        Assert.IsType<JobRunReq>(_api.LastBody);
    }

    [Fact]
    public async Task PatchProgressAsync_PatchesRunEntity()
    {
        var runId = Guid.NewGuid();
        await _relativeClient.PatchProgressAsync(runId, 42, "halfway");

        Assert.Equal($"Job/Run/{runId}", _api.LastUri);
        Assert.IsType<PatchRequest>(_api.LastBody);
    }
}

public class JobWorkerInstanceClientTests
{
    [Fact]
    public async Task RegisterAsync_PostsToWorkerInstanceRoute()
    {
        var api = new RecordingApiClient();
        var client = new JobWorkerInstanceClient(api, "https://api.test");

        await client.RegisterAsync(new JobWorkerInstanceReq {
            WorkerType = "cs",
            MachineName = "host",
            ProcessId = 1,
            State = JobWorkerInstanceState.Running,
            StartedTimestamp = DateTime.UtcNow,
            LastHeartbeatUtc = DateTime.UtcNow
        });

        Assert.Equal("https://api.test/Job/WorkerInstance", api.LastUri);
        Assert.IsType<JobWorkerInstanceReq>(api.LastBody);
    }

    [Fact]
    public async Task HeartbeatAsync_PatchesInstanceFields()
    {
        var api = new RecordingApiClient();
        var client = new JobWorkerInstanceClient(api);
        var id = Guid.NewGuid();

        await client.HeartbeatAsync(id, 3);

        Assert.Equal("Job/WorkerInstance", api.LastUri);
        var patch = Assert.IsType<PatchRequest>(api.LastBody);
        Assert.NotNull(patch.Keys);
        Assert.Single(patch.Keys);
        Assert.Equal(id, patch.Keys[0][0]);
    }
}

internal sealed class RecordingApiClient : IApiClient
{
    public string? LastUri { get; private set; }

    public object? LastBody { get; private set; }

    public void Dispose() { }

    public JsonSerializerOptions GetSerializerOptions() => new();

    public HttpClient GetClient() => new();

    public Task<TResult?> GetAsAsync<TRequest, TResult>(string uri, TRequest? query = default, string? enumerableDelimiter = null, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        LastUri = uri;
        return Task.FromResult(default(TResult));
    }

    public Task<TResult?> GetAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        LastUri = uri;
        return Task.FromResult(default(TResult));
    }

    public Task<byte[]> GetFileAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<(Stream Content, string? FileName, long? ContentLength)> GetFileStreamAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<(byte[] Content, FileTypeInfo FileType)> GetFileWithTypeAsync(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<TResult> PutAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException();

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

    public Task<byte[]> PostAsBinaryAsync<TRequest>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<TResult> PostFileAsAsync<TResult>(string uri, Stream stream, FileTypeInfo fileType, string? fileName = null, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<TResult> PostFileAsAsync<TResult>(string uri, Stream stream, string fileName, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<TResult> PostFileAsAsync<TResult>(string uri, byte[] data, FileTypeInfo fileType, string? fileName = null, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<TResult> PostFileAsAsync<TResult>(string uri, byte[] data, string fileName, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<TResult> PostFileAsAsync<TResult>(string uri, string filePath, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<TResult> DeleteAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<TResult> DeleteAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException();
}
