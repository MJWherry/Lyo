using System.Text.Json;
using Lyo.Api.Client;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Job.Models;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.MessageQueue;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Constants = Lyo.Job.Models.Constants;

namespace Lyo.Job.Scheduler;

/// <summary>Advances workflow runs when job runs complete, starting ready steps and applying failure policies.</summary>
public sealed class JobWorkflowEngine : BackgroundService
{
    private const string FinishedEventsQueue = "job.workflow.run.finished";

    private static readonly string[] WorkflowRunIncludes = [
        "JobWorkflowRunSteps", "JobWorkflowRunSteps.JobWorkflowStep", "JobWorkflow", "JobWorkflow.JobWorkflowSteps"
    ];

    private readonly IApiClient _apiClient;
    private readonly ILogger<JobWorkflowEngine> _logger;
    private readonly IMqService _mqService;
    private readonly JobWorkflowEngineOptions _options;

    public JobWorkflowEngine(
        JobWorkflowEngineOptions options,
        IApiClient apiClient,
        IMqService mqService,
        ILogger<JobWorkflowEngine>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(apiClient);
        ArgumentNullException.ThrowIfNull(mqService);
        options.Validate();
        _options = options;
        _apiClient = apiClient;
        _mqService = mqService;
        _logger = logger ?? NullLogger<JobWorkflowEngine>.Instance;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_mqService.IsConnected())
            await _mqService.ConnectAsync(stoppingToken).ConfigureAwait(false);

        await _mqService.CreateQueue(FinishedEventsQueue, true, false, false, null, stoppingToken).ConfigureAwait(false);
        await _mqService.BindQueueToExchange(FinishedEventsQueue, Constants.Mq.JobEventExchange, Constants.Mq.JobRunFinishedRoutingKey, stoppingToken)
            .ConfigureAwait(false);

        await _mqService.SubscribeToQueue(FinishedEventsQueue, OnRunFinishedAsync, stoppingToken).ConfigureAwait(false);

        try {
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            // shutdown
        }
    }

    private async Task<bool> OnRunFinishedAsync(byte[] body)
    {
        Guid? runId;
        try {
            runId = JsonSerializer.Deserialize<Guid>(body);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Could not parse workflow run-finished message");
            return false;
        }

        if (!runId.HasValue)
            return false;

        try {
            await ProcessRunCompletionAsync(runId.Value).ConfigureAwait(false);
            return false;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Workflow engine failed processing run {RunId}", runId);
            return true;
        }
    }

    internal async Task ProcessRunCompletionAsync(Guid runId)
    {
        var runStep = await FindWorkflowRunStepAsync(runId).ConfigureAwait(false);
        if (runStep is null)
            return;

        var run = await GetJobRunAsync(runId).ConfigureAwait(false);
        if (run is null)
            return;

        var stepState = run.Result is JobRunResult.Failure or JobRunResult.Cancelled or JobRunResult.Unknown
            ? JobWorkflowStepState.Failed
            : JobWorkflowStepState.Finished;

        await PatchWorkflowRunStepAsync(runStep.Id, stepState, runId).ConfigureAwait(false);

        var workflowRun = await GetWorkflowRunAsync(runStep.JobWorkflowRunId).ConfigureAwait(false);
        if (workflowRun?.RunSteps is null || workflowRun.JobWorkflow?.Steps is null)
            return;

        if (stepState == JobWorkflowStepState.Failed) {
            var failedStep = workflowRun.JobWorkflow.Steps.FirstOrDefault(s => s.Id == runStep.JobWorkflowStepId);
            if (failedStep?.FailurePolicy == JobWorkflowFailurePolicy.Stop) {
                await FinalizeWorkflowRunAsync(workflowRun.Id, JobWorkflowRunState.Failed).ConfigureAwait(false);
                await SkipPendingStepsAsync(workflowRun).ConfigureAwait(false);
                return;
            }
        }

        if (await TryFinalizeWorkflowRunAsync(workflowRun).ConfigureAwait(false))
            return;

        await StartReadyStepsAsync(workflowRun).ConfigureAwait(false);
    }

    private async Task<JobWorkflowRunStepRes?> FindWorkflowRunStepAsync(Guid jobRunId)
    {
        var where = WhereClauseBuilder.Condition("JobRunId", ComparisonOperatorEnum.Equals, jobRunId.ToString());
        var result = await _apiClient.PostAsAsync<QueryConcreteReq, QueryRes<JobWorkflowRunStepRes>>(
            BuildUri($"{Constants.Rest.Job.WorkflowRunSteps}/QueryConcrete"), new QueryConcreteReqBuilder().AddWhere(where).First().Build()).ConfigureAwait(false);

        return result.Items?.FirstOrDefault();
    }

    private async Task<JobRunRes?> GetJobRunAsync(Guid id)
    {
        var include = string.Join("&include=", "JobRunResults");
        return await _apiClient.GetAsAsync<JobRunRes>($"{BuildUri(Constants.Rest.Job.Runs)}/{id}?include={include}").ConfigureAwait(false);
    }

    private async Task<JobWorkflowRunRes?> GetWorkflowRunAsync(Guid id)
    {
        var include = string.Join("&include=", WorkflowRunIncludes);
        return await _apiClient.GetAsAsync<JobWorkflowRunRes>($"{BuildUri(Constants.Rest.Job.WorkflowRuns)}/{id}?include={include}").ConfigureAwait(false);
    }

    private async Task PatchWorkflowRunStepAsync(Guid stepId, JobWorkflowStepState state, Guid jobRunId)
    {
        var patch = PatchRequestBuilder.ForId(stepId).SetProperty("State", state).SetProperty("JobRunId", jobRunId).Build();
        await _apiClient.PatchAsAsync<PatchRequest, object>(BuildUri($"{Constants.Rest.Job.WorkflowRunSteps}/{stepId}"), patch).ConfigureAwait(false);
    }

    private async Task<bool> TryFinalizeWorkflowRunAsync(JobWorkflowRunRes workflowRun)
    {
        var steps = workflowRun.RunSteps ?? [];
        if (steps.Count == 0)
            return false;

        if (steps.Any(s => s.State is JobWorkflowStepState.Pending or JobWorkflowStepState.Running))
            return false;

        var finalState = steps.Any(s => s.State == JobWorkflowStepState.Failed) ? JobWorkflowRunState.Failed : JobWorkflowRunState.Finished;
        await FinalizeWorkflowRunAsync(workflowRun.Id, finalState).ConfigureAwait(false);
        return true;
    }

    private async Task FinalizeWorkflowRunAsync(Guid workflowRunId, JobWorkflowRunState state)
    {
        var patch = PatchRequestBuilder.ForId(workflowRunId)
            .SetProperty("State", state)
            .SetProperty("FinishedTimestamp", DateTime.UtcNow)
            .Build();

        await _apiClient.PatchAsAsync<PatchRequest, object>(BuildUri($"{Constants.Rest.Job.WorkflowRuns}/{workflowRunId}"), patch).ConfigureAwait(false);
    }

    private async Task SkipPendingStepsAsync(JobWorkflowRunRes workflowRun)
    {
        foreach (var step in workflowRun.RunSteps ?? []) {
            if (step.State != JobWorkflowStepState.Pending)
                continue;

            var patch = PatchRequestBuilder.ForId(step.Id).SetProperty("State", JobWorkflowStepState.Skipped).Build();
            await _apiClient.PatchAsAsync<PatchRequest, object>(BuildUri($"{Constants.Rest.Job.WorkflowRunSteps}/{step.Id}"), patch).ConfigureAwait(false);
        }
    }

    private async Task StartReadyStepsAsync(JobWorkflowRunRes workflowRun)
    {
        var runSteps = workflowRun.RunSteps ?? [];
        var definitions = workflowRun.JobWorkflow?.Steps ?? [];
        var started = workflowRun.State == JobWorkflowRunState.Pending;

        foreach (var step in definitions.Where(s => s.Enabled).OrderBy(s => s.StepOrder)) {
            var runStep = runSteps.FirstOrDefault(rs => rs.JobWorkflowStepId == step.Id);
            if (runStep is null || runStep.State != JobWorkflowStepState.Pending)
                continue;

            if (!DependenciesSatisfied(step, runSteps))
                continue;

            var created = await CreateStepRunAsync(step).ConfigureAwait(false);
            if (created is null)
                continue;

            var patch = PatchRequestBuilder.ForId(runStep.Id)
                .SetProperty("State", JobWorkflowStepState.Running)
                .SetProperty("JobRunId", created.Id)
                .Build();

            await _apiClient.PatchAsAsync<PatchRequest, object>(BuildUri($"{Constants.Rest.Job.WorkflowRunSteps}/{runStep.Id}"), patch).ConfigureAwait(false);
            started = true;
        }

        if (started && workflowRun.State == JobWorkflowRunState.Pending) {
            var patch = PatchRequestBuilder.ForId(workflowRun.Id)
                .SetProperty("State", JobWorkflowRunState.Running)
                .SetProperty("StartedTimestamp", DateTime.UtcNow)
                .Build();

            await _apiClient.PatchAsAsync<PatchRequest, object>(BuildUri($"{Constants.Rest.Job.WorkflowRuns}/{workflowRun.Id}"), patch).ConfigureAwait(false);
        }
    }

    private async Task<JobRunRes?> CreateStepRunAsync(JobWorkflowStepRes step)
    {
        var req = new JobRunReq {
            JobDefinitionId = step.JobDefinitionId,
            CreatedBy = _options.CreatedBy,
            AllowTriggers = false
        };

        var created = await _apiClient.PostAsAsync<JobRunReq, CreateResult<JobRunRes>>(BuildUri(Constants.Rest.Job.RunsCreate), req).ConfigureAwait(false);
        if (!created.IsSuccess || created.Data is null) {
            _logger.LogWarning("Failed to create workflow step run for step {StepName}", step.StepName);
            return null;
        }

        return created.Data;
    }

    private static bool DependenciesSatisfied(JobWorkflowStepRes step, IReadOnlyList<JobWorkflowRunStepRes> runSteps)
    {
        if (string.IsNullOrWhiteSpace(step.DependsOnStepIds))
            return true;

        var depIds = step.DependsOnStepIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(Guid.Parse)
            .ToHashSet();

        return depIds.All(depId => runSteps.Any(rs => rs.JobWorkflowStepId == depId && rs.State == JobWorkflowStepState.Finished));
    }

    private string BuildUri(string path)
    {
        var baseUrl = _options.ApiBaseUrl.TrimEnd('/');
        var p = path.TrimStart('/');
        return $"{baseUrl}/{p}";
    }
}
