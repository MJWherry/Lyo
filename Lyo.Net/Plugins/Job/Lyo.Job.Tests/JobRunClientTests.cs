using Lyo.Api.Models.Common.Request;
using Lyo.Common.Core.Identifiers;
using Lyo.Job.Client;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;

namespace Lyo.Job.Tests;

public class JobRunClientTests
{
    private readonly RecordingApiClient _api = new();
    private readonly JobRunClient _prefixedClient;
    private readonly JobRunClient _relativeClient;

    public JobRunClientTests()
    {
        _relativeClient = new(_api);
        _prefixedClient = new(_api, "https://localhost:5074");
    }

    [Fact]
    public async Task StartAsync_UsesRelativeRouteByDefault()
    {
        var runId = LyoGuid.CreateV7();
        await _relativeClient.StartAsync(runId, ["JobRunParameters"], TestCt);
        Assert.Equal($"Job/Run/{runId}/Started?include=JobRunParameters", _api.LastUri);
    }

    [Fact]
    public async Task StartAsync_UsesPrefixedRouteWhenConfigured()
    {
        var runId = LyoGuid.CreateV7();
        await _prefixedClient.StartAsync(runId, ct: TestCt);
        Assert.Equal($"https://localhost:5074/Job/Run/{runId}/Started", _api.LastUri);
    }

    [Fact]
    public async Task StartAsync_WhenRequestProvided_PostsStartedBody()
    {
        var runId = LyoGuid.CreateV7();
        var instanceId = LyoGuid.CreateV7();
        await _relativeClient.StartAsync(runId, request: new() { WorkerInstanceId = instanceId }, ct: TestCt);
        Assert.Equal($"Job/Run/{runId}/Started", _api.LastUri);
        var body = Assert.IsType<JobRunStartedReq>(_api.LastBody);
        Assert.Equal(instanceId, body.WorkerInstanceId);
    }

    [Fact]
    public async Task StartAsync_WhenMachineAndPidProvided_PostsThoseFields()
    {
        var runId = LyoGuid.CreateV7();
        await _relativeClient.StartAsync(runId, request: new() { MachineName = "box-a", ProcessId = 99 }, ct: TestCt);
        var body = Assert.IsType<JobRunStartedReq>(_api.LastBody);
        Assert.Equal("box-a", body.MachineName);
        Assert.Equal(99, body.ProcessId);
    }

    [Fact]
    public async Task FinishAsync_PostsToFinishedRoute()
    {
        var runId = LyoGuid.CreateV7();
        var results = new[] { new JobRunResultReq("Result", JobRunResult.Success) };
        await _relativeClient.FinishAsync(runId, results, TestCt);
        Assert.Equal($"Job/Run/{runId}/Finished", _api.LastUri);
        Assert.NotNull(_api.LastBody);
    }

    [Fact]
    public async Task CreateAsync_PostsToRunsCreateNotRunsRoot()
    {
        await _relativeClient.CreateAsync(new(LyoGuid.CreateV7(), "tester"), TestCt);
        Assert.Equal("Job/Run/Create", _api.LastUri);
        Assert.IsType<JobRunReq>(_api.LastBody);
    }

    [Fact]
    public async Task PatchProgressAsync_PatchesRunEntity()
    {
        var runId = LyoGuid.CreateV7();
        await _relativeClient.PatchProgressAsync(runId, 42, "halfway", TestCt);
        Assert.Equal($"Job/Run/{runId}", _api.LastUri);
        Assert.IsType<PatchRequest>(_api.LastBody);
    }
}