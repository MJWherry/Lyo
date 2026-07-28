using Lyo.Api.Client;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using JobRoutes = Lyo.Job.Models.Constants.Rest.Job;

namespace Lyo.Job.Client;

/// <summary>Worker instance registration and heartbeat endpoints for the job worker grid.</summary>
public sealed class JobWorkerInstanceClient(IApiClient client, string? routePrefix = null)
{
    private string Route => JobRouteBuilder.Build(routePrefix, JobRoutes.WorkerInstances);

    public Task<CreateResult<JobWorkerInstanceRes>> RegisterAsync(JobWorkerInstanceReq request, CancellationToken ct = default)
        => client.PostAsAsync<JobWorkerInstanceReq, CreateResult<JobWorkerInstanceRes>>(Route, request, ct: ct);

    public Task PatchAsync(PatchRequest patch, CancellationToken ct = default) => client.PatchAsAsync<PatchRequest, object>(Route, patch, ct: ct);

    public Task HeartbeatAsync(Guid instanceId, int inFlightCount, CancellationToken ct = default)
    {
        var patch = PatchRequestBuilder.ForId(instanceId).SetProperty("LastHeartbeatUtc", DateTime.UtcNow).SetProperty("InFlightCount", inFlightCount).Build();
        return PatchAsync(patch, ct);
    }

    public Task StopAsync(Guid instanceId, CancellationToken ct = default)
    {
        var patch = PatchRequestBuilder.ForId(instanceId)
            .SetProperty("State", JobWorkerInstanceState.Stopped)
            .SetProperty("InFlightCount", 0)
            .SetProperty("LastHeartbeatUtc", DateTime.UtcNow)
            .Build();

        return PatchAsync(patch, ct);
    }
}