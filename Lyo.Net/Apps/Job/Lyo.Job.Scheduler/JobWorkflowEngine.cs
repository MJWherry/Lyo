using System.Text.Json;
using Lyo.Api.Client;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Events;
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
using Lyo.Exceptions;

namespace Lyo.Job.Scheduler;

/// <summary>Moves workflow runs forward when job runs complete, starting ready steps and applying failure policies.</summary>
public sealed class JobWorkflowEngine : BackgroundService
{
    private const string FinishedEventsQueue = "job.workflow.run.finished";

    /// <summary>Max retries for a failing completion message before it is dropped. Capped so a poison message cannot requeue forever.</summary>
    internal const int MaxRequeueCount = 5;

    private static readonly string[] WorkflowRunIncludes = ["JobWorkflowRunSteps", "JobWorkflowRunSteps.JobWorkflowStep", "JobWorkflow", "JobWorkflow.JobWorkflowSteps"];

    private readonly IApiClient _apiClient;
    private readonly IJobEventPublisher? _eventPublisher;
    private readonly ILogger<JobWorkflowEngine> _logger;
    private readonly IMqService _mqService;
    private readonly JobWorkflowEngineOptions _options;

    public JobWorkflowEngine(
        JobWorkflowEngineOptions options,
        IApiClient apiClient,
        IMqService mqService,
        ILogger<JobWorkflowEngine>? logger = null,
        IJobEventPublisher? eventPublisher = null)
    {
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNull(apiClient);
        ArgumentHelpers.ThrowIfNull(mqService);
        options.Validate();
        _options = options;
        _apiClient = apiClient;
        _mqService = mqService;
        _logger = logger ?? NullLogger<JobWorkflowEngine>.Instance;
        _eventPublisher = eventPublisher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_mqService.IsConnected())
            await _mqService.ConnectAsync(stoppingToken).ConfigureAwait(false);

        await _mqService.CreateQueue(FinishedEventsQueue, true, false, false, null, stoppingToken).ConfigureAwait(false);
        await _mqService.BindQueueToExchange(FinishedEventsQueue, Constants.Mq.JobEventExchange, Constants.Mq.JobRunFinishedRoutingKey, stoppingToken).ConfigureAwait(false);

        // Typed subscribe handles both enveloped retries (republished below) and the raw-Guid exchange messages.
        await _mqService.SubscribeToQueueAsync<Guid>(FinishedEventsQueue, OnRunFinishedAsync, ct: stoppingToken).ConfigureAwait(false);
        try {
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            // shutdown
        }
    }

    private async Task<bool> OnRunFinishedAsync(Guid runId, QueueMessageEnvelope<Guid>? envelope)
    {
        try {
            await ProcessRunCompletionAsync(runId).ConfigureAwait(false);
            return false;
        }
        catch (Exception ex) {
            // Bounded retry: republish with an incremented count instead of an unbounded broker requeue (which loops poison messages forever).
            var requeueCount = envelope?.RequeueCount ?? 0;
            if (requeueCount >= MaxRequeueCount) {
                _logger.LogError(ex, "Workflow engine giving up on run {RunId} after {Count} requeue(s)", runId, requeueCount);
                return false;
            }

            _logger.LogWarning(ex, "Workflow engine failed processing run {RunId}; requeue {Count}/{Max}", runId, requeueCount + 1, MaxRequeueCount);
            var retry = new QueueMessageEnvelope<Guid>(runId, requeueCount + 1, envelope?.MessageId ?? Guid.NewGuid().ToString("D"), envelope?.EnqueuedAt ?? DateTime.UtcNow);
            await _mqService.SendToQueue(FinishedEventsQueue, JsonSerializer.SerializeToUtf8Bytes(retry)).ConfigureAwait(false);
            return false; // ack the original; the republished envelope carries the retry count
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

        var stepState = IsSuccessOutcome(run.Result) ? JobWorkflowStepState.Finished : JobWorkflowStepState.Failed;
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
                BuildUri($"{Constants.Rest.Job.WorkflowRunSteps}/QueryConcrete"), new QueryConcreteReqBuilder().AddWhere(where).First().Build())
            .ConfigureAwait(false);

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
        var patch = PatchRequestBuilder.ForId(workflowRunId).SetProperty("State", state).SetProperty("FinishedTimestamp", DateTime.UtcNow).Build();
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
        if (!await ValidateDependencyGraphAsync(workflowRun, definitions).ConfigureAwait(false))
            return;

        var started = workflowRun.State == JobWorkflowRunState.Pending;
        foreach (var step in definitions.Where(s => s.Enabled).OrderBy(s => s.StepOrder)) {
            var runStep = runSteps.FirstOrDefault(rs => rs.JobWorkflowStepId == step.Id);
            if (runStep is null || runStep.State != JobWorkflowStepState.Pending)
                continue;

            if (!DependenciesSatisfied(step, runSteps, definitions))
                continue;

            // When a publisher is available, create the run with dispatch suppressed, link it to the run step, and only then dispatch.
            // Otherwise a fast worker could finish the run before the JobRunId patch lands and the completion handler would not find
            // the step. Without a publisher we fall back to immediate dispatch (bounded requeue above absorbs the race).
            var suppressDispatch = _eventPublisher is not null;
            var created = await CreateStepRunAsync(workflowRun.Id, runStep.Id, step, suppressDispatch).ConfigureAwait(false);
            if (created is null)
                continue;

            var patch = PatchRequestBuilder.ForId(runStep.Id).SetProperty("State", JobWorkflowStepState.Running).SetProperty("JobRunId", created.Id).Build();
            await _apiClient.PatchAsAsync<PatchRequest, object>(BuildUri($"{Constants.Rest.Job.WorkflowRunSteps}/{runStep.Id}"), patch).ConfigureAwait(false);
            if (suppressDispatch)
                await DispatchStepRunAsync(created).ConfigureAwait(false);

            started = true;
        }

        if (started && workflowRun.State == JobWorkflowRunState.Pending) {
            var patch = PatchRequestBuilder.ForId(workflowRun.Id).SetProperty("State", JobWorkflowRunState.Running).SetProperty("StartedTimestamp", DateTime.UtcNow).Build();
            await _apiClient.PatchAsAsync<PatchRequest, object>(BuildUri($"{Constants.Rest.Job.WorkflowRuns}/{workflowRun.Id}"), patch).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Fails the workflow run when its dependency graph cannot be executed — a cycle, or a <c>DependsOnStepIds</c> value that is not a GUID. Both are permanent configuration
    /// faults: without failing fast the run would sit <c>Running</c> with every step <c>Pending</c> forever.
    /// </summary>
    /// <returns>true when the graph is executable.</returns>
    private async Task<bool> ValidateDependencyGraphAsync(JobWorkflowRunRes workflowRun, IReadOnlyList<JobWorkflowStepRes> definitions)
    {
        var malformed = definitions.Where(s => !TryParseDependencies(s.DependsOnStepIds, out _)).Select(s => s.StepName).ToList();
        if (malformed.Count > 0) {
            _logger.LogError(
                "Workflow run {WorkflowRunId} has step(s) with malformed DependsOnStepIds: {Steps}. Failing the run — the configuration must be corrected", workflowRun.Id,
                string.Join(", ", malformed));

            await FailWorkflowForInvalidGraphAsync(workflowRun).ConfigureAwait(false);
            return false;
        }

        var cycle = FindDependencyCycle(definitions);
        if (cycle.Count == 0)
            return true;

        var names = definitions.Where(s => cycle.Contains(s.Id)).Select(s => s.StepName);
        _logger.LogError(
            "Workflow run {WorkflowRunId} has a circular step dependency involving: {Steps}. Failing the run — the configuration must be corrected", workflowRun.Id,
            string.Join(", ", names));

        await FailWorkflowForInvalidGraphAsync(workflowRun).ConfigureAwait(false);
        return false;
    }

    private async Task FailWorkflowForInvalidGraphAsync(JobWorkflowRunRes workflowRun)
    {
        await FinalizeWorkflowRunAsync(workflowRun.Id, JobWorkflowRunState.Failed).ConfigureAwait(false);
        await SkipPendingStepsAsync(workflowRun).ConfigureAwait(false);
    }

    private async Task<JobRunRes?> CreateStepRunAsync(Guid workflowRunId, Guid runStepId, JobWorkflowStepRes step, bool suppressDispatch)
    {
        var req = new JobRunReq {
            JobDefinitionId = step.JobDefinitionId,
            CreatedBy = _options.CreatedBy,
            AllowTriggers = false,
            SuppressDispatch = suppressDispatch,

            // Deduplicate step starts across concurrent sibling completions and redelivered completion messages: the same workflow run
            // step always maps to the same key, so a duplicate create resolves to the existing run instead of starting the step twice.
            IdempotencyKey = $"workflow:{workflowRunId:N}:{runStepId:N}"
        };

        var created = await _apiClient.PostAsAsync<JobRunReq, CreateResult<JobRunRes>>(BuildUri(Constants.Rest.Job.RunsCreate), req).ConfigureAwait(false);
        if (!created.IsSuccess || created.Data is null) {
            _logger.LogWarning("Failed to create workflow step run for step {StepName}", step.StepName);
            return null;
        }

        return created.Data;
    }

    /// <summary>Dispatches a step run that was created with dispatch suppressed. If publishing fails, the maintenance service's stuck-queued recovery picks the run up.</summary>
    private async Task DispatchStepRunAsync(JobRunRes run)
    {
        var workerType = run.JobDefinition?.WorkerType;
        if (_eventPublisher is null || workerType is null) {
            _logger.LogWarning("Cannot dispatch workflow step run {RunId} (no publisher or worker type); maintenance will redispatch it", run.Id);
            return;
        }

        try {
            await _eventPublisher.PublishRunCreatedAsync(run.Id, workerType, run.Priority).ConfigureAwait(false);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Failed to dispatch workflow step run {RunId}; maintenance will redispatch it", run.Id);
        }
    }

    /// <summary>
    /// Whether a run outcome lets a workflow step count as <see cref="JobWorkflowStepState.Finished" />. Anything else — including
    /// <see cref="JobRunResult.Timeout" /> and a missing result — marks the step failed, so a timed-out step cannot silently advance the DAG or finalize the workflow as
    /// successful.
    /// </summary>
    private static bool IsSuccessOutcome(JobRunResult? result)
        => result is JobRunResult.Success or JobRunResult.SuccessWithWarnings or JobRunResult.PartialSuccess or JobRunResult.Skipped;

    /// <summary>
    /// Parses a step's comma-separated <c>DependsOnStepIds</c>. Returns false when any entry is not a GUID: malformed configuration is a permanent condition, so the caller fails
    /// the workflow instead of letting the completion message bounce through the requeue path.
    /// </summary>
    private static bool TryParseDependencies(string? dependsOnStepIds, out HashSet<Guid> dependencyIds)
    {
        dependencyIds = [];
        if (string.IsNullOrWhiteSpace(dependsOnStepIds))
            return true;

        foreach (var raw in dependsOnStepIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
            if (!Guid.TryParse(raw, out var id))
                return false;

            dependencyIds.Add(id);
        }

        return true;
    }

    /// <summary>
    /// Whether every dependency of <paramref name="step" /> has reached a state that permits the step to start. A dependency in
    /// <see cref="JobWorkflowStepState.Failed" /> satisfies the dependency only when that dependency's own failure policy is
    /// <see cref="JobWorkflowFailurePolicy.Continue" /> — otherwise a <c>Continue</c> failure would leave every dependent step <c>Pending</c> forever and the workflow run would
    /// never finalize. <see cref="JobWorkflowStepState.Skipped" /> never satisfies a dependency: the upstream work did not run.
    /// </summary>
    private static bool DependenciesSatisfied(JobWorkflowStepRes step, IReadOnlyList<JobWorkflowRunStepRes> runSteps, IReadOnlyList<JobWorkflowStepRes> definitions)
    {
        if (!TryParseDependencies(step.DependsOnStepIds, out var depIds))
            return false;

        if (depIds.Count == 0)
            return true;

        return depIds.All(depId => {
            var dependencyRun = runSteps.FirstOrDefault(rs => rs.JobWorkflowStepId == depId);
            return dependencyRun?.State switch {
                JobWorkflowStepState.Finished => true,
                JobWorkflowStepState.Failed => definitions.FirstOrDefault(d => d.Id == depId)?.FailurePolicy == JobWorkflowFailurePolicy.Continue,
                _ => false
            };
        });
    }

    /// <summary>
    /// Returns the step ids that participate in a dependency cycle (including self-references), or an empty set when the graph is acyclic. Without this check a circular
    /// <c>DependsOnStepIds</c> configuration leaves every step <c>Pending</c> and the workflow run <c>Running</c> forever.
    /// </summary>
    internal static IReadOnlySet<Guid> FindDependencyCycle(IReadOnlyList<JobWorkflowStepRes> definitions)
    {
        var edges = new Dictionary<Guid, HashSet<Guid>>();
        foreach (var step in definitions) {
            // Unparseable dependencies are reported separately; treat them as no edges here.
            edges[step.Id] = TryParseDependencies(step.DependsOnStepIds, out var deps) ? deps : [];
        }

        var permanent = new HashSet<Guid>();
        var inStack = new HashSet<Guid>();
        var cycle = new HashSet<Guid>();

        bool Visit(Guid id)
        {
            if (permanent.Contains(id))
                return false;

            if (!inStack.Add(id)) {
                cycle.Add(id);
                return true;
            }

            if (edges.TryGetValue(id, out var deps)) {
                foreach (var dep in deps) {
                    if (!Visit(dep))
                        continue;

                    cycle.Add(id);
                    inStack.Remove(id);
                    return true;
                }
            }

            inStack.Remove(id);
            permanent.Add(id);
            return false;
        }

        foreach (var step in definitions)
            Visit(step.Id);

        return cycle;
    }

    private string BuildUri(string path)
    {
        var baseUrl = _options.ApiBaseUrl.TrimEnd('/');
        var p = path.TrimStart('/');
        return $"{baseUrl}/{p}";
    }
}