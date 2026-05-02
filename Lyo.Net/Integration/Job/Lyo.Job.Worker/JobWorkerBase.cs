using System.Collections.Concurrent;
using Lyo.Api.Client;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Common.Request;
using Lyo.Common;
using Lyo.Job.Models;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Events;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.MessageQueue;
using Lyo.Metrics;
using Lyo.Result;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Constants = Lyo.Job.Models.Constants;

namespace Lyo.Job.Worker;

/// <summary>
/// Base class for all job workers. Handles the complete job lifecycle:
/// <list type="number">
///   <item>Receive a run ID from the worker queue.</item>
///   <item>Fetch the full <see cref="JobRunRes"/> from the Job API.</item>
///   <item>Mark the run as <c>Running</c> via <c>POST /Job/Run/{id}/Started</c>.</item>
///   <item>Subscribe to cancellation signals for this worker type.</item>
///   <item>Call the abstract <see cref="ExecuteAsync"/> with a rich context object.</item>
///   <item>Catch unhandled exceptions and mark the run as <c>Failure</c>.</item>
///   <item>Report results via <c>POST /Job/Run/{id}/Finished</c>.</item>
/// </list>
/// Subclasses only need to implement <see cref="ExecuteAsync"/>.
/// </summary>
public abstract class JobWorkerBase : QueueWorkerBase<Guid, Result<Unit>>
{
    private static readonly string[] RunIncludes =
        ["JobRunParameters", "JobRunResults", "JobSchedule", "JobTrigger", "JobDefinition", "JobDefinition.JobParameters"];

    private readonly IApiClient _apiClient;
    private readonly IJobEventPublisher _eventPublisher;
    private readonly string _apiBaseUrl;

    /// <summary>
    /// Per-run cancellation sources, keyed by run ID. Populated when a run starts so that a
    /// cancellation message from <see cref="IJobEventPublisher.SubscribeToRunCancellationsAsync"/>
    /// can cancel the correct token.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _runCancellationSources = new();

    /// <summary>Interval between heartbeat PATCH calls while a run is executing.</summary>
    protected virtual TimeSpan HeartbeatInterval => TimeSpan.FromSeconds(30);

    /// <summary>Worker type string — used to derive the input queue name and cancellation subscription queue.</summary>
    protected string WorkerType { get; }

    /// <param name="mqService">Message queue service (provides the input queue subscription).</param>
    /// <param name="apiClient">HTTP client used to call the Job API.</param>
    /// <param name="eventPublisher">Job event publisher used for cancellation subscription.</param>
    /// <param name="workerType">
    /// Worker type identifier. Must match the <c>WorkerType</c> on the <see cref="JobDefinition"/> entities
    /// this worker handles. Determines both the queue name (<c>job.run.{workerType}</c>) and the
    /// cancellation subscription queue.
    /// </param>
    /// <param name="apiBaseUrl">Base URL of the Job API (e.g. <c>https://api.example.com</c>).</param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="metrics">Optional metrics.</param>
    /// <param name="maxRequeueCount">Max requeue attempts before the message is routed to the DLQ.</param>
    /// <param name="dlqName">Dead-letter queue name. When null, messages exceeding the requeue limit are dropped.</param>
    protected JobWorkerBase(
        IMqService mqService,
        IApiClient apiClient,
        IJobEventPublisher eventPublisher,
        string workerType,
        string apiBaseUrl,
        ILogger? logger = null,
        IMetrics? metrics = null,
        int? maxRequeueCount = null,
        string? dlqName = null)
        : base(mqService, Constants.Mq.QueueGetJobRunCreated(workerType), logger, metrics, maxRequeueCount: maxRequeueCount, dlqName: dlqName)
    {
        _apiClient = apiClient;
        _eventPublisher = eventPublisher;
        _apiBaseUrl = apiBaseUrl.TrimEnd('/');
        WorkerType = workerType;
    }

    /// <inheritdoc />
    public override async Task StartAsync(CancellationToken ct = default)
    {
        await base.StartAsync(ct).ConfigureAwait(false);

        // Subscribe to cancellation signals for this worker type.
        await _eventPublisher.SubscribeToRunCancellationsAsync(WorkerType, OnCancelAsync, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async Task<Result<Unit>> DoWorkAsync(Guid runId, CancellationToken ct)
    {
        using var scope = Logger.BeginScope("JobRunId={JobRunId} WorkerType={WorkerType}", runId, WorkerType);

        // 1. Fetch full run.
        var run = await FetchRunAsync(runId, ct).ConfigureAwait(false);
        if (run is null) {
            Logger.LogError("Job run {RunId} not found — skipping", runId);
            return ResultVoid.Failure("Job run not found", "NotFound");
        }

        // 2. Mark as Running.
        var include = string.Join("&include=", RunIncludes);
        var startedRun = await _apiClient
            .PostAsAsync<JobRunRes>($"{_apiBaseUrl}/{Constants.Rest.Job.RunStarted(runId)}?include={include}", ct: ct)
            .ConfigureAwait(false);

        if (startedRun is null) {
            Logger.LogWarning("Failed to mark run {RunId} as started — it may have been cancelled or already processed", runId);
            return ResultVoid.Failure("Failed to start run", "StartFailed");
        }

        // 3. Create per-run linked CancellationTokenSource so cancellation signals can stop ExecuteAsync.
        using var runCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _runCancellationSources[runId] = runCts;

        var results = new JobWorkerResultBuilder();
        var ctx = new JobWorkerContextImpl(startedRun, Logger, runCts.Token, results);

        using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var heartbeatTask = RunHeartbeatAsync(runId, heartbeatCts.Token);

        try {
            Logger.LogInformation("Executing job run {RunId}", runId);
            await ExecuteAsync(ctx).ConfigureAwait(false);
            Logger.LogInformation("Job run {RunId} completed with outcome {Outcome}", runId, results.CurrentOutcome);
        }
        catch (OperationCanceledException) {
            Logger.LogInformation("Job run {RunId} was cancelled", runId);
            results.Cancel();
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Unhandled exception in job run {RunId}", runId);
            results.AddError(ex.Message);
        }
        finally {
            _runCancellationSources.TryRemove(runId, out _);
            await heartbeatCts.CancelAsync().ConfigureAwait(false);
            try { await heartbeatTask.ConfigureAwait(false); } catch { /* heartbeat task already cancelled */ }
        }

        // 4. Report results.
        await ReportFinishedAsync(runId, results.Build(), ct).ConfigureAwait(false);
        return ResultVoid.Success();
    }

    /// <summary>
    /// Implement this to perform the actual work. Use <paramref name="ctx"/> to read parameters,
    /// add results, check the cancellation token, and log messages.
    /// </summary>
    protected abstract Task ExecuteAsync(IJobWorkerContext ctx);

    private async Task<JobRunRes?> FetchRunAsync(Guid runId, CancellationToken ct)
    {
        try {
            var include = string.Join("&include=", RunIncludes);
            return await _apiClient.GetAsAsync<JobRunRes>($"{_apiBaseUrl}/{Constants.Rest.Job.Runs}/{runId}?include={include}", ct: ct).ConfigureAwait(false);
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Error fetching run {RunId}", runId);
            return null;
        }
    }

    private async Task RunHeartbeatAsync(Guid runId, CancellationToken ct)
    {
        using var timer = new PeriodicTimer(HeartbeatInterval);
        while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false)) {
            try {
                var patch = PatchRequestBuilder.ForId(runId).SetProperty("LastHeartbeatUtc", DateTime.UtcNow).Build();
                await _apiClient.PatchAsAsync<PatchRequest, object>(
                    $"{_apiBaseUrl}/{Constants.Rest.Job.Runs}/{runId}", patch, ct: ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                break;
            }
            catch (Exception ex) {
                Logger.LogWarning(ex, "Heartbeat failed for run {RunId}", runId);
            }
        }
    }

    private async Task ReportFinishedAsync(Guid runId, IReadOnlyList<JobRunResultReq> results, CancellationToken ct)
    {
        try {
            await _apiClient.PostAsAsync<IReadOnlyList<JobRunResultReq>, JobRunRes>(
                    $"{_apiBaseUrl}/{Constants.Rest.Job.RunFinished(runId)}", results, ct: ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Failed to report finish for run {RunId}", runId);
        }
    }

    private Task OnCancelAsync(Guid runId)
    {
        if (_runCancellationSources.TryGetValue(runId, out var cts)) {
            Logger.LogInformation("Cancelling job run {RunId} on worker request", runId);
            cts.Cancel();
        }

        return Task.CompletedTask;
    }

    private sealed class JobWorkerContextImpl(
        JobRunRes run,
        ILogger logger,
        CancellationToken cancellationToken,
        JobWorkerResultBuilder results) : IJobWorkerContext
    {
        public JobRunRes Run { get; } = run;
        public ILogger Logger { get; } = logger;
        public CancellationToken CancellationToken { get; } = cancellationToken;
        public JobWorkerResultBuilder Results { get; } = results;
    }
}
