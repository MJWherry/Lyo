using System.Collections.Concurrent;
using System.Diagnostics;
using Lyo.Api.Client;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Common.Request;
using Lyo.Exceptions;
using Lyo.Job.Client;
using Lyo.Job.Models;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Events;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Job.Models.Security;
using Lyo.MessageQueue;
using Lyo.Metrics;
using Lyo.Result;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Constants = Lyo.Job.Models.Constants;

namespace Lyo.Job.Worker;

/// <summary>
/// Base class for all job workers. Handles the complete job lifecycle:
/// <list type="number">
/// <item>Receive a run ID from the worker queue.</item> <item>Fetch the full <see cref="JobRunRes" /> from the Job API.</item>
/// <item>Mark the run as <c>Running</c> via <c>POST /Job/Run/{id}/Started</c>.</item> <item>Subscribe to cancellation signals for this worker type.</item>
/// <item>Call the abstract <see cref="ExecuteAsync" /> with a rich context object.</item> <item>Catch unhandled exceptions and mark the run as <c>Failure</c>.</item>
/// <item>Report results via <c>POST /Job/Run/{id}/Finished</c>.</item>
/// </list>
/// Subclasses only need to implement <see cref="ExecuteAsync" />.
/// </summary>
public abstract class JobWorkerBase : QueueWorkerBase<Guid, Result<Unit>>, IHostedService
{
    private static readonly string[] RunIncludes = ["JobRunParameters", "JobRunResults", "JobSchedule", "JobTrigger", "JobDefinition", "JobDefinition.JobParameters"];

    /// <summary>Result metadata that tells <see cref="QueueWorkerBase{TRequest,TResult}" /> not to requeue the message even though the result is a failure.</summary>
    private static readonly IReadOnlyDictionary<string, object> NoRequeueMetadata = new Dictionary<string, object> { ["requeue"] = false };

    private readonly IJobClient _jobClient;
    private readonly IJobEventPublisher _eventPublisher;
    private readonly IJobParameterEncryptionService? _parameterEncryption;

    /// <summary>
    /// Per-run cancellation sources, keyed by run ID. Populated when a run starts so that a cancellation message from
    /// <see cref="IJobEventPublisher.SubscribeToRunCancellationsAsync" /> can cancel the correct token.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _runCancellationSources = new();

    private Guid? _workerInstanceId;
    private int? _progressPercent;
    private string? _progressMessage;
    private CancellationTokenSource? _workerHeartbeatCts;
    private Task? _workerHeartbeatTask;

    /// <summary>Interval between heartbeat PATCH calls while a run is executing.</summary>
    protected virtual TimeSpan HeartbeatInterval => TimeSpan.FromSeconds(30);

    /// <summary>Worker type string — used to derive the input queue name and cancellation subscription queue.</summary>
    protected string WorkerType { get; }

    /// <param name="mqService">Message queue service (provides the input queue subscription).</param>
    /// <param name="jobClient">Typed client for the Job API.</param>
    /// <param name="eventPublisher">Job event publisher used for cancellation subscription.</param>
    /// <param name="workerType">
    /// Worker type identifier. Must match the <c>WorkerType</c> on the <see cref="JobDefinition" /> entities this worker handles. Determines both the queue name
    /// (<c>job.run.{workerType}</c>) and the cancellation subscription queue.
    /// </param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="metrics">Optional metrics.</param>
    /// <param name="maxRequeueCount">Max requeue attempts before the message is routed to the DLQ.</param>
    /// <param name="dlqName">Dead-letter queue name. When null, messages exceeding the requeue limit are dropped.</param>
    protected JobWorkerBase(
        IMqService mqService,
        IJobClient jobClient,
        IJobEventPublisher eventPublisher,
        string workerType,
        ILogger? logger = null,
        IMetrics? metrics = null,
        int? maxRequeueCount = null,
        string? dlqName = null,
        IJobParameterEncryptionService? parameterEncryption = null)
        : base(mqService, Constants.Mq.QueueGetJobRunCreated(workerType), logger, metrics, maxRequeueCount: maxRequeueCount, dlqName: dlqName)
    {
        _jobClient = jobClient;
        _eventPublisher = eventPublisher;
        _parameterEncryption = parameterEncryption;
        WorkerType = workerType;
    }

    /// <inheritdoc />
    public override async Task StartAsync(CancellationToken ct = default)
    {
        await base.StartAsync(ct).ConfigureAwait(false);
        if (!IsRunning)
            throw new InvalidOperationException($"Failed to subscribe to worker queue '{QueueName}'.");

        await RegisterWorkerInstanceAsync(ct).ConfigureAwait(false);

        await _eventPublisher.SubscribeToRunCancellationsAsync(WorkerType, OnCancelAsync, ct).ConfigureAwait(false);

        _workerHeartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _workerHeartbeatTask = RunWorkerInstanceHeartbeatAsync(_workerHeartbeatCts.Token);
    }

    /// <inheritdoc />
    public new async Task StopAsync(CancellationToken ct = default) => await StopWorkerAsync(ct).ConfigureAwait(false);

    async Task IHostedService.StopAsync(CancellationToken cancellationToken) => await StopWorkerAsync(cancellationToken).ConfigureAwait(false);

    private async Task StopWorkerAsync(CancellationToken ct)
    {
        if (_workerHeartbeatCts is not null) {
            await _workerHeartbeatCts.CancelAsync().ConfigureAwait(false);
            if (_workerHeartbeatTask is not null) {
                try {
                    await _workerHeartbeatTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException) { /* expected */
                }
            }

            _workerHeartbeatCts.Dispose();
            _workerHeartbeatCts = null;
            _workerHeartbeatTask = null;
        }

        await DeregisterWorkerInstanceAsync(ct).ConfigureAwait(false);
        await base.StopAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async Task<Result<Unit>> DoWorkAsync(Guid runId, CancellationToken ct)
    {
        var parentTraceId = CurrentMessageEnvelope?.TraceId;
        using var activity = JobTracing.StartWorkerExecution(runId, WorkerType, parentTraceId);
        using var scope = Logger.BeginScope("JobRunId={JobRunId} WorkerType={WorkerType}", runId, WorkerType);
        _progressPercent = null;
        _progressMessage = null;

        var run = await FetchRunAsync(runId, ct).ConfigureAwait(false);
        if (run is null) {
            Logger.LogError("Job run {RunId} not found — skipping", runId);
            return ResultVoid.Failure("Job run not found", "NotFound");
        }

        JobRunRes startedRun;
        try {
            startedRun = await _jobClient.Runs.StartAsync(runId, RunIncludes, ct).ConfigureAwait(false);
        }
        catch (ApiException ex) when (ex.StatusCode == 400) {
            // The Started CAS rejected the transition: the run is not Queued (duplicate delivery already running/finished it, or it was
            // cancelled while queued). Ack without requeue — redelivery would be rejected the same way every time.
            Logger.LogInformation(ex, "Run {RunId} is not startable (already started, cancelled, or finished) — dropping duplicate dispatch", runId);
            Metrics.IncrementCounter(Constants.Metrics.Worker.StartRejected);
            return ResultVoid.Failure("Run not in a startable state", "StartRejected", metadata: NoRequeueMetadata);
        }
        catch (Exception ex) {
            // Transient failure (API/network): the run is still Queued, so the counted requeue can retry it.
            Logger.LogWarning(ex, "Failed to mark run {RunId} as started — retrying via requeue", runId);
            return ResultVoid.Failure("Failed to start run", "StartFailed");
        }

        startedRun = DecryptRunParameters(startedRun);

        using var runCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _runCancellationSources[runId] = runCts;
        var results = new JobWorkerResultBuilder();
        var ctx = new JobWorkerContextImpl(startedRun, Logger, runCts.Token, results, _jobClient, Metrics, this);
        using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var heartbeatTask = RunHeartbeatAsync(runId, heartbeatCts.Token);
        var wasCancelled = false;
        var sw = Stopwatch.StartNew();
        try {
            Logger.LogInformation("Executing job run {RunId}", runId);
            await ExecuteAsync(ctx).ConfigureAwait(false);
            Logger.LogInformation("Job run {RunId} completed with outcome {Outcome}", runId, results.CurrentOutcome);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) {
            // Host shutdown, not a user cancellation: hand the run back to Queued and rethrow so the base returns the message to the
            // broker. Redelivery (after restart or on another instance) re-runs the job instead of terminally cancelling it.
            Logger.LogInformation("Job run {RunId} interrupted by worker shutdown — requeueing for redelivery", runId);
            Metrics.IncrementCounter(Constants.Metrics.Worker.ShutdownRequeued);
            await TryRequeueRunForShutdownAsync(runId).ConfigureAwait(false);
            throw;
        }
        catch (OperationCanceledException) {
            Logger.LogInformation("Job run {RunId} was cancelled", runId);
            results.Cancel();
            wasCancelled = true;
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Unhandled exception in job run {RunId}", runId);
            results.AddError(ex.Message);
        }
        finally {
            _runCancellationSources.TryRemove(runId, out var _);
            await heartbeatCts.CancelAsync().ConfigureAwait(false);
            try {
                await heartbeatTask.ConfigureAwait(false);
            }
            catch { /* heartbeat task already cancelled */
            }
        }

        sw.Stop();
        var outcome = results.CurrentOutcome.ToString();
        Metrics.IncrementCounter(Constants.Metrics.Worker.RunExecuted, tags: [("outcome", outcome)]);
        Metrics.RecordTiming(Constants.Metrics.Worker.RunDuration, sw.Elapsed, [("outcome", outcome)]);
        if (wasCancelled)
            Metrics.IncrementCounter(Constants.Metrics.Worker.CancellationHonored);

        var reported = await ReportFinishedAsync(runId, results.Build(), ct).ConfigureAwait(false);
        if (!reported) {
            // Do not requeue: the run already executed, and a redelivered message would be rejected by the Started CAS guard anyway.
            // If the finish never landed, the run stays Running until dead-job detection times it out and feeds retries/triggers.
            return ResultVoid.Failure("Failed to report run finish", "FinishReportFailed", metadata: NoRequeueMetadata);
        }

        return ResultVoid.Success();
    }

    /// <summary>
    /// Best-effort <c>Running -&gt; Queued</c> hand-back during host shutdown. When the run is not <c>Running</c> anymore (e.g. a user cancellation moved it to
    /// <c>Cancelling</c>), the requeue is rejected and the run is finalized by dead-job detection instead.
    /// </summary>
    private async Task TryRequeueRunForShutdownAsync(Guid runId)
    {
        try {
            await _jobClient.Runs.RequeueAsync(runId, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex) {
            Logger.LogWarning(ex, "Failed to requeue run {RunId} during shutdown; dead-job detection will finalize it", runId);
        }
    }

    /// <summary>Implement this to perform the actual work. Use <paramref name="ctx" /> to read parameters, add results, check the cancellation token, and log messages.</summary>
    protected abstract Task ExecuteAsync(IJobWorkerContext ctx);

    private async Task RegisterWorkerInstanceAsync(CancellationToken ct)
    {
        try {
            var now = DateTime.UtcNow;
            var req = new JobWorkerInstanceReq {
                WorkerType = WorkerType,
                MachineName = Environment.MachineName,
                ProcessId = Environment.ProcessId,
                State = JobWorkerInstanceState.Running,
                InFlightCount = 0,
                StartedTimestamp = now,
                LastHeartbeatUtc = now
            };

            var result = await _jobClient.WorkerInstances.RegisterAsync(req, ct).ConfigureAwait(false);

            if (!result.IsSuccess || result.Data is null)
                Logger.LogWarning("Worker instance registration for {WorkerType} failed: {Error}", WorkerType, result.Error?.Detail ?? result.Error?.Title ?? "unknown");
            else if (result.Data.Id == Guid.Empty)
                Logger.LogWarning("Worker instance registration for {WorkerType} returned an empty id", WorkerType);
            else
                _workerInstanceId = result.Data.Id;
        }
        catch (Exception ex) {
            Logger.LogWarning(ex, "Failed to register worker instance for {WorkerType}", WorkerType);
        }
    }

    private async Task DeregisterWorkerInstanceAsync(CancellationToken ct)
    {
        if (!_workerInstanceId.HasValue || _workerInstanceId.Value == Guid.Empty)
            return;

        try {
            await _jobClient.WorkerInstances.StopAsync(_workerInstanceId.Value, ct).ConfigureAwait(false);
        }
        catch (Exception ex) {
            Logger.LogWarning(ex, "Failed to deregister worker instance {WorkerInstanceId}", _workerInstanceId);
        }
        finally {
            _workerInstanceId = null;
        }
    }

    private async Task RunWorkerInstanceHeartbeatAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(HeartbeatInterval);
        while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false)) {
            if (!_workerInstanceId.HasValue || _workerInstanceId.Value == Guid.Empty) {
                await RegisterWorkerInstanceAsync(ct).ConfigureAwait(false);
                continue;
            }

            try {
                await _jobClient.WorkerInstances.HeartbeatAsync(_workerInstanceId.Value, InFlightCount, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                break;
            }
            catch (ApiException ex) when (ex.StatusCode == 404) {
                Logger.LogWarning("Worker instance {WorkerInstanceId} not found — re-registering", _workerInstanceId);
                _workerInstanceId = null;
                await RegisterWorkerInstanceAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex) {
                Logger.LogWarning(ex, "Worker instance heartbeat failed for {WorkerInstanceId}", _workerInstanceId);
            }
        }
    }

    private async Task<JobRunRes?> FetchRunAsync(Guid runId, CancellationToken ct)
    {
        try {
            return await _jobClient.Runs.GetAsync(runId, RunIncludes, ct).ConfigureAwait(false);
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
                var patch = PatchRequestBuilder.ForId(runId).SetProperty("LastHeartbeatUtc", DateTime.UtcNow);
                if (_progressPercent.HasValue)
                    patch.SetProperty("ProgressPercent", _progressPercent.Value);

                if (_progressMessage is not null)
                    patch.SetProperty("ProgressMessage", _progressMessage);

                await _jobClient.Runs.PatchAsync(runId, patch.Build(), ct).ConfigureAwait(false);
                Metrics.IncrementCounter(Constants.Metrics.Worker.HeartbeatSent);
            }
            catch (OperationCanceledException) {
                break;
            }
            catch (Exception ex) {
                Logger.LogWarning(ex, "Heartbeat failed for run {RunId}", runId);
                Metrics.IncrementCounter(Constants.Metrics.Worker.HeartbeatFailed);
            }
        }
    }

    /// <summary>
    /// Reports the run's results to the Job API. Transient failures are retried in-process a few times; a 400 rejection (the run is no longer finishable — e.g. dead-job
    /// detection already timed it out while this worker was still executing) is terminal and never retried. Returns false when the run row was not transitioned to Finished.
    /// </summary>
    private async Task<bool> ReportFinishedAsync(Guid runId, IReadOnlyList<JobRunResultReq> results, CancellationToken ct)
    {
        const int maxAttempts = 3;
        for (var attempt = 1; attempt <= maxAttempts; attempt++) {
            try {
                await _jobClient.Runs.FinishAsync(runId, results, ct).ConfigureAwait(false);
                return true;
            }
            catch (ApiException ex) when (ex.StatusCode == 400) {
                // Late finish: the run was already finalized (typically Timeout via dead-job detection). Drop cleanly — retrying can never succeed.
                Logger.LogWarning(ex, "Run {RunId} is no longer finishable (already finalized, likely timed out) — dropping late finish report", runId);
                Metrics.IncrementCounter(Constants.Metrics.Worker.LateFinishDropped);
                return false;
            }
            catch (Exception ex) when (attempt < maxAttempts) {
                Logger.LogWarning(ex, "Failed to report finish for run {RunId} (attempt {Attempt}/{Max}) — retrying", runId, attempt, maxAttempts);
                try {
                    await Task.Delay(TimeSpan.FromSeconds(attempt), ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) {
                    return false;
                }
            }
            catch (Exception ex) {
                Logger.LogError(ex, "Failed to report finish for run {RunId} after {Max} attempt(s)", runId, maxAttempts);
                return false;
            }
        }

        return false;
    }

    private Task OnCancelAsync(Guid runId)
    {
        if (_runCancellationSources.TryGetValue(runId, out var cts)) {
            Logger.LogInformation("Cancelling job run {RunId} on worker request", runId);
            cts.Cancel();
        }

        return Task.CompletedTask;
    }

    private JobRunRes DecryptRunParameters(JobRunRes run)
    {
        if (_parameterEncryption is null || run.JobRunParameters is null)
            return run;

        var decrypted = run.JobRunParameters.Select(DecryptRunParameter).ToList();
        return run with { JobRunParameters = decrypted };
    }

    private JobRunParameterRes DecryptRunParameter(JobRunParameterRes parameter)
    {
        if (_parameterEncryption is null || !_parameterEncryption.UsesEncryptedStorage(parameter.EncryptedValue))
            return parameter;

        var value = _parameterEncryption.DecryptValue(parameter.EncryptedValue) ?? parameter.Value;
        return parameter with { Value = value };
    }

    private sealed class JobWorkerContextImpl(
        JobRunRes run,
        ILogger logger,
        CancellationToken ct,
        JobWorkerResultBuilder results,
        IJobClient jobClient,
        IMetrics metrics,
        JobWorkerBase worker) : IJobWorkerContext
    {
        public JobRunRes Run { get; } = run;

        public ILogger Logger { get; } = logger;

        public CancellationToken CancellationToken { get; } = ct;

        public JobWorkerResultBuilder Results { get; } = results;

        public async Task ReportProgressAsync(int percent, string? message = null, CancellationToken ct = default)
        {
            worker._progressPercent = percent;
            worker._progressMessage = message;

            await jobClient.Runs.PatchProgressAsync(Run.Id, percent, message, ct).ConfigureAwait(false);
            metrics.IncrementCounter(Constants.Metrics.Worker.ProgressReported);
        }

        public Task<IReadOnlyList<JobRunRes>> CreateChildRunsAsync(JobCreateChildRunsReq request, CancellationToken ct = default)
            => jobClient.Runs.CreateChildrenAsync(Run.Id, request, ct);
    }
}
