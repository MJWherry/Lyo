using Lyo.Api.Services.Crud.Read.Query;
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
/// Hosted service that runs maintenance tasks against the job database on a periodic schedule:
/// <list type="bullet">
/// <item>
/// <term>Dead job detection</term>
/// <description>
/// Finds <c>Running</c>/<c>Cancelling</c> runs whose <c>LastHeartbeatUtc</c> is older than <c>JobDefinition.TimeoutMinutes</c> and moves them to
/// <c>Finished / Timeout</c>.
/// </description>
/// </item>
/// <item>
/// <term>Circuit breaker reset</term>
/// <description>Turns job definitions back on after the circuit breaker has tripped and the <c>CircuitBreakerResetMinutes</c> cooldown has elapsed.</description>
/// </item>
/// <item>
/// <term>Run history retention</term>
/// <description>
/// Deletes finished runs (with their logs, parameters, and results) older than the definition's <c>RetentionDays</c> (or the global
/// <see cref="JobMaintenanceOptions.DefaultRetentionDays" />) in batches of <see cref="JobMaintenanceOptions.PurgeBatchSize" />.
/// </description>
/// </item>
/// <item>
/// <term>Stale worker pruning</term>
/// <description>Deletes <c>job_worker_instance</c> rows whose heartbeat is older than <see cref="JobMaintenanceOptions.WorkerInstanceStaleMinutes" />.</description>
/// </item>
/// <item>
/// <term>SLA breach detection</term>
/// <description>
/// Sets <c>SlaBreached=true</c> when a running job exceeds <c>ExpectedDurationMinutes</c> or a queued job is past <c>MustStartByMinutes</c> without starting.
/// </description>
/// </item>
/// </list>
/// Register through <see cref="Extensions.AddJobMaintenanceService" />.
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

        // Unhealthy when not running, or when no tick has succeeded for three intervals.
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

        // FailDeadJobsAsync commits its own guarded updates. Everything below batches into the single SaveChanges.
        var timedOut = await FailDeadJobsAsync(db, ct).ConfigureAwait(false);
        var slaBreaches = await CheckSlaBreachesAsync(db, ct).ConfigureAwait(false);
        var resetDefinitionIds = await ResetCircuitBreakersAsync(db, ct).ConfigureAwait(false);
        await PruneStaleWorkerInstancesAsync(db, ct).ConfigureAwait(false);
        var redispatchCandidates = await FindStuckQueuedRunsAsync(db, ct).ConfigureAwait(false);
        var saved = await TrySaveTrackedChangesAsync(db, ct).ConfigureAwait(false);

        // Every publish happens after the commit so consumers never read state that is still in flight, or that a failed save rolled back.
        // Dead-job timeouts commit through their own guarded updates, so their events stay independent of the tracked save above.
        await PublishDeadJobAlertsAsync(timedOut, ct).ConfigureAwait(false);
        await PublishRunsFinishedAsync(timedOut.Select(t => t.RunId).ToList(), ct).ConfigureAwait(false);
        if (saved) {
            await PublishSlaAlertsAsync(slaBreaches, ct).ConfigureAwait(false);
            await PublishDefinitionsUpdatedAsync(resetDefinitionIds, ct).ConfigureAwait(false);
        }

        await RedispatchStuckQueuedRunsAsync(redispatchCandidates, ct).ConfigureAwait(false);
        if (timedOut.Count > 0)
            await QueryCacheInvalidation.InvalidateQueryCachesForBroadEntityTypeAsync<JobRun, JobDefinition>(_cache).ConfigureAwait(false);

        await PurgeExpiredRunsAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Publishes run-finished events for runs this maintenance pass timed out. Without this, a timeout would be a silent dead end: the scheduler would never see the completion,
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

    /// <summary>
    /// Commits tracked changes from the SLA, circuit-breaker, and worker-pruning passes. <c>job_run</c> carries an <c>xmin</c> concurrency token, so a worker touching a run
    /// mid-pass surfaces as <see cref="DbUpdateConcurrencyException" />. That is expected contention rather than a fault, and the next tick re-evaluates from fresh state.
    /// </summary>
    /// <returns>true when those changes were committed.</returns>
    private async Task<bool> TrySaveTrackedChangesAsync(JobContext db, CancellationToken ct)
    {
        try {
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            return true;
        }
        catch (DbUpdateConcurrencyException ex) {
            _metrics.IncrementCounter(Constants.Metrics.Maintenance.ConcurrencyConflict);
            _logger.LogInformation(
                ex, "Maintenance pass lost a concurrency race on {Count} entrie(s); skipping this tick's tracked updates", ex.Entries.Count);

            return false;
        }
    }

    /// <summary>Publishes dead-job alerts for the runs this pass timed out. Runs after the commit so an alert never describes a transition that rolled back.</summary>
    private async Task PublishDeadJobAlertsAsync(IReadOnlyList<DeadJobTimeout> timedOut, CancellationToken ct)
    {
        if (timedOut.Count == 0 || !_eventPublisher.IsConnected())
            return;

        foreach (var run in timedOut) {
            try {
                await _eventPublisher.PublishAlertAsync(
                        run.JobDefinitionId, run.RunId, JobAlertType.DeadJob, $"Job run {run.RunId} for '{run.DefinitionName}' timed out (no heartbeat)", ct)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to publish dead-job alert for run {RunId}", run.RunId);
            }
        }
    }

    /// <summary>Publishes SLA alerts for breaches this pass found, matching what <see cref="JobService" /> publishes on the start and finish paths.</summary>
    private async Task PublishSlaAlertsAsync(IReadOnlyList<SlaBreach> breaches, CancellationToken ct)
    {
        if (breaches.Count == 0 || !_eventPublisher.IsConnected())
            return;

        foreach (var breach in breaches) {
            try {
                await _eventPublisher.PublishAlertAsync(breach.JobDefinitionId, breach.RunId, JobAlertType.SlaBreach, $"'{breach.DefinitionName}': {breach.Reason}", ct)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to publish SLA alert for run {RunId}", breach.RunId);
            }
        }
    }

    /// <summary>Tells schedulers that circuit-breaker definitions were re-enabled so their in-memory caches refresh promptly.</summary>
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

    /// <summary>
    /// Times out runs whose worker stopped heartbeating. Each transition is an atomic compare-and-swap that re-evaluates both the state and the heartbeat baseline in SQL, so a
    /// worker that heartbeats or finishes between the candidate scan and the update wins the race. Previously the tracked-entity write clobbered a successful finish with
    /// <c>Timeout</c>.
    /// </summary>
    /// <returns>Ids of the runs actually timed out, used for post-commit event publication.</returns>
    private async Task<IReadOnlyList<DeadJobTimeout>> FailDeadJobsAsync(JobContext db, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var orphanCeiling = _options.OrphanedRunTimeoutMinutes;

        // Candidate scan only. The authoritative deadline check happens in the guarded update below. Runs that died before their first
        // heartbeat have a null LastHeartbeatUtc, so fall back to StartedTimestamp/CreatedTimestamp. Definitions without a TimeoutMinutes
        // are still bounded by OrphanedRunTimeoutMinutes. Otherwise a crashed worker would leave them Running forever.
        var candidates = await db.JobRuns.AsNoTracking()
            .Where(r => (r.State == JobState.Running || r.State == JobState.Cancelling) && (r.JobDefinition.TimeoutMinutes > 0 || orphanCeiling > 0))
            .OrderBy(r => r.LastHeartbeatUtc ?? r.StartedTimestamp ?? r.CreatedTimestamp)
            .Take(_options.ActiveRunScanBatchSize)
            .Select(r => new DeadJobCandidate(
                r.Id, r.JobDefinitionId, r.JobDefinition.Name, r.JobDefinition.TimeoutMinutes, r.LastHeartbeatUtc ?? r.StartedTimestamp ?? r.CreatedTimestamp))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var timedOut = new List<DeadJobTimeout>();
        foreach (var candidate in candidates) {
            var timeoutMinutes = candidate.TimeoutMinutes > 0 ? candidate.TimeoutMinutes : orphanCeiling;
            if (timeoutMinutes <= 0)
                continue;

            if (now < candidate.Baseline.AddMinutes(timeoutMinutes))
                continue;

            // The baseline cutoff is computed here (not in SQL) so the predicate stays translatable, while still re-reading the run's
            // current heartbeat when the update runs.
            var cutoff = now.AddMinutes(-timeoutMinutes);
            var rows = await db.JobRuns
                .Where(r => r.Id == candidate.RunId && (r.State == JobState.Running || r.State == JobState.Cancelling) &&
                    (r.LastHeartbeatUtc ?? r.StartedTimestamp ?? r.CreatedTimestamp) <= cutoff)
                .ExecuteUpdateAsync(
                    s => s
                        .SetProperty(r => r.State, JobState.Finished)
                        .SetProperty(r => r.Result, JobRunResult.Timeout)
                        .SetProperty(r => r.FinishedTimestamp, now)
                        .SetProperty(r => r.UpdatedTimestamp, now), ct)
                .ConfigureAwait(false);

            if (rows == 0) {
                // The worker heartbeated or finished in the meantime, so leave it alone.
                _logger.LogDebug("Skipped timing out run {RunId}: it is no longer overdue", candidate.RunId);
                continue;
            }

            _logger.LogWarning(
                "Job run {RunId} (definition {DefinitionName}) has not sent a heartbeat since {LastActivity:u} (timeout {TimeoutMinutes} min) — marking as failed",
                candidate.RunId, candidate.DefinitionName, candidate.Baseline, timeoutMinutes);

            timedOut.Add(new(candidate.RunId, candidate.JobDefinitionId, candidate.DefinitionName));
        }

        if (timedOut.Count > 0)
            _metrics.IncrementCounter(Constants.Metrics.Maintenance.DeadJobsFailed, timedOut.Count);

        return timedOut;
    }

    /// <summary>A <c>Running</c>/<c>Cancelling</c> run considered for heartbeat timeout, projected without tracking.</summary>
    private sealed record DeadJobCandidate(Guid RunId, Guid JobDefinitionId, string DefinitionName, int TimeoutMinutes, DateTime Baseline);

    /// <summary>A run this maintenance pass moved to <c>Finished / Timeout</c>.</summary>
    private sealed record DeadJobTimeout(Guid RunId, Guid JobDefinitionId, string DefinitionName);

    /// <summary>An SLA breach found by this maintenance pass, used for post-commit alert publication.</summary>
    private sealed record SlaBreach(Guid RunId, Guid JobDefinitionId, string DefinitionName, string Reason);

    /// <summary>
    /// Flags runs that have exceeded their start or duration SLA. Returns the breaches so the caller can publish alerts after the commit. Previously maintenance marked
    /// <c>SlaBreached</c> silently while <see cref="JobService" /> published alerts on the start/finish paths, so breaches only maintenance noticed never reached operators.
    /// </summary>
    private async Task<IReadOnlyList<SlaBreach>> CheckSlaBreachesAsync(JobContext db, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var candidates = await db.JobRuns.Include(r => r.JobDefinition)
            .Where(r => !r.SlaBreached && (r.State == JobState.Running || r.State == JobState.Cancelling || r.State == JobState.Queued))
            .OrderBy(r => r.CreatedTimestamp)
            .Take(_options.ActiveRunScanBatchSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var breaches = new List<SlaBreach>();
        foreach (var run in candidates) {
            var def = run.JobDefinition;
            string reason;
            if (run.State is JobState.Running or JobState.Cancelling) {
                if (def.ExpectedDurationMinutes <= 0 || !run.StartedTimestamp.HasValue)
                    continue;

                if (run.StartedTimestamp.Value.AddMinutes(def.ExpectedDurationMinutes) >= now)
                    continue;

                reason = $"Run exceeded ExpectedDuration SLA ({def.ExpectedDurationMinutes} min)";
            }
            else {
                if (def.MustStartByMinutes <= 0)
                    continue;

                if (run.CreatedTimestamp.AddMinutes(def.MustStartByMinutes) >= now)
                    continue;

                reason = $"Run exceeded MustStartBy SLA ({def.MustStartByMinutes} min)";
            }

            _logger.LogWarning("SLA breach detected for run {RunId} (definition {DefinitionName}, state {State})", run.Id, def.Name, run.State);
            run.SlaBreached = true;
            breaches.Add(new(run.Id, run.JobDefinitionId, def.Name, reason));
        }

        if (breaches.Count > 0)
            _metrics.IncrementCounter(Constants.Metrics.Sla.Breach, breaches.Count);

        return breaches;
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
    /// dispatches whose owner crashed before publishing). Re-publishes the run-created message. Duplicate deliveries are harmless because <c>StartedJobRun</c> only transitions
    /// <c>Queued -&gt; Running</c> once.
    /// </summary>
    /// <summary>Finds the stuck runs to redispatch. Only the scan happens here. The publish runs after the commit so consumers cannot observe pre-commit state.</summary>
    private async Task<IReadOnlyList<StuckQueuedRun>> FindStuckQueuedRunsAsync(JobContext db, CancellationToken ct)
    {
        if (_options.QueuedRunRedispatchMinutes <= 0 || !_eventPublisher.IsConnected())
            return [];

        var now = DateTime.UtcNow;
        var cutoff = now.AddMinutes(-_options.QueuedRunRedispatchMinutes);

        // Two triggers: (a) a run with a slot that has come due but was last touched before the slot (delayed retry not yet dispatched);
        // (b) any due queued run left untouched longer than the threshold (lost dispatch).
        return await db.JobRuns.AsNoTracking()
            .Where(r => r.State == JobState.Queued && !r.DryRun &&
                ((r.ScheduledSlotUtc != null && r.ScheduledSlotUtc <= now && (r.UpdatedTimestamp ?? r.CreatedTimestamp) < r.ScheduledSlotUtc) ||
                    ((r.ScheduledSlotUtc == null || r.ScheduledSlotUtc <= now) && (r.UpdatedTimestamp ?? r.CreatedTimestamp) < cutoff)))
            .OrderBy(r => r.CreatedTimestamp)
            .Take(_options.QueuedRunRedispatchBatchSize)
            .Select(r => new StuckQueuedRun(r.Id, r.JobDefinition.WorkerType, r.Priority))
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    private async Task RedispatchStuckQueuedRunsAsync(IReadOnlyList<StuckQueuedRun> candidates, CancellationToken ct)
    {
        if (candidates.Count == 0 || !_eventPublisher.IsConnected())
            return;

        await using var db = await _dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var now = DateTime.UtcNow;
        var redispatched = 0;
        foreach (var run in candidates) {
            try {
                await _eventPublisher.PublishRunCreatedAsync(run.RunId, run.WorkerType, run.Priority, ct).ConfigureAwait(false);

                // Bump UpdatedTimestamp so a stuck run is retried once per threshold window, not on every tick. Guarded on Queued so a run
                // a worker picked up in the meantime is left alone.
                await db.JobRuns.Where(r => r.Id == run.RunId && r.State == JobState.Queued)
                    .ExecuteUpdateAsync(s => s.SetProperty(r => r.UpdatedTimestamp, now), ct)
                    .ConfigureAwait(false);

                redispatched++;
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to redispatch stuck queued run {RunId}", run.RunId);
            }
        }

        if (redispatched > 0) {
            _metrics.IncrementCounter(Constants.Metrics.Maintenance.RunsRedispatched, redispatched);
            _logger.LogInformation("Redispatched {Count} stuck queued job run(s)", redispatched);
        }
    }

    /// <summary>A due <c>Queued</c> run whose dispatch message looks like it was lost.</summary>
    private sealed record StuckQueuedRun(Guid RunId, string WorkerType, int Priority);

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
    /// Deletes finished runs older than the effective retention (per-definition <c>RetentionDays</c>, falling back to the global default) in batches. Uses its own DbContext so
    /// batched deletes commit independently of the main maintenance pass.
    /// </summary>
    private async Task PurgeExpiredRunsAsync(CancellationToken ct)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var now = DateTime.UtcNow;
        var defaults = _options.DefaultRetentionDays;

        // Definitions with an explicit retention always purge. Others only when a global default is configured.
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
            var expiredQuery = db.JobRuns.Where(r => r.JobDefinitionId == def.Id && r.State == JobState.Finished && r.FinishedTimestamp != null && r.FinishedTimestamp < cutoff)
                .OrderBy(r => r.FinishedTimestamp)
                .ThenBy(r => r.Id)
                .Take(budget);

            var expired = await JobRunDependents.IncludeDependents(expiredQuery).ToListAsync(ct).ConfigureAwait(false);
            if (expired.Count == 0)
                continue;

            foreach (var run in expired)
                JobRunDependents.Remove(db, run);

            // Detach workflow run steps that reference purged runs. Unlike the delete endpoint, which removes the steps entirely on an
            // explicit user delete, retention purge preserves workflow history and only nulls the run reference.
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
        await QueryCacheInvalidation.InvalidateQueryCachesForBroadEntityTypeAsync<JobRun, JobDefinition>(_cache).ConfigureAwait(false);
    }
}