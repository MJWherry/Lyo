using Lyo.Api.Models.Common.Request;
using Lyo.Job.Client;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;

namespace Lyo.Job.Tests;

public class JobWorkerInstanceClientTests
{
    [Fact]
    public async Task RegisterAsync_PostsToWorkerInstanceRoute()
    {
        var api = new RecordingApiClient();
        var client = new JobWorkerInstanceClient(api, "https://api.test");
        await client.RegisterAsync(
            new() {
                WorkerType = "cs",
                MachineName = "host",
                ProcessId = 1,
                State = JobWorkerInstanceState.Running,
                StartedTimestamp = DateTime.UtcNow,
                LastHeartbeatUtc = DateTime.UtcNow
            }, TestContext.Current.CancellationToken);

        Assert.Equal("https://api.test/Job/WorkerInstance", api.LastUri);
        Assert.IsType<JobWorkerInstanceReq>(api.LastBody);
    }

    [Fact]
    public async Task HeartbeatAsync_PatchesInstanceFields()
    {
        var api = new RecordingApiClient();
        var client = new JobWorkerInstanceClient(api);
        var id = Guid.NewGuid();
        await client.HeartbeatAsync(id, 3, TestContext.Current.CancellationToken);
        Assert.Equal("Job/WorkerInstance", api.LastUri);
        var patch = Assert.IsType<PatchRequest>(api.LastBody);
        Assert.NotNull(patch.Keys);
        Assert.Single(patch.Keys);
        Assert.Equal(id, patch.Keys[0][0]);
        Assert.False(patch.Properties.ContainsKey("MetadataJson"));
    }

    [Fact]
    public async Task HeartbeatAsync_WhenMetadataProvided_PatchesMetadataJson()
    {
        var api = new RecordingApiClient();
        var client = new JobWorkerInstanceClient(api);
        var id = Guid.NewGuid();
        await client.HeartbeatAsync(id, 1, TestContext.Current.CancellationToken, new Dictionary<string, string?> { ["workingSetBytes"] = "2048" });
        var patch = Assert.IsType<PatchRequest>(api.LastBody);
        Assert.True(patch.Properties.ContainsKey("MetadataJson"));
        Assert.Contains("workingSetBytes", Assert.IsType<string>(patch.Properties["MetadataJson"]), StringComparison.Ordinal);
    }
}

internal sealed class RecordingApiClient : StubApiClient
{
    public string? LastUri { get; private set; }

    public object? LastBody { get; private set; }

    public override Task<TResult?> GetAsAsync<TRequest, TResult>(
        string uri,
        TRequest? query = default,
        string? enumerableDelimiter = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
        where TRequest : default
        where TResult : default
    {
        LastUri = uri;
        return Task.FromResult(default(TResult));
    }

    public override Task<TResult?> GetAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        where TResult : default
    {
        LastUri = uri;
        return Task.FromResult(default(TResult));
    }

    public override Task<TResult> PatchAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        where TRequest : default
    {
        LastUri = uri;
        LastBody = request;
        return Task.FromResult(default(TResult)!);
    }

    public override Task<TResult> PostAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        where TRequest : default
    {
        LastUri = uri;
        LastBody = request;
        return Task.FromResult(default(TResult)!);
    }

    public override Task<TResult> PostAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
    {
        LastUri = uri;
        return Task.FromResult(default(TResult)!);
    }
}