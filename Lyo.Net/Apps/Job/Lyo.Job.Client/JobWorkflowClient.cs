using Lyo.Api.Client;
using Lyo.Api.Models.Common.Response;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using JobRoutes = Lyo.Job.Models.Constants.Rest.Job;

namespace Lyo.Job.Client;

/// <summary>Workflow start and workflow-run read calls.</summary>
public sealed class JobWorkflowClient(IApiClient client, string? routePrefix = null)
{
    private static readonly string[] RunIncludes = ["JobWorkflowRunSteps", "JobWorkflowRunSteps.JobWorkflowStep", "JobWorkflow", "JobWorkflow.JobWorkflowSteps"];

    /// <summary>
    /// Starts <paramref name="workflowId" />, returning the materialized run. Pass <paramref name="idempotencyKey" /> whenever the call may be retried (queue redelivery, UI
    /// double-click) so the retry resolves to the run the first attempt created instead of starting the workflow twice.
    /// </summary>
    public Task<CreateResult<JobWorkflowRunRes>> StartAsync(Guid workflowId, string? idempotencyKey = null, CancellationToken ct = default)
        => client.PostAsAsync<JobWorkflowStartReq, CreateResult<JobWorkflowRunRes>>(
            ApiRouteBuilder.Build(routePrefix, JobRoutes.WorkflowStart(workflowId)), new JobWorkflowStartReq { IdempotencyKey = idempotencyKey }, ct: ct);

    /// <summary>Reads one workflow run with its steps, their step definitions, and the owning workflow — the graph the engine works against.</summary>
    public Task<JobWorkflowRunRes?> GetRunAsync(Guid workflowRunId, CancellationToken ct = default)
        => client.GetAsAsync<JobWorkflowRunRes>(
            $"{ApiRouteBuilder.Build(routePrefix, JobRoutes.WorkflowRuns)}/{workflowRunId}?include={string.Join("&include=", RunIncludes)}", ct: ct);
}
