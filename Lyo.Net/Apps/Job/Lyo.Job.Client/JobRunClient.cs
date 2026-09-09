using Lyo.Api.Client;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Query.Models.Common.Request;
using JobRoutes = Lyo.Job.Models.Constants.Rest.Job;

namespace Lyo.Job.Client;

/// <summary>Job run lifecycle, log, and progress calls.</summary>
public sealed class JobRunClient(IApiClient client, string? routePrefix = null)
{
    public Task<JobRunRes?> GetAsync(Guid runId, IEnumerable<string>? includes = null, CancellationToken ct = default)
        => client.GetAsAsync<JobRunRes>(ApiRouteBuilder.WithIncludes(ApiRouteBuilder.Build(routePrefix, $"{JobRoutes.Runs}/{runId}"), includes), ct: ct);

    public Task<CreateResult<JobRunRes>> CreateAsync(JobRunReq request, CancellationToken ct = default)
        => client.PostAsAsync<JobRunReq, CreateResult<JobRunRes>>(ApiRouteBuilder.Build(routePrefix, JobRoutes.RunsCreate), request, ct: ct);

    public Task<QueryRes<JobRunRes>> QueryAsync(QueryConcreteReq request, CancellationToken ct = default)
        => client.PostAsAsync<QueryConcreteReq, QueryRes<JobRunRes>>(ApiRouteBuilder.Build(routePrefix, JobRoutes.RunsQuery), request, ct: ct);

    /// <summary>
    /// Sets the run to <c>Running</c>. Pass <paramref name="request" /> so the API can snapshot the worker instance that claimed the run.
    /// Older callers may still send an empty body.
    /// </summary>
    public Task<JobRunRes> StartAsync(Guid runId, IEnumerable<string>? includes = null, CancellationToken ct = default, JobRunStartedReq? request = null)
        => request is null
            ? client.PostAsAsync<JobRunRes>(ApiRouteBuilder.WithIncludes(ApiRouteBuilder.Build(routePrefix, JobRoutes.RunStarted(runId)), includes), ct: ct)
            : client.PostAsAsync<JobRunStartedReq, JobRunRes>(
                ApiRouteBuilder.WithIncludes(ApiRouteBuilder.Build(routePrefix, JobRoutes.RunStarted(runId)), includes), request, ct: ct);

    public Task<JobRunRes> FinishAsync(Guid runId, IEnumerable<JobRunResultReq> results, CancellationToken ct = default)
        => client.PostAsAsync<IEnumerable<JobRunResultReq>, JobRunRes>(ApiRouteBuilder.Build(routePrefix, JobRoutes.RunFinished(runId)), results, ct: ct);

    public Task<JobRunRes> CancelAsync(Guid runId, CancellationToken ct = default)
        => client.PostAsAsync<JobRunRes>(ApiRouteBuilder.Build(routePrefix, $"{JobRoutes.Runs}/{runId}/Cancel"), ct: ct);

    /// <summary>Puts a <c>Running</c> run back to <c>Queued</c> (graceful worker shutdown). Fails if the run is not <c>Running</c> (for example <c>Cancelling</c>).</summary>
    public Task<JobRunRes> RequeueAsync(Guid runId, CancellationToken ct = default)
        => client.PostAsAsync<JobRunRes>(ApiRouteBuilder.Build(routePrefix, JobRoutes.RunRequeue(runId)), ct: ct);

    public Task<CreateResult<JobRunRes>> RerunAsync(Guid runId, CancellationToken ct = default)
        => client.PostAsAsync<CreateResult<JobRunRes>>(ApiRouteBuilder.Build(routePrefix, $"{JobRoutes.Runs}/{runId}/Rerun"), ct: ct);

    /// <summary>
    /// Republishes due <c>Queued</c> runs that are missing from the worker RabbitMQ queues. When <paramref name="definitionId" /> is set, only that definition's runs are
    /// included.
    /// </summary>
    public Task<JobRunResyncRes> ResyncQueuedAsync(Guid? definitionId = null, CancellationToken ct = default)
    {
        var route = ApiRouteBuilder.Build(routePrefix, JobRoutes.RunsResync);
        if (definitionId is { } id)
            route = $"{route}?definitionId={id}";

        return client.PostAsAsync<JobRunResyncRes>(route, ct: ct);
    }

    public Task<CreateResult<JobRunLogRes>> LogAsync(Guid runId, JobRunLogReq request, CancellationToken ct = default)
        => client.PostAsAsync<JobRunLogReq, CreateResult<JobRunLogRes>>(ApiRouteBuilder.Build(routePrefix, JobRoutes.RunLog(runId)), request, ct: ct);

    public Task<JobRunRes> HeartbeatAsync(Guid runId, JobRunHeartbeatReq? request = null, CancellationToken ct = default)
        => client.PatchAsAsync<JobRunHeartbeatReq, JobRunRes>(ApiRouteBuilder.Build(routePrefix, JobRoutes.RunHeartbeat(runId)), request, ct: ct);

    public Task PatchAsync(Guid runId, PatchRequest patch, CancellationToken ct = default)
        => client.PatchAsAsync<PatchRequest, object>(ApiRouteBuilder.Build(routePrefix, $"{JobRoutes.Runs}/{runId}"), patch, ct: ct);

    public Task PatchProgressAsync(Guid runId, int percent, string? message = null, CancellationToken ct = default)
    {
        var patch = PatchRequestBuilder.ForId(runId).SetProperty("ProgressPercent", percent).SetProperty("LastHeartbeatUtc", DateTime.UtcNow);
        if (message is not null)
            patch.SetProperty("ProgressMessage", message);

        return PatchAsync(runId, patch.Build(), ct);
    }

    public async Task<IReadOnlyList<JobRunRes>> CreateChildrenAsync(Guid parentRunId, JobCreateChildRunsReq request, CancellationToken ct = default)
    {
        var created = await client
            .PostAsAsync<JobCreateChildRunsReq, IReadOnlyList<JobRunRes>>(ApiRouteBuilder.Build(routePrefix, JobRoutes.RunChildren(parentRunId)), request, ct: ct)
            .ConfigureAwait(false);

        return created ?? [];
    }
}