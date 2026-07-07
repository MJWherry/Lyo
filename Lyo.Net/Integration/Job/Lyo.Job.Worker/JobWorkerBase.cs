using System.Collections.Concurrent;
using System.Diagnostics;
using Lyo.Api.Client;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Common.Request;
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

    private readonly string _apiBaseUrl;

    private readonly IApiClient _apiClient;
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
    /// <param name="apiClient">HTTP client used to call the Job API.</param>
    /// <param name="eventPublisher">Job event publisher used for cancellation subscription.</param>
    /// <param name="workerType">
    /// Worker type identifier. Must match the <c>WorkerType</c> on the <see cref="JobDefinition" /> entities this worker handles. Determines both the queue name
    /// (<c>job.run.{workerType}</c>) and the cancellation subscription queue.
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
        string? dlqName = null,
        IJobParameterEncryptionService? parameterEncryption = null)
        : base(mqService, Constants.Mq.QueueGetJobRunCreated(workerType), logger, metrics, maxRequeueCount: maxRequeueCount, dlqName: dlqName)
    {
        _apiClient = apiClient;
        _eventPublisher = eventPublisher;
        _parameterEncryption = parameterEncryption;
        _apiBaseUrl = apiBaseUrl.TrimEnd('/');
        WorkerType = workerType;
    }

    /// <inheritdoc />
    public override async Task StartAsync(CancellationToken ct = default)
    {
        if (!MqService.IsConnected())
            await MqService.ConnectAsync(ct).ConfigureAwait(false);

        await MqService.CreateQueue(QueueName, arguments: new Dictionary<string, object> { ["x-max-priority"] = 10 }, ct: ct).ConfigureAwait(false);
        await RegisterWorkerInstanceAsync(ct).ConfigureAwait(false);

        await base.StartAsync(ct).ConfigureAwait(false);

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

        var include = string.Join("&include=", RunIncludes);
        var startedRun = await _apiClient.PostAsAsync<JobRunRes>($"{_apiBaseUrl}/{Constants.Rest.Job.RunStarted(runId)}?include={include}", ct: ct).ConfigureAwait(false);
        if (startedRun is null) {
            Logger.LogWarning("Failed to mark run {RunId} as started — it may have been cancelled or already processed", runId);
            return ResultVoid.Failure("Failed to start run", "StartFailed");
        }

        startedRun = DecryptRunParameters(startedRun);

        using var runCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _runCancellationSources[runId] = runCts;
        var results = new JobWorkerResultBuilder();
        var ctx = new JobWorkerContextImpl(startedRun, Logger, runCts.Token, results, _apiClient, _apiBaseUrl, Metrics, this);
        using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var heartbeatTask = RunHeartbeatAsync(runId, heartbeatCts.Token);
        var wasCancelled = false;
        var sw = Stopwatch.StartNew();
        try {
            Logger.LogInformation("Executing job run {RunId}", runId);
            await ExecuteAsync(ctx).ConfigureAwait(false);
            Logger.LogInformation("Job run {RunId} completed with outcome {Outcome}", runId, results.CurrentOutcome);
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
        if (!reported)
            return ResultVoid.Failure("Failed to report run finish", "FinishReportFailed");

        return ResultVoid.Success();
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

            var instance = await _apiClient.PostAsAsync<JobWorkerInstanceReq, JobWorkerInstanceRes>($"{_apiBaseUrl}/{Constants.Rest.Job.WorkerInstances}", req, ct: ct)
                .ConfigureAwait(false);

            if (instance is null)
                Logger.LogWarning("Worker instance registration for {WorkerType} returned no result", WorkerType);
            else
                _workerInstanceId = instance.Id;
        }
        catch (Exception ex) {
            Logger.LogWarning(ex, "Failed to register worker instance for {WorkerType}", WorkerType);
        }
    }

    private async Task DeregisterWorkerInstanceAsync(CancellationToken ct)
    {
        if (!_workerInstanceId.HasValue)
            return;

        try {
            var patch = PatchRequestBuilder.ForId(_workerInstanceId.Value)
                .SetProperty("State", JobWorkerInstanceState.Stopped)
                .SetProperty("InFlightCount", 0)
                .SetProperty("LastHeartbeatUtc", DateTime.UtcNow)
                .Build();

            await _apiClient.PatchAsAsync<PatchRequest, object>($"{_apiBaseUrl}/{Constants.Rest.Job.WorkerInstances}/{_workerInstanceId.Value}", patch, ct: ct)
                .ConfigureAwait(false);
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
            if (!_workerInstanceId.HasValue)
                continue;

            try {
                var patch = PatchRequestBuilder.ForId(_workerInstanceId.Value)
                    .SetProperty("LastHeartbeatUtc", DateTime.UtcNow)
                    .SetProperty("InFlightCount", InFlightCount)
                    .SetProperty("State", JobWorkerInstanceState.Running)
                    .Build();

                await _apiClient.PatchAsAsync<PatchRequest, object>($"{_apiBaseUrl}/{Constants.Rest.Job.WorkerInstances}/{_workerInstanceId.Value}", patch, ct: ct)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                break;
            }
            catch (Exception ex) {
                Logger.LogWarning(ex, "Worker instance heartbeat failed for {WorkerInstanceId}", _workerInstanceId);
            }
        }
    }

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
                var patch = PatchRequestBuilder.ForId(runId).SetProperty("LastHeartbeatUtc", DateTime.UtcNow);
                if (_progressPercent.HasValue)
                    patch.SetProperty("ProgressPercent", _progressPercent.Value);

                if (_progressMessage is not null)
                    patch.SetProperty("ProgressMessage", _progressMessage);

                await _apiClient.PatchAsAsync<PatchRequest, object>($"{_apiBaseUrl}/{Constants.Rest.Job.Runs}/{runId}", patch.Build(), ct: ct).ConfigureAwait(false);
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

    /// <summary>Reports the run's results to the Job API. Returns false when the finish POST failed (the run row was not transitioned to Finished).</summary>
    private async Task<bool> ReportFinishedAsync(Guid runId, IReadOnlyList<JobRunResultReq> results, CancellationToken ct)
    {
        try {
            var finished = await _apiClient.PostAsAsync<IReadOnlyList<JobRunResultReq>, JobRunRes>($"{_apiBaseUrl}/{Constants.Rest.Job.RunFinished(runId)}", results, ct: ct)
                .ConfigureAwait(false);

            if (finished is null)
                Logger.LogError("Finish report for run {RunId} returned no result — run state may not have been updated", runId);

            return finished is not null;
        }
        catch (Exception ex) {
            Logger.LogError(ex, "Failed to report finish for run {RunId}", runId);
            return false;
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
        IApiClient apiClient,
        string apiBaseUrl,
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

            var patch = PatchRequestBuilder.ForId(Run.Id).SetProperty("ProgressPercent", percent).SetProperty("LastHeartbeatUtc", DateTime.UtcNow);
            if (message is not null)
                patch.SetProperty("ProgressMessage", message);

            await apiClient.PatchAsAsync<PatchRequest, object>($"{apiBaseUrl}/{Constants.Rest.Job.Runs}/{Run.Id}", patch.Build(), ct: ct).ConfigureAwait(false);
            metrics.IncrementCounter(Constants.Metrics.Worker.ProgressReported);
        }

        public async Task<IReadOnlyList<JobRunRes>> CreateChildRunsAsync(JobCreateChildRunsReq request, CancellationToken ct = default)
        {
            var created = await apiClient.PostAsAsync<JobCreateChildRunsReq, IReadOnlyList<JobRunRes>>(
                $"{apiBaseUrl}/{Constants.Rest.Job.RunChildren(Run.Id)}", request, ct: ct).ConfigureAwait(false);

            return created ?? [];
        }
    }
}
