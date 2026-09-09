using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Lyo.Api.Client;
using Lyo.Api.Models.Builders;
using Lyo.Formatter;
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
using Lyo.SystemInformation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Constants = Lyo.Job.Models.Constants;

namespace Lyo.Job.Worker;

/// <summary>
/// Base for every job worker. Owns the full run lifecycle:
/// <list type="number">
/// <item>Take a run ID from the worker queue.</item> <item>Load the full <see cref="JobRunRes" /> from the Job API.</item>
/// <item>Mark the run <c>Running</c> with <c>POST /Job/Run/{id}/Started</c>.</item> <item>Subscribe to cancel signals for this worker type.</item>
/// <item>Call abstract <see cref="ExecuteAsync" /> with a context object.</item> <item>Catch unhandled exceptions and mark the run <c>Failure</c>.</item>
/// <item>Report results with <c>POST /Job/Run/{id}/Finished</c>.</item>
/// </list>
/// Subclasses only implement <see cref="ExecuteAsync" />.
/// </summary>
public abstract class JobWorkerBase : QueueWorkerBase<Guid, Result<Unit>>, IHostedService
{
    private static readonly string[] RunIncludes = ["JobRunParameters", "JobRunResults", "JobSchedule", "JobTrigger", "JobDefinition", "JobDefinition.JobParameters"];

    /// <summary>Result metadata that tells <see cref="QueueWorkerBase{TRequest,TResult}" /> not to requeue the message even when the result is a failure.</summary>
    private static readonly IReadOnlyDictionary<string, object> NoRequeueMetadata = new Dictionary<string, object> { ["requeue"] = false };

    private readonly string? _dlqName;
    private readonly IJobEventPublisher _eventPublisher;

    private readonly IJobClient _jobClient;
    private readonly int? _maxRequeueCount;
    private readonly IJobParameterEncryptionService? _parameterEncryption;

    /// <summary>
    /// Per-run cancellation sources keyed by run ID. Filled when a run starts so a cancel message from
    /// <see cref="IJobEventPublisher.SubscribeToRunCancellationsAsync" /> can cancel the matching token.
    /// </summary>
    private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _runCancellationSources = new();

    private string? _cancelQueueName;
    private IReadOnlyDictionary<string, string?>? _staticSystemMetadata;
    private CancellationTokenSource? _workerHeartbeatCts;
    private Task? _workerHeartbeatTask;

    private Guid? _workerInstanceId;

    /// <summary>How often heartbeat PATCH calls fire while a run is executing.</summary>
    protected virtual TimeSpan HeartbeatInterval => TimeSpan.FromSeconds(30);

    /// <summary>Worker type string. Used to build the input queue name and the cancel-subscription queue.</summary>
    protected string WorkerType { get; }

    /// <param name="mqService">Message queue used to subscribe to the input queue.</param>
    /// <param name="jobClient">Typed Job API client.</param>
    /// <param name="eventPublisher">Job event publisher used to subscribe to cancellations.</param>
    /// <param name="workerType">
    /// Worker type id. Must match <c>WorkerType</c> on the <see cref="JobDefinition" /> entities this worker handles. Sets both the queue name
    /// (<c>job.run.{workerType}</c>) and the cancel-subscription queue.
    /// </param>
    /// <param name="logger">Optional logger.</param>
    /// <param name="metrics">Optional metrics.</param>
    /// <param name="maxRequeueCount">Max requeue attempts before the message goes to the DLQ.</param>
    /// <param name="dlqName">Dead-letter queue name. Null drops messages that exceed the requeue limit.</param>
    /// <param name="parameterEncryption">Optional service that decrypts encrypted job run parameters.</param>
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
        _maxRequeueCount = maxRequeueCount;
        _dlqName = dlqName;
        WorkerType = workerType;
    }

    /// <summary>Optional formatter for string parameter placeholders. DI sets this after construction; without it, templates stay unchanged.</summary>
    internal IFormatterService? Formatter { get; set; }

    /// <inheritdoc />
    public override async Task StartAsync(CancellationToken ct = default)
    {
        await EnsureJobTopologyAsync(ct).ConfigureAwait(false);

        // Register before we consume the dispatch queue so the first in-flight start already has an instance id.
        var cancelSuffix = Guid.NewGuid().ToString("N");
        _cancelQueueName = Constants.Mq.QueueGetJobRunCancelInstance(WorkerType, cancelSuffix);
        await RegisterWorkerInstanceAsync(ct).ConfigureAwait(false);
        await _eventPublisher.SubscribeToRunCancellationsAsync(WorkerType, OnCancelAsync, ct, cancelSuffix).ConfigureAwait(false);
        _workerHeartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _workerHeartbeatTask = RunWorkerInstanceHeartbeatAsync(_workerHeartbeatCts.Token);

        await base.StartAsync(ct).ConfigureAwait(false);
        if (!IsRunning)
            throw new InvalidOperationException($"Failed to subscribe to worker queue '{QueueName}'.");
    }

    /// <summary>
    /// Declares <c>job.events</c> and this worker's dispatch queue (and DLQ when configured) when they are missing. RabbitMQ subscribers do not create queues, so a worker
    /// that starts before the API — or against an empty broker — would otherwise fail to subscribe.
    /// </summary>
    private async Task EnsureJobTopologyAsync(CancellationToken ct)
    {
        if (!MqService.IsConnected())
            await MqService.ConnectAsync(ct).ConfigureAwait(false);

        if (!await MqService.CreateExchange(Constants.Mq.JobEventExchange, Constants.Mq.JobEventExchangeType, true, false, null, ct).ConfigureAwait(false))
            throw new InvalidOperationException($"Failed to declare exchange '{Constants.Mq.JobEventExchange}'.");

        if (!await MqService.CreateQueue(QueueName, true, false, false, null, ct).ConfigureAwait(false)) {
            throw new InvalidOperationException(
                $"Failed to declare queue '{QueueName}'. If it already exists with different arguments, delete it in RabbitMQ and restart the host.");
        }

        if (string.IsNullOrWhiteSpace(_dlqName))
            return;

        if (!await MqService.CreateQueue(_dlqName, true, false, false, null, ct).ConfigureAwait(false)) {
            throw new InvalidOperationException(
                $"Failed to declare queue '{_dlqName}'. If it already exists with different arguments, delete it in RabbitMQ and restart the host.");
        }
    }

    async Task IHostedService.StopAsync(CancellationToken cancellationToken) => await StopWorkerAsync(cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public new async Task StopAsync(CancellationToken ct = default) => await StopWorkerAsync(ct).ConfigureAwait(false);

    private async Task StopWorkerAsync(CancellationToken ct)
    {
        // Drain first, then stop heartbeats, then deregister. Deregistering before the drain published a Stopped instance while runs were
        // still executing, and stopping the heartbeat first let stale-worker pruning delete a registration that was still draining.
        await base.StopAsync(ct).ConfigureAwait(false);
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
    }

    /// <inheritdoc />
    protected override async Task<Result<Unit>> DoWorkAsync(Guid runId, CancellationToken ct)
    {
        var parentTraceId = CurrentMessageEnvelope?.TraceId;
        using var activity = JobTracing.StartWorkerExecution(runId, WorkerType, parentTraceId);
        using var scope = Logger.BeginScope("JobRunId={JobRunId} WorkerType={WorkerType}", runId, WorkerType);
        var (fetchOutcome, run) = await FetchRunAsync(runId, ct).ConfigureAwait(false);
        if (fetchOutcome == RunFetchOutcome.NotFound) {
            // Gone for good (deleted or purged by retention). Ack without spending requeue budget. Redelivery will never find it.
            Logger.LogError("Job run {RunId} does not exist — dropping the dispatch message", runId);
            Metrics.IncrementCounter(Constants.Metrics.Worker.RunNotFound);
            return ResultVoid.Failure("Job run not found", "NotFound", metadata: NoRequeueMetadata);
        }

        if (fetchOutcome == RunFetchOutcome.TransientError || run is null) {
            // API or network problem. The run is still Queued, so the counted requeue can retry.
            Logger.LogWarning("Could not fetch run {RunId} — retrying via requeue", runId);
            return ResultVoid.Failure("Failed to fetch job run", "FetchFailed");
        }

        JobRunRes startedRun;
        try {
            startedRun = await _jobClient.Runs.StartAsync(runId, RunIncludes, ct, BuildRunStartedRequest()).ConfigureAwait(false);
        }
        catch (ApiException ex) when (ex.StatusCode == 400) {
            // Started CAS rejected the transition: the run is not Queued (duplicate delivery already running/finished it, or it was
            // cancelled while queued). Ack without requeue. Redelivery would be rejected the same way every time.
            Logger.LogInformation(ex, "Run {RunId} is not startable (already started, cancelled, or finished) — dropping duplicate dispatch", runId);
            Metrics.IncrementCounter(Constants.Metrics.Worker.StartRejected);
            return ResultVoid.Failure("Run not in a startable state", "StartRejected", metadata: NoRequeueMetadata);
        }
        catch (Exception ex) {
            // Transient API/network failure. The run is still Queued, so the counted requeue can retry.
            Logger.LogWarning(ex, "Failed to mark run {RunId} as started — retrying via requeue", runId);
            return ResultVoid.Failure("Failed to start run", "StartFailed");
        }

        startedRun = DecryptRunParameters(startedRun);
        using var runCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _runCancellationSources[runId] = runCts;
        var results = new JobWorkerResultBuilder();

        // Per-run progress. The worker is a singleton and ProcessingLimit may allow concurrent runs, so instance fields
        // would let one run's progress overwrite another's on the heartbeat patch.
        var progress = new RunProgressState();
        var ctx = new JobWorkerContextImpl(startedRun, Logger, runCts.Token, results, _jobClient, Metrics, progress, Formatter);
        using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var heartbeatTask = RunHeartbeatAsync(runId, progress, heartbeatCts.Token);
        var wasCancelled = false;
        var sw = Stopwatch.StartNew();
        try {
            Logger.LogInformation("Executing job run {RunId}", runId);
            await ExecuteAsync(ctx).ConfigureAwait(false);
            Logger.LogInformation("Job run {RunId} completed with outcome {Outcome}", runId, results.CurrentOutcome);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) {
            // Host shutdown, not a user cancel: put the run back to Queued and rethrow so the base returns the message to the
            // broker. Redelivery (after restart or on another instance) re-runs the job instead of cancelling it for good.
            Logger.LogInformation("Job run {RunId} interrupted by worker shutdown — requeueing for redelivery", runId);
            if (await TryRequeueRunForShutdownAsync(runId).ConfigureAwait(false)) {
                Metrics.IncrementCounter(Constants.Metrics.Worker.ShutdownRequeued);
                throw;
            }

            // Hand-back failed, so the run row is still Running. Returning the message to the broker would strand it: redelivery is
            // rejected by the Started CAS and no worker owns it again. Ack instead and let dead-job detection finalize the run, which
            // feeds the normal retry and trigger paths.
            Logger.LogWarning("Run {RunId} could not be handed back during shutdown; dropping the message and leaving it to dead-job detection", runId);
            Metrics.IncrementCounter(Constants.Metrics.Worker.ShutdownRequeueFailed);
            return ResultVoid.Failure("Shutdown hand-back failed", "ShutdownRequeueFailed", metadata: NoRequeueMetadata);
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
            // Do not requeue. The run already executed, and a redelivered message would hit the Started CAS guard anyway.
            // If the finish never landed, the run stays Running until dead-job detection times it out and feeds retries/triggers.
            return ResultVoid.Failure("Failed to report run finish", "FinishReportFailed", metadata: NoRequeueMetadata);
        }

        return ResultVoid.Success();
    }

    /// <summary>
    /// <c>Running -&gt; Queued</c> hand-back during host shutdown. If the run is no longer <c>Running</c> (for example a user cancel moved it to <c>Cancelling</c>), the
    /// requeue is rejected and dead-job detection finalizes the run instead.
    /// </summary>
    /// <returns>true when the run is back in <c>Queued</c> and safe to redeliver; false when the caller must ack the message instead.</returns>
    private async Task<bool> TryRequeueRunForShutdownAsync(Guid runId)
    {
        try {
            await _jobClient.Runs.RequeueAsync(runId, CancellationToken.None).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) {
            Logger.LogWarning(ex, "Failed to requeue run {RunId} during shutdown; dead-job detection will finalize it", runId);
            return false;
        }
    }

    /// <summary>Do the actual work here. Use <paramref name="ctx" /> to read parameters, add results, check the cancellation token, and log.</summary>
    protected abstract Task ExecuteAsync(IJobWorkerContext ctx);

    /// <summary>Host-supplied keys merged into the instance metadata bag on register and heartbeat. Built-in system info and queue subscriptions are always present.</summary>
    protected virtual IReadOnlyDictionary<string, string?>? GetWorkerMetadata() => null;

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
                LastHeartbeatUtc = now,
                Metadata = BuildWorkerMetadata()
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

    private IReadOnlyDictionary<string, string?> BuildWorkerMetadata()
    {
        var metadata = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in GetStaticSystemMetadata())
            metadata[pair.Key] = pair.Value;

        OverlayLiveProcessStats(metadata);
        OverlayWorkerQueueMetadata(metadata);
        var extra = GetWorkerMetadata();
        if (extra == null)
            return metadata;

        foreach (var pair in extra) {
            if (string.IsNullOrWhiteSpace(pair.Key))
                continue;
            metadata[pair.Key] = pair.Value;
        }

        return metadata;
    }

    private IReadOnlyDictionary<string, string?> GetStaticSystemMetadata()
    {
        if (_staticSystemMetadata is not null)
            return _staticSystemMetadata;

        var hardware = SystemInfoCollector.GetHardwareInfo();
        var software = SystemInfoCollector.GetSoftwareInfo();
        _staticSystemMetadata = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase) {
            [Constants.WorkerMetadata.Os] = software.OsDescription,
            [Constants.WorkerMetadata.OsPlatform] = software.OsPlatform,
            [Constants.WorkerMetadata.OsVersion] = software.OsVersion,
            [Constants.WorkerMetadata.Framework] = software.FrameworkDescription,
            [Constants.WorkerMetadata.RuntimeIdentifier] = software.RuntimeIdentifier,
            [Constants.WorkerMetadata.ClrVersion] = software.ClrVersion,
            [Constants.WorkerMetadata.ProcessArchitecture] = hardware.ProcessArchitecture,
            [Constants.WorkerMetadata.OsArchitecture] = hardware.OsArchitecture,
            [Constants.WorkerMetadata.ProcessorCount] = hardware.ProcessorCount.ToString(CultureInfo.InvariantCulture),
            [Constants.WorkerMetadata.CpuModel] = hardware.CpuModel,
            [Constants.WorkerMetadata.TotalPhysicalMemoryBytes] = FormatInt64(hardware.TotalPhysicalMemoryBytes),
            [Constants.WorkerMetadata.IsServerGc] = software.IsServerGC.ToString(),
            [Constants.WorkerMetadata.ProcessName] = software.ProcessName,
            [Constants.WorkerMetadata.AssemblyVersion] = Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
        };
        return _staticSystemMetadata;
    }

    private void OverlayLiveProcessStats(Dictionary<string, string?> metadata)
    {
        try {
            using var process = Process.GetCurrentProcess();
            metadata[Constants.WorkerMetadata.WorkingSetBytes] = FormatInt64(process.WorkingSet64);
        }
        catch {
            /* process metrics are best-effort */
        }

        try {
            metadata[Constants.WorkerMetadata.GcHeapBytes] = FormatInt64(GC.GetGCMemoryInfo().HeapSizeBytes);
        }
        catch {
            metadata[Constants.WorkerMetadata.GcHeapBytes] = FormatInt64(GC.GetTotalMemory(false));
        }
    }

    private void OverlayWorkerQueueMetadata(Dictionary<string, string?> metadata)
    {
        metadata[Constants.WorkerMetadata.Queue] = QueueName;
        metadata[Constants.WorkerMetadata.WaitQueue] = Constants.Mq.QueueGetJobRunCreatedWait(WorkerType);
        metadata[Constants.WorkerMetadata.CancelQueue] = _cancelQueueName;
        metadata[Constants.WorkerMetadata.Dlq] = _dlqName;
        metadata[Constants.WorkerMetadata.MaxRequeueCount] = _maxRequeueCount?.ToString(CultureInfo.InvariantCulture);
        metadata[Constants.WorkerMetadata.HeartbeatInterval] = HeartbeatInterval.ToString();
        if (RequeueDelay is { } delay)
            metadata[Constants.WorkerMetadata.RequeueDelay] = delay.ToString();

        string[] subscriptions = _cancelQueueName is { Length: > 0 } ? [QueueName, _cancelQueueName] : [QueueName];
        metadata[Constants.WorkerMetadata.Subscriptions] = string.Join(", ", subscriptions);
    }

    private static string? FormatInt64(long? value) => value?.ToString(CultureInfo.InvariantCulture);

    private JobRunStartedReq BuildRunStartedRequest()
        => new() {
            WorkerInstanceId = _workerInstanceId,
            MachineName = Environment.MachineName,
            ProcessId = Environment.ProcessId
        };

    private void ApplyWorkerSnapshot(PatchRequestBuilder patch)
    {
        if (_workerInstanceId is { } instanceId)
            patch.SetProperty("WorkerInstanceId", instanceId);

        patch.SetProperty("WorkerMachineName", Environment.MachineName);
        patch.SetProperty("WorkerProcessId", Environment.ProcessId);
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
                await _jobClient.WorkerInstances.HeartbeatAsync(_workerInstanceId.Value, InFlightCount, ct, BuildWorkerMetadata()).ConfigureAwait(false);
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

    /// <summary>Why a run fetch returned nothing, so the caller can tell a permanently missing run (ack) from a transient failure (requeue).</summary>
    private enum RunFetchOutcome
    {
        Found,
        NotFound,
        TransientError
    }

    /// <summary>
    /// Loads the run being dispatched. A 404 (or a null body) is terminal: the run was deleted or purged, and redelivery will not find it. Anything else is treated as
    /// transient so the counted requeue can retry.
    /// </summary>
    private async Task<(RunFetchOutcome Outcome, JobRunRes? Run)> FetchRunAsync(Guid runId, CancellationToken ct)
    {
        try {
            var run = await _jobClient.Runs.GetAsync(runId, RunIncludes, ct).ConfigureAwait(false);
            return run is null ? (RunFetchOutcome.NotFound, null) : (RunFetchOutcome.Found, run);
        }
        catch (ApiException ex) when (ex.StatusCode == 404) {
            Logger.LogWarning(ex, "Run {RunId} was not found by the Job API", runId);
            return (RunFetchOutcome.NotFound, null);
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Error fetching run {RunId}", runId);
            return (RunFetchOutcome.TransientError, null);
        }
    }

    private async Task RunHeartbeatAsync(Guid runId, RunProgressState progress, CancellationToken ct)
    {
        using var timer = new PeriodicTimer(HeartbeatInterval);
        while (await timer.WaitForNextTickAsync(ct).ConfigureAwait(false)) {
            try {
                var patch = PatchRequestBuilder.ForId(runId).SetProperty("LastHeartbeatUtc", DateTime.UtcNow);
                var (percent, message) = progress.Read();
                if (percent.HasValue)
                    patch.SetProperty("ProgressPercent", percent.Value);

                if (message is not null)
                    patch.SetProperty("ProgressMessage", message);

                ApplyWorkerSnapshot(patch);

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
    /// Sends the run's results to the Job API. Transient failures retry in-process a few times. A 400 rejection (the run is no longer finishable, for example dead-job
    /// detection already timed it out while this worker was still executing) is terminal and is never retried. Returns false when the run row was not moved to Finished.
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
                // Late finish: the run was already finalized (usually Timeout via dead-job detection). Drop it. Retrying can never succeed.
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

    /// <summary>
    /// Latest progress from one run, so the heartbeat loop can include it. Scoped per run because the worker is a singleton and <c>ProcessingLimit</c> may allow several
    /// runs at once. Shared instance fields would let concurrent runs report each other's percentage and message.
    /// </summary>
    private sealed class RunProgressState
    {
        private readonly object _gate = new();
        private string? _message;
        private int? _percent;

        public void Set(int percent, string? message)
        {
            lock (_gate) {
                _percent = percent;
                _message = message;
            }
        }

        public (int? Percent, string? Message) Read()
        {
            lock (_gate) {
                return (_percent, _message);
            }
        }
    }

    private sealed class JobWorkerContextImpl : IJobWorkerContext
    {
        private readonly IJobClient _jobClient;
        private readonly IMetrics _metrics;
        private readonly JobWorkerParameterFormatter _parameters;
        private readonly RunProgressState _progress;

        public JobWorkerContextImpl(
            JobRunRes run,
            ILogger logger,
            CancellationToken ct,
            JobWorkerResultBuilder results,
            IJobClient jobClient,
            IMetrics metrics,
            RunProgressState progress,
            IFormatterService? formatter)
        {
            Logger = logger;
            CancellationToken = ct;
            Results = results;
            _jobClient = jobClient;
            _metrics = metrics;
            _progress = progress;
            _parameters = new(formatter, run);
        }

        public JobRunRes Run => _parameters.Run;

        public ILogger Logger { get; }

        public CancellationToken CancellationToken { get; }

        public JobWorkerResultBuilder Results { get; }

        public void AddContext(string name, object? value) => _parameters.AddContext(name, value);

        public string Format(string template) => _parameters.Format(template);

        public async Task ReportProgressAsync(int percent, string? message = null, CancellationToken ct = default)
        {
            _progress.Set(percent, message);
            await _jobClient.Runs.PatchProgressAsync(Run.Id, percent, message, ct).ConfigureAwait(false);
            _metrics.IncrementCounter(Constants.Metrics.Worker.ProgressReported);
        }

        public Task<IReadOnlyList<JobRunRes>> CreateChildRunsAsync(JobCreateChildRunsReq request, CancellationToken ct = default)
            => _jobClient.Runs.CreateChildrenAsync(Run.Id, request, ct);
    }
}