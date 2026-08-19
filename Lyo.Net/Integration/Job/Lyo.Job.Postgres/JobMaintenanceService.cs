using System.Diagnostics;
using Lyo.Cache;
using Lyo.Health;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Events;
using Lyo.Job.Postgres.Database;
using Lyo.Metrics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Constants = Lyo.Job.Models.Constants;
using JobRunResult = Lyo.Job.Models.Enums.JobRunResult;
using JobState = Lyo.Job.Models.Enums.JobState;
using JobWorkerInstanceState = Lyo.Job.Models.Enums.JobWorkerInstanceState;

namespace Lyo.Job.Postgres;

/// <summary>
/// Background service that runs maintenance tasks on the job database on a periodic schedule:
/// <list type="bullet">
/// <item>
/// <term>Dead job detection</term>
/// <description>
/// Scans <c>Running</c>/<c>Cancelling</c> runs whose <c>LastHeartbeatUtc</c> is older than <c>JobDefinition.TimeoutMinutes</c> and transitions them to
/// <c>Finished / Timeout</c>.
/// </description>
/// </item>
/// <item>
/// <term>Circuit breaker reset</term>
/// <description>Re-enables job definitions whose circuit breaker has been tripped and whose <c>CircuitBreakerResetMinutes</c> cooldown has elapsed.</description>
/// </item>
/// <item>
/// <term>Run history retention</term>
/// <description>
/// Purges finished runs (with their logs, parameters, and results) older than the definition's <c>RetentionDays</c> (or the global
/// <see cref="JobMaintenanceOptions.DefaultRetentionDays" />) in batches of <see cref="JobMaintenanceOptions.PurgeBatchSize" />.
/// </description>
/// </item>
/// <item>
/// <term>Stale worker pruning</term>
/// <description>Removes <c>job_worker_instance</c> rows whose heartbeat is older than <see cref="JobMaintenanceOptions.WorkerInstanceStaleMinutes" />.</description>
/// </item>
/// <item>
/// <term>SLA breach detection</term>
/// <description>Marks runs with <c>SlaBreached=true</c> when a running job exceeds <c>ExpectedDurationMinutes</c> or a queued job is past <c>MustStartByMinutes</c> without starting.</description>
/// </item>
/// </list>
/// Register via <see cref="Extensions.AddJobMaintenanceService" />.
/// </summary>
public sealed class JobMaintenanceService : BackgroundService, IHealth
{
    private readonly IDbContextFactory<JobContext> _dbFactory;
    private readonly IJobEventPublisher _eventPublisher;
    private readonly ILogger<JobMaintenanceService> _logger;
    private readonly IMetrics _metrics;
    private readonly JobMaintenanceOptions _options;
    private readonly ICacheService? _cache;

    private DateTime? _lastSuccessfulTickUtc;
    private string? _lastTickError;

    public bool IsRunning => !ExecuteTask?.IsCompleted ?? false;

    public JobMaintenanceService(
        IDbContextFactory<JobContext> dbFactory,
        ILogger<JobMaintenanceService> logger,
        IJobEventPublisher eventPublisher,
        JobMaintenanceOptions? options = null,
        IMetrics? metrics = null,
        ICacheService? cache = null)
    {
        _dbFactory = dbFactory;
        _eventPublisher = eventPublisher;
        _logger = logger;
        _options = options ?? new JobMaintenanceOptions();
        _metrics = metrics ?? NullMetrics.Instance;
        _cache = cache;
    }

    /// <inheritdoc />
    public string HealthCheckName => "job-maintenance";

    /// <inheritdoc />
    public Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var metadata = new Dictionary<string, object?> {
            ["is_running"] = IsRunning,
            ["last_successful_tick_utc"] = _lastSuccessfulTickUtc,
            ["last_tick_error"] = _lastTickError,
            ["check_interval_seconds"] = _options.CheckIntervalSeconds,
            ["default_retention_days"] = _options.DefaultRetentionDays
        };

        // Unhealthy when not running, or when no tick has succeeded for 3 intervals.
        var staleAfter = TimeSpan.FromSeconds(_options.CheckIntervalSeconds * 3);
        var stale = _lastSuccessfulTickUtc.HasValue && DateTime.UtcNow - _lastSuccessfulTickUtc.Value > staleAfter;
        var result = !IsRunning ? HealthResult.Unhealthy(sw.Elapsed, "Maintenance service is not running", metadata) :
            stale ? HealthResult.Unhealthy(sw.Elapsed, $"No successful maintenance tick since {_lastSuccessfulTickUtc:u}", metadata) :
            HealthResult.Healthy(sw.Elapsed, "Maintenance service running", metadata);

        return Task.FromResult(result);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.CheckIntervalSeconds));
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false)) {
            try {
                using (_metrics.StartTimer(Constants.Metrics.Maintenance.TickDuration))
                    await RunMaintenanceAsync(stoppingToken).ConfigureAwait(false);

                _lastSuccessfulTickUtc = DateTime.UtcNow;
                _lastTickError = null;
            }
            catch (OperationCanceledException) {
                break;
            }
            catch (Exception ex) {
                _lastTickError = ex.Message;
                _metrics.IncrementCounter(Constants.Metrics.Maintenance.TickError);
                _logger.LogError(ex, "JobMaintenanceService tick failed");
            }
        }
    }

    private async Task RunMaintenanceAsync(CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var timedOutRunIds = await FailDeadJobsAsync(db, ct).ConfigureAwait(false);
        await CheckSlaBreachesAsync(db, ct).ConfigureAwait(false);
        var resetDefinitionIds = await ResetCircuitBreakersAsync(db, ct).ConfigureAwait(false);
        await PruneStaleWorkerInstancesAsync(db, ct).ConfigureAwait(false);
        await RedispatchStuckQueuedRunsAsync(db, ct).ConfigureAwait(false);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Publish only after the state changes are committed, so consumers read the final state.
        await PublishRunsFinishedAsync(timedOutRunIds, ct).ConfigureAwait(false);
        await PublishDefinitionsUpdatedAsync(resetDefinitionIds, ct).ConfigureAwait(false);
        if (timedOutRunIds.Count > 0)
            await JobRunQueryCache.InvalidateAsync(_cache).ConfigureAwait(false);

        await PurgeExpiredRunsAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Publishes run-finished events for runs the maintenance pass timed out. Without this, a timeout would be a silent dead end: the scheduler would never see the completion,
    /// so retries, triggers, and circuit-breaker accounting would not fire.
    /// </summary>
    private async Task PublishRunsFinishedAsync(IReadOnlyList<Guid> runIds, CancellationToken ct)
    {
        if (runIds.Count == 0 || !_eventPublisher.IsConnected())
            return;

        foreach (var runId in runIds) {
            try {
                await _eventPublisher.PublishRunFinishedAsync(runId, ct).ConfigureAwait(false);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to publish run-finished event for timed-out run {RunId}", runId);
            }
        }
    }

    /// <summary>Notifies schedulers that circuit-breaker definitions were re-enabled so their in-memory caches refresh promptly.</summary>
    private async Task PublishDefinitionsUpdatedAsync(IReadOnlyList<Guid> definitionIds, CancellationToken ct)
    {
        if (definitionIds.Count == 0 || !_eventPublisher.IsConnected())
            return;

        foreach (var definitionId in definitionIds) {
            try {
                await _eventPublisher.PublishDefinitionUpdatedAsync(definitionId, ct).ConfigureAwait(false);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to publish definition-updated event for {DefinitionId}", definitionId);
            }
        }
    }

    private async Task<IReadOnlyList<Guid>> FailDeadJobsAsync(JobContext db, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // Load running/cancelling runs that have a heartbeat timeout defined on their definition. Runs that died before their first
        // heartbeat have a null LastHeartbeatUtc — fall back to StartedTimestamp/CreatedTimestamp so they cannot stay orphaned forever.
        var candidates = await db.JobRuns.Include(r => r.JobDefinition)
            .Where(r => (r.State == JobState.Running || r.State == JobState.Cancelling) && r.JobDefinition.TimeoutMinutes > 0)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var failed = 0;
        var timedOutRunIds = new List<Guid>();
        foreach (var run in candidates) {
            var baseline = run.LastHeartbeatUtc ?? run.StartedTimestamp ?? run.CreatedTimestamp;
            var deadline = baseline.AddMinutes(run.JobDefinition.TimeoutMinutes);
            if (now < deadline)
                continue;

            _logger.LogWarning(
                "Job run {RunId} (definition {DefinitionName}) has not sent a heartbeat since {LastActivity:u} " + "(timeout {TimeoutMinutes} min) — marking as failed", run.Id,
                run.JobDefinition.Name, baseline, run.JobDefinition.TimeoutMinutes);

            run.State = JobState.Finished;
            run.Result = JobRunResult.Timeout;
            run.FinishedTimestamp = now;
            failed++;
            timedOutRunIds.Add(run.Id);
            if (_eventPublisher.IsConnected()) {
                try {
                    await _eventPublisher.PublishAlertAsync(
                            run.JobDefinitionId, run.Id, JobAlertType.DeadJob, $"Job run {run.Id} for '{run.JobDefinition.Name}' timed out (no heartbeat)", ct)
                        .ConfigureAwait(false);
                }
                catch (Exception ex) {
                    _logger.LogError(ex, "Failed to publish dead-job alert for run {RunId}", run.Id);
                }
            }
        }

        if (failed > 0)
            _metrics.IncrementCounter(Constants.Metrics.Maintenance.DeadJobsFailed, failed);

        return timedOutRunIds;
    }

    private async Task CheckSlaBreachesAsync(JobContext db, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var candidates = await db.JobRuns.Include(r => r.JobDefinition)
            .Where(r => !r.SlaBreached && (r.State == JobState.Running || r.State == JobState.Cancelling || r.State == JobState.Queued))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var breached = 0;
        foreach (var run in candidates) {
            var def = run.JobDefinition;
            if (run.State is JobState.Running or JobState.Cancelling) {
                if (def.ExpectedDurationMinutes <= 0 || !run.StartedTimestamp.HasValue)
                    continue;

                if (run.StartedTimestamp.Value.AddMinutes(def.ExpectedDurationMinutes) >= now)
                    continue;
            }
            else {
                if (def.MustStartByMinutes <= 0)
                    continue;

                if (run.CreatedTimestamp.AddMinutes(def.MustStartByMinutes) >= now)
                    continue;
            }

            _logger.LogWarning("SLA breach detected for run {RunId} (definition {DefinitionName}, state {State})", run.Id, def.Name, run.State);
            run.SlaBreached = true;
            breached++;
        }

        if (breached > 0)
            _metrics.IncrementCounter(Constants.Metrics.Sla.Breach, breached);
    }

    private async Task<IReadOnlyList<Guid>> ResetCircuitBreakersAsync(JobContext db, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var tripped = await db.JobDefinitions.Where(d => !d.Enabled && d.CircuitBreakerResetMinutes > 0 && d.CircuitBreakerTrippedAt != null).ToListAsync(ct).ConfigureAwait(false);
        var resetIds = new List<Guid>();
        foreach (var def in tripped) {
            var resetAt = def.CircuitBreakerTrippedAt!.Value.AddMinutes(def.CircuitBreakerResetMinutes);
            if (now < resetAt)
                continue;

            _logger.LogInformation("Resetting circuit breaker for definition {DefinitionName} ({DefinitionId}) — cooldown elapsed", def.Name, def.Id);
            def.Enabled = true;
            def.CircuitBreakerTrippedAt = null;
            resetIds.Add(def.Id);
        }

        if (resetIds.Count > 0)
            _metrics.IncrementCounter(Constants.Metrics.Maintenance.CircuitBreakersReset, resetIds.Count);

        return resetIds;
    }

    /// <summary>
    /// Recovery path for <c>Queued</c> runs that were persisted but never dispatched (publish failure after insert, delayed retries whose slot has passed, or suppressed
    /// dispatches whose owner crashed before publishing). Re-publishes the run-created message; duplicate deliveries are harmless because <c>StartedJobRun</c> only transitions
    /// <c>Queued -&gt; Running</c> once.
    /// </summary>
    private async Task RedispatchStuckQueuedRunsAsync(JobContext db, CancellationToken ct)
    {
        if (_options.QueuedRunRedispatchMinutes <= 0 || !_eventPublisher.IsConnected())
            return;

        var now = DateTime.UtcNow;
        var cutoff = now.AddMinutes(-_options.QueuedRunRedispatchMinutes);

        // Two triggers: (a) a run with a slot that has come due but was last touched before the slot (delayed retry not yet dispatched);
        // (b) any due queued run untouched for longer than the threshold (lost dispatch). Redispatching bumps UpdatedTimestamp so a stuck
        // run is retried once per threshold window, not every tick.
        var candidates = await db.JobRuns.Include(r => r.JobDefinition)
            .Where(r => r.State == JobState.Queued && !r.DryRun &&
                ((r.ScheduledSlotUtc != null && r.ScheduledSlotUtc <= now && (r.UpdatedTimestamp ?? r.CreatedTimestamp) < r.ScheduledSlotUtc) ||
                    ((r.ScheduledSlotUtc == null || r.ScheduledSlotUtc <= now) && (r.UpdatedTimestamp ?? r.CreatedTimestamp) < cutoff)))
            .OrderBy(r => r.CreatedTimestamp)
            .Take(_options.QueuedRunRedispatchBatchSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var redispatched = 0;
        foreach (var run in candidates) {
            try {
                await _eventPublisher.PublishRunCreatedAsync(run.Id, run.JobDefinition.WorkerType, run.Priority, ct).ConfigureAwait(false);
                run.UpdatedTimestamp = now;
                redispatched++;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to redispatch stuck queued run {RunId}", run.Id);
            }
        }

        if (redispatched > 0) {
            _metrics.IncrementCounter(Constants.Metrics.Maintenance.RunsRedispatched, redispatched);
            _logger.LogInformation("Redispatched {Count} stuck queued job run(s)", redispatched);
        }
    }

    private async Task PruneStaleWorkerInstancesAsync(JobContext db, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-_options.WorkerInstanceStaleMinutes);
        var stoppedState = nameof(JobWorkerInstanceState.Stopped);
        var stale = await db.JobWorkerInstances.Where(w => w.LastHeartbeatUtc < cutoff || w.State == stoppedState).ToListAsync(ct).ConfigureAwait(false);
        if (stale.Count == 0)
            return;

        foreach (var instance in stale) {
            if (instance.State != stoppedState) {
                _logger.LogWarning(
                    "Removing stale worker instance {InstanceId} ({WorkerType} on {MachineName}) — last heartbeat {LastHeartbeat:u}", instance.Id, instance.WorkerType,
                    instance.MachineName, instance.LastHeartbeatUtc);
            }
        }

        db.JobWorkerInstances.RemoveRange(stale);
        _metrics.IncrementCounter(Constants.Metrics.Maintenance.WorkerInstancesPruned, stale.Count);
    }

    /// <summary>
    /// Purges finished runs older than the effective retention (per-definition <c>RetentionDays</c>, falling back to the global default) in batches. Uses its own DbContext so
    /// batched deletes commit independently of the main maintenance pass.
    /// </summary>
    private async Task PurgeExpiredRunsAsync(CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var now = DateTime.UtcNow;
        var defaults = _options.DefaultRetentionDays;

        // Definitions with an explicit retention always purge; others only when a global default is configured.
        var definitions = await db.JobDefinitions.Where(d => d.RetentionDays > 0 || defaults > 0).Select(d => new { d.Id, d.RetentionDays }).ToListAsync(ct).ConfigureAwait(false);
        var totalPurged = 0;
        var budget = _options.PurgeBatchSize;
        foreach (var def in definitions) {
            if (budget <= 0)
                break;

            var retentionDays = def.RetentionDays > 0 ? def.RetentionDays : defaults;
            if (retentionDays <= 0)
                continue;

            var cutoff = now.AddDays(-retentionDays);
            var expired = await db.JobRuns.Where(r => r.JobDefinitionId == def.Id && r.State == JobState.Finished && r.FinishedTimestamp != null && r.FinishedTimestamp < cutoff)
                .OrderBy(r => r.FinishedTimestamp)
                .Take(budget)
                .Include(r => r.JobRunLogs)
                .Include(r => r.JobRunParameters)
                .Include(r => r.JobRunResults)
                .Include(r => r.InverseReRanFromJobRun)
                .Include(r => r.InverseTriggeredByJobRun)
                .Include(r => r.InverseParentJobRun)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            if (expired.Count == 0)
                continue;

            foreach (var run in expired) {
                // Detach self-referencing runs that survive this purge so FK constraints are not violated.
                foreach (var child in run.InverseReRanFromJobRun)
                    child.ReRanFromJobRunId = null;

                foreach (var child in run.InverseTriggeredByJobRun)
                    child.TriggeredByJobRunId = null;

                foreach (var child in run.InverseParentJobRun)
                    child.ParentJobRunId = null;

                db.JobRunLogs.RemoveRange(run.JobRunLogs);
                db.JobRunParameters.RemoveRange(run.JobRunParameters);
                db.JobRunResults.RemoveRange(run.JobRunResults);
            }

            // Detach workflow run steps that reference purged runs. Unlike the delete endpoint (which removes the steps entirely on an
            // explicit user delete), retention purge preserves workflow history and only nulls the run reference.
            var expiredIds = expired.Select(r => r.Id).ToList();
            var workflowSteps = await db.JobWorkflowRunSteps.Where(s => s.JobRunId != null && expiredIds.Contains(s.JobRunId.Value)).ToListAsync(ct).ConfigureAwait(false);
            foreach (var step in workflowSteps)
                step.JobRunId = null;

            db.JobRuns.RemoveRange(expired);
            budget -= expired.Count;
            totalPurged += expired.Count;
        }

        if (totalPurged == 0)
            return;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        _metrics.IncrementCounter(Constants.Metrics.Maintenance.RunsPurged, totalPurged);
        _logger.LogInformation("Purged {Count} expired job runs (retention policy)", totalPurged);
        await JobRunQueryCache.InvalidateAsync(_cache).ConfigureAwait(false);
    }
}