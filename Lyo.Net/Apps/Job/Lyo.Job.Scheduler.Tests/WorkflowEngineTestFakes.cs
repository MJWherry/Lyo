using System.Text.Json;
using Lyo.Api.Client;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Common.Metadata.Records;
using Lyo.Http.Client;
using Lyo.Http.Client.Plan;
using Lyo.Http.Client.Session;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Query.Models.Common.Request;
using Constants = Lyo.Job.Models.Constants;

namespace Lyo.Job.Scheduler.Tests;

/// <summary>Every <see cref="IApiClient" /> member throws. Fakes derive from this and override only the calls the code under test makes, so an unexpected call fails
/// loudly.</summary>
internal abstract class StubApiClient : IApiClient
{
    public void Dispose() { }

    public JsonSerializerOptions GetSerializerOptions() => new();

    public HttpClient GetClient() => throw new NotImplementedException();

    public ILyoHttpSession Session { get; } = new LyoHttpSession();

    public LyoHttpClientHooks Hooks { get; } = new();

    public HttpClientPlanBuilder Get(string uri) => throw new NotImplementedException(uri);

    public HttpClientPlanBuilder Post(string uri) => throw new NotImplementedException(uri);

    public Task<HttpClientPlanRunResult> RunAsync(HttpClientPlan plan, HttpClientPlanRuntime? runtime = null, CancellationToken ct = default)
        => throw new NotImplementedException();

    public virtual Task<TResult> PostAsAsync<TRequest, TResult>(
        string uri,
        TRequest? request = default,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public virtual Task<TResult?> GetAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default) => throw new NotImplementedException(uri);

    public virtual Task<TResult> PatchAsAsync<TRequest, TResult>(
        string uri,
        TRequest? request = default,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public virtual Task<TResult?> GetAsAsync<TRequest, TResult>(
        string uri,
        TRequest? query = default,
        string? enumerableDelimiter = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public virtual Task<TResult> PostAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<TResult> PutAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(uri);

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
        => throw new NotImplementedException(uri);

    public Task<TResult> PostFileAsAsync<TResult>(string uri, Stream stream, string fileName, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<TResult> PostFileAsAsync<TResult>(
        string uri,
        byte[] data,
        FileTypeInfo fileType,
        string? fileName = null,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<TResult> PostFileAsAsync<TResult>(string uri, byte[] data, string fileName, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<TResult> PostFileAsAsync<TResult>(string uri, string filePath, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<TResult> DeleteAsAsync<TRequest, TResult>(string uri, TRequest? request = default, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        => throw new NotImplementedException(uri);

    public Task<TResult> DeleteAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default) => throw new NotImplementedException(uri);

    public string ToQueryString<T>(T obj, string? enumerableDelimiter = null) => throw new NotImplementedException();

    public Task<TResult?> QueryProjectAsync<TResult>(
        string route,
        ProjectionQueryReq request,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
        => throw new NotImplementedException(route);

    public Task<TResult?> QueryConcreteAsync<TResult>(
        string route,
        QueryConcreteReq request,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
        => throw new NotImplementedException(route);
}

/// <summary>
/// In-memory backing store for one workflow run. Patches are applied to the stored state, so a <c>GET</c> issued after a step patch observes the new step state exactly as the
/// real API would. which is what makes finalization assertions meaningful.
/// </summary>
internal sealed class FakeWorkflowApiClient : StubApiClient
{
    private readonly Dictionary<Guid, JobRunRes> _jobRuns = [];

    public FakeWorkflowApiClient(JobWorkflowRunRes workflowRun) => WorkflowRun = workflowRun;

    /// <summary>Current workflow run state, including patches applied through this client.</summary>
    public JobWorkflowRunRes WorkflowRun { get; private set; }

    public List<JobRunReq> CreatedRunRequests { get; } = [];

    public List<(Guid Id, PatchRequest Patch)> WorkflowRunPatches { get; } = [];

    public List<(Guid Id, PatchRequest Patch)> StepPatches { get; } = [];

    /// <summary>Registers the finished job run that <c>GET Job/Run/{id}</c> returns.</summary>
    public FakeWorkflowApiClient WithJobRun(JobRunRes run)
    {
        _jobRuns[run.Id] = run;
        return this;
    }

    public JobWorkflowStepState StepState(Guid workflowStepId) => WorkflowRun.RunSteps!.First(s => s.JobWorkflowStepId == workflowStepId).State;

    public override Task<TResult> PostAsAsync<TRequest, TResult>(
        string uri,
        TRequest? request = default,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
        where TRequest : default
    {
        if (uri.Contains($"{Constants.Rest.Job.WorkflowRunSteps}/QueryConcrete", StringComparison.OrdinalIgnoreCase)) {
            var runId = ExtractQueriedJobRunId(request);
            var matches = WorkflowRun.RunSteps!.Where(s => s.JobRunId == runId).ToList();
            return Task.FromResult((TResult)(object)BuildQueryRes(matches));
        }

        if (uri.Contains(Constants.Rest.Job.RunsCreate, StringComparison.OrdinalIgnoreCase)) {
            var runReq = (JobRunReq)(object)request!;
            CreatedRunRequests.Add(runReq);
            var created = new JobRunRes {
                Id = Guid.NewGuid(),
                JobDefinitionId = runReq.JobDefinitionId,
                State = JobState.Queued,
                CreatedTimestamp = DateTime.UtcNow
            };

            return Task.FromResult((TResult)(object)new CreateResult<JobRunRes>(true, created, null));
        }

        throw new NotImplementedException(uri);
    }

    public override Task<TResult?> GetAsAsync<TResult>(string uri, Action<HttpRequestMessage>? before = null, CancellationToken ct = default)
        where TResult : default
    {
        // The workflow routes are prefixes of each other, so the most specific one has to be tested first.
        if (uri.Contains(Constants.Rest.Job.WorkflowRuns, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult((TResult?)(object?)WorkflowRun);

        if (uri.Contains(Constants.Rest.Job.Runs, StringComparison.OrdinalIgnoreCase)) {
            var id = ExtractRouteId(uri);
            return Task.FromResult(_jobRuns.TryGetValue(id, out var run) ? (TResult?)(object?)run : default);
        }

        throw new NotImplementedException(uri);
    }

    public override Task<TResult> PatchAsAsync<TRequest, TResult>(
        string uri,
        TRequest? request = default,
        Action<HttpRequestMessage>? before = null,
        CancellationToken ct = default)
        where TRequest : default
    {
        var patch = (PatchRequest)(object)request!;
        var id = ExtractRouteId(uri);
        if (uri.Contains(Constants.Rest.Job.WorkflowRunSteps, StringComparison.OrdinalIgnoreCase)) {
            StepPatches.Add((id, patch));
            ApplyStepPatch(id, patch);
        }
        else {
            WorkflowRunPatches.Add((id, patch));
            if (patch.Properties.TryGetValue("State", out var state) && state is JobWorkflowRunState runState)
                WorkflowRun = WorkflowRun with { State = runState };
        }

        return Task.FromResult(default(TResult)!);
    }

    private void ApplyStepPatch(Guid runStepId, PatchRequest patch)
    {
        if (!patch.Properties.TryGetValue("State", out var raw) || raw is not JobWorkflowStepState state)
            return;

        var jobRunId = patch.Properties.TryGetValue("JobRunId", out var rawRunId) && rawRunId is Guid parsed ? parsed : (Guid?)null;
        WorkflowRun = WorkflowRun with {
            RunSteps = WorkflowRun.RunSteps!.Select(s => s.Id == runStepId ? s with { State = state, JobRunId = jobRunId ?? s.JobRunId } : s).ToList()
        };
    }

    private static Guid ExtractRouteId(string uri)
    {
        var lastSegment = uri.Split('?')[0].Split('/')[^1];
        return Guid.TryParse(lastSegment, out var id) ? id : Guid.Empty;
    }

    /// <summary>Reads the run id out of the engine's <c>JobRunId Equals</c> where clause without depending on the query builder's internal shape.</summary>
    private static Guid ExtractQueriedJobRunId<TRequest>(TRequest? request)
    {
        var json = JsonSerializer.Serialize(request);
        foreach (var candidate in json.Split(['"', ',', ':', '{', '}', '[', ']'], StringSplitOptions.RemoveEmptyEntries)) {
            if (Guid.TryParse(candidate, out var id))
                return id;
        }

        return Guid.Empty;
    }

    private static QueryRes<T> BuildQueryRes<T>(IReadOnlyList<T> items) => new(new(), true, items, 0, items.Count, items.Count, false, 0, null);
}
