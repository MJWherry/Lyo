using System.Text.Json;
using Lyo.Api.Client;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Common.Core.Identifiers;
using Lyo.Exceptions;
using Lyo.Formatter;
using Lyo.Job.Models;
using Lyo.Job.Models.Context;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Events;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.MessageQueue;
using Lyo.Metrics;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common.Request;
using Lyo.Query.Models.Enums;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Constants = Lyo.Job.Models.Constants;

namespace Lyo.Job.Scheduler;

/// <summary>Moves workflow runs forward when job runs complete, starting ready steps and applying failure policies.</summary>
public sealed class JobWorkflowEngine : BackgroundService
{
    private const string FinishedEventsQueue = "job.workflow.run.finished";
    private const string CreatedEventsQueue = "job.workflow.run.created";

    /// <summary>Max retries for a failing completion message before it is dropped. Capped so a poison message cannot requeue forever.</summary>
    internal const int MaxRequeueCount = 5;

    private static readonly string[] WorkflowRunIncludes = ["JobWorkflowRunSteps", "JobWorkflowRunSteps.JobWorkflowStep", "JobWorkflow", "JobWorkflow.JobWorkflowSteps"];

    private readonly IApiClient _apiClient;
    private readonly IJobEventPublisher? _eventPublisher;
    private readonly ILogger<JobWorkflowEngine> _logger;
    private readonly IMetrics _metrics;
    private readonly IMqService _mqService;
    private readonly JobWorkflowEngineOptions _options;
    private readonly JobRunRequestFactory _requestFactory;

    public JobWorkflowEngine(
        JobWorkflowEngineOptions options,
        IApiClient apiClient,
        IMqService mqService,
        IFormatterService formatter,
        ILogger<JobWorkflowEngine>? logger = null,
        IJobEventPublisher? eventPublisher = null,
        IMetrics? metrics = null)
    {
        ArgumentHelpers.ThrowIfNull(options);
        ArgumentHelpers.ThrowIfNull(apiClient);
        ArgumentHelpers.ThrowIfNull(mqService);
        ArgumentHelpers.ThrowIfNull(formatter);
        options.Validate();
        _options = options;
        _apiClient = apiClient;
        _mqService = mqService;
        _logger = logger ?? NullLogger<JobWorkflowEngine>.Instance;
        _eventPublisher = eventPublisher;
        _metrics = metrics ?? NullMetrics.Instance;
        _requestFactory = new(formatter, options.CreatedBy, _logger);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_mqService.IsConnected())
            await _mqService.ConnectAsync(stoppingToken).ConfigureAwait(false);

        await _mqService.CreateQueue(FinishedEventsQueue, true, false, false, null, stoppingToken).ConfigureAwait(false);
        await _mqService.BindQueueToExchange(FinishedEventsQueue, Constants.Mq.JobEventExchange, Constants.Mq.JobRunFinishedRoutingKey, stoppingToken).ConfigureAwait(false);
        await _mqService.CreateQueue(CreatedEventsQueue, true, false, false, null, stoppingToken).ConfigureAwait(false);
        await _mqService.BindQueueToExchange(CreatedEventsQueue, Constants.Mq.JobEventExchange, Constants.Mq.JobWorkflowRunCreatedRoutingKey, stoppingToken).ConfigureAwait(false);

        // Typed subscribe handles both enveloped retries (republished below) and the raw-Guid exchange messages.
        await _mqService.SubscribeToQueueAsync<Guid>(FinishedEventsQueue, OnRunFinishedAsync, ct: stoppingToken).ConfigureAwait(false);
        await _mqService.SubscribeToQueueAsync<Guid>(CreatedEventsQueue, OnWorkflowRunCreatedAsync, ct: stoppingToken).ConfigureAwait(false);
        try {
            await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            // shutdown
        }
    }

    private Task<bool> OnRunFinishedAsync(Guid runId, QueueMessageEnvelope<Guid>? envelope)
        => HandleWithBoundedRequeueAsync(FinishedEventsQueue, runId, envelope, ProcessRunCompletionAsync, "job run completion");

    private Task<bool> OnWorkflowRunCreatedAsync(Guid workflowRunId, QueueMessageEnvelope<Guid>? envelope)
        => HandleWithBoundedRequeueAsync(CreatedEventsQueue, workflowRunId, envelope, ProcessWorkflowRunCreatedAsync, "workflow run creation");

    /// <summary>
    /// Runs <paramref name="process" /> and, when it throws, republishes the id with an incremented requeue count instead of letting the broker requeue it. An unbounded broker
    /// requeue loops a poison message forever; the count caps it at <see cref="MaxRequeueCount" />.
    /// </summary>
    /// <returns>Always false: the original delivery is acknowledged, and the republished envelope carries the retry.</returns>
    private async Task<bool> HandleWithBoundedRequeueAsync(string queue, Guid id, QueueMessageEnvelope<Guid>? envelope, Func<Guid, Task> process, string what)
    {
        try {
            await process(id).ConfigureAwait(false);
            return false;
        }
        catch (Exception ex) {
            var requeueCount = envelope?.RequeueCount ?? 0;
            if (requeueCount >= MaxRequeueCount) {
                _logger.LogError(ex, "Workflow engine giving up on {What} {Id} after {Count} requeue(s)", what, id, requeueCount);
                return false;
            }

            _logger.LogWarning(ex, "Workflow engine failed processing {What} {Id}; requeue {Count}/{Max}", what, id, requeueCount + 1, MaxRequeueCount);
            var retry = new QueueMessageEnvelope<Guid>(id, requeueCount + 1, envelope?.MessageId ?? LyoGuid.CreateV7().ToString("D"), envelope?.EnqueuedAt ?? DateTime.UtcNow);
            await _mqService.SendToQueue(queue, JsonSerializer.SerializeToUtf8Bytes(retry)).ConfigureAwait(false);
            return false;
        }
    }

    /// <summary>
    /// Dispatches the root steps of a freshly materialized workflow run. A run is created with every step <c>Pending</c> and nothing has completed yet, so this is the only thing
    /// that gets a workflow moving. Runs that are no longer <c>Pending</c> are ignored, which makes a redelivered created-event harmless.
    /// </summary>
    internal async Task ProcessWorkflowRunCreatedAsync(Guid workflowRunId)
    {
        var workflowRun = await GetWorkflowRunAsync(workflowRunId).ConfigureAwait(false);
        if (workflowRun?.RunSteps is null || workflowRun.JobWorkflow?.Steps is null || workflowRun.State != JobWorkflowRunState.Pending)
            return;

        await StartReadyStepsAsync(workflowRun).ConfigureAwait(false);
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
            _metrics.IncrementCounter(Constants.Metrics.Workflow.StepsFailed);
            var failedStep = workflowRun.JobWorkflow.Steps.FirstOrDefault(s => s.Id == runStep.JobWorkflowStepId);
            await PublishStepFailureAlertAsync(workflowRun, failedStep, run).ConfigureAwait(false);
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

    /// <summary>
    /// Finalizes the run once no step can still make progress. <see cref="JobWorkflowStepState.ConditionNotMet" /> is terminal-success: a workflow whose tail was gated off did
    /// exactly what it was configured to do.
    /// </summary>
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
        _metrics.IncrementCounter(state == JobWorkflowRunState.Failed ? Constants.Metrics.Workflow.RunsFailed : Constants.Metrics.Workflow.RunsFinished);
    }

    /// <summary>
    /// Raises <see cref="JobAlertType.Failure" /> for a failed step, attributed to the step's own definition and run so the existing consumer routing still applies. This is the
    /// only producer of that alert type: <c>JobDefinition.AlertOnFailure</c> was removed when workflows took over failure notification.
    /// </summary>
    private async Task PublishStepFailureAlertAsync(JobWorkflowRunRes workflowRun, JobWorkflowStepRes? failedStep, JobRunRes run)
    {
        if (_eventPublisher is null || !_eventPublisher.IsConnected())
            return;

        var workflowName = workflowRun.JobWorkflow?.Name ?? workflowRun.JobWorkflowId.ToString();
        var stepName = failedStep?.StepName ?? "step";
        try {
            await _eventPublisher.PublishAlertAsync(run.JobDefinitionId, run.Id, JobAlertType.Failure, $"Workflow '{workflowName}' step '{stepName}' failed (run {run.Id})")
                .ConfigureAwait(false);
        }
        catch (Exception ex) {
            // The alert is advisory; losing it must not stop the run from being finalized and its dependents from being resolved.
            _logger.LogError(ex, "Failed to publish failure alert for workflow run {WorkflowRunId} step {StepName}", workflowRun.Id, stepName);
        }
    }

    private async Task SkipPendingStepsAsync(JobWorkflowRunRes workflowRun)
    {
        foreach (var step in workflowRun.RunSteps ?? []) {
            if (step.State != JobWorkflowStepState.Pending)
                continue;

            var patch = PatchRequestBuilder.ForId(step.Id).SetProperty("State", JobWorkflowStepState.Skipped).Build();
            await _apiClient.PatchAsAsync<PatchRequest, object>(BuildUri($"{Constants.Rest.Job.WorkflowRunSteps}/{step.Id}"), patch).ConfigureAwait(false);
            _metrics.IncrementCounter(Constants.Metrics.Workflow.StepsSkipped);
        }
    }

    private async Task StartReadyStepsAsync(JobWorkflowRunRes workflowRun)
    {
        // Mutable local view: conditions are resolved to a fixpoint before anything starts, and the finalization check at the bottom has to see those marks.
        var runSteps = (workflowRun.RunSteps ?? []).ToList();
        var definitions = workflowRun.JobWorkflow?.Steps ?? [];
        if (!await ValidateDependencyGraphAsync(workflowRun, definitions).ConfigureAwait(false))
            return;

        var pending = definitions.Where(s => s.Enabled && runSteps.Any(rs => rs.JobWorkflowStepId == s.Id && rs.State == JobWorkflowStepState.Pending)).ToList();

        // Built once for the whole batch: the upstream runs and definitions are fetched in one query each rather than per step, and every step scope is a copy of this bag.
        // Inputs come off the run rather than the schedule, so a gate reads the values the start froze instead of whatever the schedule says now.
        var runScope = new JobContextBag().WithWorkflow(workflowRun.JobWorkflow, workflowRun).WithInputs(workflowRun.InputsJson);
        var upstreamRuns = await LoadUpstreamRunsAsync(pending, runSteps).ConfigureAwait(false);

        // Scopes are built for every pending step, not only the ones already cleared to start: a gate may read the step's own definition or an upstream result, and the
        // condition pass below runs before anything is ready.
        var stepDefinitions = await LoadStepDefinitionsAsync(pending).ConfigureAwait(false);
        var scopes = pending.ToDictionary(s => s.Id, s => BuildStepScope(runScope, s, definitions, runSteps, upstreamRuns, stepDefinitions));
        await ApplyConditionsAsync(definitions, runSteps, scopes).ConfigureAwait(false);
        var ready = definitions.Where(s => s.Enabled)
            .OrderBy(s => s.StepOrder)
            .Select(s => (Step: s, RunStep: runSteps.FirstOrDefault(rs => rs.JobWorkflowStepId == s.Id)))
            .Where(p => p.RunStep?.State == JobWorkflowStepState.Pending && DependenciesSatisfied(p.Step, runSteps, definitions))
            .ToList();

        var started = workflowRun.State == JobWorkflowRunState.Pending;
        foreach (var (step, runStep) in ready) {
            var stepScope = scopes.TryGetValue(step.Id, out var existing) ? existing : BuildStepScope(runScope, step, definitions, runSteps, upstreamRuns, stepDefinitions);

            // When a publisher is available, create the run with dispatch suppressed, link it to the run step, and only then dispatch.
            // Otherwise a fast worker could finish the run before the JobRunId patch lands and the completion handler would not find
            // the step. Without a publisher we fall back to immediate dispatch (bounded requeue above absorbs the race).
            var suppressDispatch = _eventPublisher is not null;
            var created = await CreateStepRunAsync(workflowRun.Id, runStep!.Id, step, stepDefinitions.GetValueOrDefault(step.JobDefinitionId), stepScope, suppressDispatch)
                .ConfigureAwait(false);

            if (created is null)
                continue;

            var patch = PatchRequestBuilder.ForId(runStep.Id).SetProperty("State", JobWorkflowStepState.Running).SetProperty("JobRunId", created.Id).Build();
            await _apiClient.PatchAsAsync<PatchRequest, object>(BuildUri($"{Constants.Rest.Job.WorkflowRunSteps}/{runStep.Id}"), patch).ConfigureAwait(false);
            if (suppressDispatch)
                await DispatchStepRunAsync(created).ConfigureAwait(false);

            ReplaceRunStep(runSteps, runStep.Id, JobWorkflowStepState.Running);
            _metrics.IncrementCounter(Constants.Metrics.Workflow.StepsStarted);
            started = true;
        }

        if (started && workflowRun.State == JobWorkflowRunState.Pending) {
            var patch = PatchRequestBuilder.ForId(workflowRun.Id).SetProperty("State", JobWorkflowRunState.Running).SetProperty("StartedTimestamp", DateTime.UtcNow).Build();
            await _apiClient.PatchAsAsync<PatchRequest, object>(BuildUri($"{Constants.Rest.Job.WorkflowRuns}/{workflowRun.Id}"), patch).ConfigureAwait(false);
            _metrics.IncrementCounter(Constants.Metrics.Workflow.RunsStarted);
        }

        // A pass that only marked steps ConditionNotMet starts nothing, so no future completion will arrive to finalize the run. Without this it would sit Running forever.
        await TryFinalizeWorkflowRunAsync(workflowRun with { RunSteps = runSteps }).ConfigureAwait(false);
    }

    /// <summary>
    /// The context one step's gate and parameter templates read: the run-level seeds, the step itself, its definition, and each dependency's completed run keyed by that
    /// dependency's step name. A copy per step, because sibling steps of a DAG run concurrently and must not see each other's upstream seeds.
    /// </summary>
    internal static JobContextBag BuildStepScope(
        JobContextBag runScope,
        JobWorkflowStepRes step,
        IReadOnlyList<JobWorkflowStepRes> definitions,
        IReadOnlyList<JobWorkflowRunStepRes> runSteps,
        IReadOnlyDictionary<Guid, JobRunRes> upstreamRuns,
        IReadOnlyDictionary<Guid, JobDefinitionRes> stepDefinitions)
        => runScope.WithWorkflowStep(step, stepDefinitions.GetValueOrDefault(step.JobDefinitionId), ResolveUpstream(step, definitions, runSteps, upstreamRuns));

    /// <summary>Each dependency paired with its completed run. A dependency whose run is not loaded yet is left out rather than seeded null, so its paths simply do not resolve.</summary>
    private static List<(string StepName, JobRunRes? Run)> ResolveUpstream(
        JobWorkflowStepRes step,
        IReadOnlyList<JobWorkflowStepRes> definitions,
        IReadOnlyList<JobWorkflowRunStepRes> runSteps,
        IReadOnlyDictionary<Guid, JobRunRes> upstreamRuns)
    {
        JobWorkflowGraph.TryParseDependencies(step.DependsOnStepIds, out var dependencyIds);
        var upstream = new List<(string StepName, JobRunRes? Run)>();
        foreach (var dependencyId in dependencyIds) {
            var dependency = definitions.FirstOrDefault(d => d.Id == dependencyId);
            var dependencyRunId = runSteps.FirstOrDefault(rs => rs.JobWorkflowStepId == dependencyId)?.JobRunId;
            if (dependency is not null && dependencyRunId is not null && upstreamRuns.TryGetValue(dependencyRunId.Value, out var dependencyRun))
                upstream.Add((dependency.StepName, dependencyRun));
        }

        return upstream;
    }

    /// <summary>
    /// Marks every pending step whose gate does not open as <see cref="JobWorkflowStepState.ConditionNotMet" />, repeating until nothing changes. A step is gated either by its
    /// own condition or by a dependency that itself never ran — the second case is what makes the skip cascade down the graph instead of deadlocking its tail.
    /// </summary>
    private async Task ApplyConditionsAsync(IReadOnlyList<JobWorkflowStepRes> definitions, List<JobWorkflowRunStepRes> runSteps, IReadOnlyDictionary<Guid, JobContextBag> scopes)
    {
        var changed = true;
        while (changed) {
            changed = false;
            foreach (var step in definitions.Where(s => s.Enabled).OrderBy(s => s.StepOrder)) {
                var runStep = runSteps.FirstOrDefault(rs => rs.JobWorkflowStepId == step.Id);
                if (runStep?.State != JobWorkflowStepState.Pending || !DependenciesSatisfied(step, runSteps, definitions))
                    continue;

                if (!IsGated(step, runSteps, scopes))
                    continue;

                var patch = PatchRequestBuilder.ForId(runStep.Id).SetProperty("State", JobWorkflowStepState.ConditionNotMet).Build();
                await _apiClient.PatchAsAsync<PatchRequest, object>(BuildUri($"{Constants.Rest.Job.WorkflowRunSteps}/{runStep.Id}"), patch).ConfigureAwait(false);
                ReplaceRunStep(runSteps, runStep.Id, JobWorkflowStepState.ConditionNotMet);
                _metrics.IncrementCounter(Constants.Metrics.Workflow.StepsSkipped);
                changed = true;
            }
        }
    }

    /// <summary>Whether <paramref name="step" /> must not run: a dependency of it never ran, or its own condition does not hold against the step's context.</summary>
    private static bool IsGated(JobWorkflowStepRes step, IReadOnlyList<JobWorkflowRunStepRes> runSteps, IReadOnlyDictionary<Guid, JobContextBag> scopes)
    {
        JobWorkflowGraph.TryParseDependencies(step.DependsOnStepIds, out var dependencyIds);
        if (dependencyIds.Any(id => runSteps.Any(rs => rs.JobWorkflowStepId == id && rs.State == JobWorkflowStepState.ConditionNotMet)))
            return true;

        // No scope means the step was not pending when the batch was built, so there is nothing to gate.
        return scopes.TryGetValue(step.Id, out var scope) && JobWorkflowConditionEvaluator.Evaluate(step, scope) == JobWorkflowConditionOutcome.Skip;
    }

    private static void ReplaceRunStep(List<JobWorkflowRunStepRes> runSteps, Guid runStepId, JobWorkflowStepState state)
    {
        var index = runSteps.FindIndex(rs => rs.Id == runStepId);
        if (index >= 0)
            runSteps[index] = runSteps[index] with { State = state };
    }

    /// <summary>
    /// Fails the workflow run when its dependency graph cannot be executed — a cycle, or a <c>DependsOnStepIds</c> value that is not a GUID. Both are permanent configuration
    /// faults: without failing fast the run would sit <c>Running</c> with every step <c>Pending</c> forever.
    /// </summary>
    /// <returns>true when the graph is executable.</returns>
    private async Task<bool> ValidateDependencyGraphAsync(JobWorkflowRunRes workflowRun, IReadOnlyList<JobWorkflowStepRes> definitions)
    {
        var malformed = definitions.Where(s => !JobWorkflowGraph.TryParseDependencies(s.DependsOnStepIds, out var _)).Select(s => s.StepName).ToList();
        if (malformed.Count > 0) {
            _logger.LogError(
                "Workflow run {WorkflowRunId} has step(s) with malformed DependsOnStepIds: {Steps}. Failing the run — the configuration must be corrected", workflowRun.Id,
                string.Join(", ", malformed));

            await FailWorkflowForInvalidGraphAsync(workflowRun).ConfigureAwait(false);
            return false;
        }

        var cycle = JobWorkflowGraph.FindDependencyCycle(definitions);
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
        _metrics.IncrementCounter(Constants.Metrics.Workflow.GraphInvalid);
        await FinalizeWorkflowRunAsync(workflowRun.Id, JobWorkflowRunState.Failed).ConfigureAwait(false);
        await SkipPendingStepsAsync(workflowRun).ConfigureAwait(false);
    }

    /// <summary>
    /// Fetches, in one query keyed by run id, the completed runs that <paramref name="steps" /> read from: their dependencies, for both result templates and the
    /// <c>Step.{name}.Result.*</c> paths their gates test. Only steps that already produced a run are fetched.
    /// </summary>
    private async Task<IReadOnlyDictionary<Guid, JobRunRes>> LoadUpstreamRunsAsync(IEnumerable<JobWorkflowStepRes> steps, IReadOnlyList<JobWorkflowRunStepRes> runSteps)
    {
        var sourceStepIds = new HashSet<Guid>();
        foreach (var step in steps) {
            JobWorkflowGraph.TryParseDependencies(step.DependsOnStepIds, out var ids);
            sourceStepIds.UnionWith(ids);
        }

        var runIds = runSteps.Where(rs => sourceStepIds.Contains(rs.JobWorkflowStepId) && rs.JobRunId is not null).Select(rs => rs.JobRunId!.Value).Distinct().ToArray();
        if (runIds.Length == 0)
            return new Dictionary<Guid, JobRunRes>();

        var query = new QueryConcreteReqBuilder().AddIncludes("JobRunResults").AddWhere(w => w.In("Id", runIds)).SetPagination(0, runIds.Length).Build();
        var result = await _apiClient.PostAsAsync<QueryConcreteReq, QueryRes<JobRunRes>>(BuildUri($"{Constants.Rest.Job.Runs}/QueryConcrete"), query).ConfigureAwait(false);
        return (result.Items ?? []).ToDictionary(r => r.Id);
    }

    /// <summary>Fetches the job definitions the ready steps run, with their parameters, in one query. Parameter merging needs each declared key's type and default.</summary>
    private async Task<IReadOnlyDictionary<Guid, JobDefinitionRes>> LoadStepDefinitionsAsync(IEnumerable<JobWorkflowStepRes> steps)
    {
        var definitionIds = steps.Select(s => s.JobDefinitionId).Distinct().ToArray();
        if (definitionIds.Length == 0)
            return new Dictionary<Guid, JobDefinitionRes>();

        var query = new QueryConcreteReqBuilder().AddIncludes("JobParameters").AddWhere(w => w.In("Id", definitionIds)).SetPagination(0, definitionIds.Length).Build();
        var result = await _apiClient.PostAsAsync<QueryConcreteReq, QueryRes<JobDefinitionRes>>(BuildUri($"{Constants.Rest.Job.Definitions}/QueryConcrete"), query)
            .ConfigureAwait(false);

        return (result.Items ?? []).ToDictionary(d => d.Id);
    }

    private async Task<JobRunRes?> CreateStepRunAsync(
        Guid workflowRunId,
        Guid runStepId,
        JobWorkflowStepRes step,
        JobDefinitionRes? definition,
        JobContextBag stepScope,
        bool suppressDispatch)
    {
        // Without the definition there is nothing to merge the step's ParametersJson against, so the run starts bare rather than not at all.
        if (definition is null)
            _logger.LogWarning("Definition {DefinitionId} for workflow step {StepName} could not be loaded; the step runs without parameters", step.JobDefinitionId, step.StepName);

        var req = definition is null
            ? new() { JobDefinitionId = step.JobDefinitionId, CreatedBy = _options.CreatedBy, JobRunParameters = [] }
            : _requestFactory.BuildForWorkflowStep(definition, step, stepScope);

        req.SuppressDispatch = suppressDispatch;

        // Deduplicate step starts across concurrent sibling completions and redelivered completion messages: the same workflow run
        // step always maps to the same key, so a duplicate create resolves to the existing run instead of starting the step twice.
        req.IdempotencyKey = $"workflow:{workflowRunId:N}:{runStepId:N}";
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
    /// Whether a run outcome lets a workflow step count as <see cref="JobWorkflowStepState.Finished" />. Anything else — including <see cref="JobRunResult.Timeout" /> and a
    /// missing result — marks the step failed, so a timed-out step cannot silently advance the DAG or finalize the workflow as successful.
    /// </summary>
    private static bool IsSuccessOutcome(JobRunResult? result)
        => result is JobRunResult.Success or JobRunResult.SuccessWithWarnings or JobRunResult.PartialSuccess or JobRunResult.Skipped;

    /// <summary>
    /// Whether every dependency of <paramref name="step" /> has reached a state that permits the step to start. A dependency in <see cref="JobWorkflowStepState.Failed" />
    /// satisfies the dependency only when that dependency's own failure policy is <see cref="JobWorkflowFailurePolicy.Continue" /> — otherwise a <c>Continue</c> failure would leave every
    /// dependent step <c>Pending</c> forever and the workflow run would never finalize. <see cref="JobWorkflowStepState.ConditionNotMet" /> satisfies it too, but only so the dependent is
    /// reachable: the condition pass then marks that dependent <c>ConditionNotMet</c> as well rather than running it. <see cref="JobWorkflowStepState.Skipped" /> never satisfies a
    /// dependency: the upstream work did not run.
    /// </summary>
    private static bool DependenciesSatisfied(JobWorkflowStepRes step, IReadOnlyList<JobWorkflowRunStepRes> runSteps, IReadOnlyList<JobWorkflowStepRes> definitions)
    {
        if (!JobWorkflowGraph.TryParseDependencies(step.DependsOnStepIds, out var depIds))
            return false;

        if (depIds.Count == 0)
            return true;

        return depIds.All(depId => {
            var dependencyRun = runSteps.FirstOrDefault(rs => rs.JobWorkflowStepId == depId);
            return dependencyRun?.State switch {
                JobWorkflowStepState.Finished or JobWorkflowStepState.ConditionNotMet => true,
                JobWorkflowStepState.Failed => definitions.FirstOrDefault(d => d.Id == depId)?.FailurePolicy == JobWorkflowFailurePolicy.Continue,
                var _ => false
            };
        });
    }

    private string BuildUri(string path)
    {
        var baseUrl = _options.ApiBaseUrl.TrimEnd('/');
        var p = path.TrimStart('/');
        return $"{baseUrl}/{p}";
    }
}