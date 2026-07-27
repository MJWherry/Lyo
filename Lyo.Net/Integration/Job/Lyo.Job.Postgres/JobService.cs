using System.Diagnostics;
using System.Text.RegularExpressions;
using Lyo.Api.Mapping;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Common.Response;
using Lyo.Api.Models.Error;
using Lyo.Api.Services.Crud.Create;
using Lyo.Api.Services.Crud.Read.Query;
using Lyo.Api.Services.Crud.Update;
using Lyo.Cache;
using Lyo.Common.Conversion;
using Lyo.Common.Identifiers;
using Lyo.Exceptions;
using Lyo.Job.Models;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Events;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Job.Models.Security;
using Lyo.Job.Postgres.Database;
using Lyo.Metrics;
using Lyo.Scheduler;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using ApiErrCodes = Lyo.Api.Models.Constants.ApiErrorCodes;
using Constants = Lyo.Job.Models.Constants;
using JobRun = Lyo.Job.Postgres.Database.JobRun;
using JobRunLog = Lyo.Job.Postgres.Database.JobRunLog;
using JobRunResult = Lyo.Job.Models.Enums.JobRunResult;

namespace Lyo.Job.Postgres;

public class JobService(
    ILogger<JobService> logger,
    IQueryService<JobContext> queryService,
    ICreateService<JobContext> createService,
    IPatchService<JobContext> patchService,
    ILyoMapper mapper,
    IJobEventPublisher eventPublisher,
    IDbContextFactory<JobContext> dbFactory,
    IHttpContextAccessor? httpContextAccessor = null,
    IMetrics? metrics = null,
    IJobParameterEncryptionService? parameterEncryption = null,
    ICacheService? cache = null,
    CacheOptions? cacheOptions = null)
{
    private readonly IMetrics _metrics = metrics ?? NullMetrics.Instance;
    private readonly IJobParameterEncryptionService? _parameterEncryption = parameterEncryption;

    /// <summary>
    /// The CAS transitions below use <c>ExecuteUpdateAsync</c>, which bypasses the CRUD services (and therefore their query-cache invalidation), so any cached
    /// <see cref="JobRun" /> GET entries must be busted manually before re-reading through <c>queryService</c>.
    /// </summary>
    private Task InvalidateRunCacheAsync(Guid jobRunId)
        => cache is null
            ? Task.CompletedTask
            : QueryCacheInvalidation.InvalidateQueryCachesForEntityKeysAsync(cache, cacheOptions ?? new CacheOptions(), typeof(JobRun), [new object?[] { jobRunId }]);

    public async Task<CreateResult<JobRunLogRes>> Log(Guid jobRunId, JobRunLogReq request)
        => await createService.CreateAsync<JobRunLogReq, JobRunLog, JobRunLogRes>(
                request, ctx => {
                    ctx.Entity.Id = LyoGuid.CreateCombPostgres();
                    ctx.Entity.JobRunId = jobRunId;
                })
            .ConfigureAwait(false);

    public async Task<CreateResult<JobRunRes>> CreateJobRun(JobRunReq request, CancellationToken ct = default)
    {
        using var activity = JobTracing.StartCreateRun(request.JobDefinitionId);
        var validationError = await ValidateRunParametersAsync(request, ct).ConfigureAwait(false);
        if (validationError is not null) {
            _metrics.IncrementCounter(Constants.Metrics.Service.RunCreateRejected, tags: [("reason", "invalid_parameters")]);
            return ResultFactory.CreateFailure<JobRunRes>(validationError);
        }

        if (request.DryRun)
            return ResultFactory.CreateSuccess(await BuildDryRunResponseAsync(request, ct).ConfigureAwait(false));

        // Dispatch is suppressed when the caller asked for it explicitly, or when the run targets a future slot (delayed retry). The caller
        // (scheduler delayed-MQ envelope, workflow engine) or the maintenance service's stuck-queued recovery performs the eventual dispatch.
        var suppressDispatch = request.SuppressDispatch || (request.ScheduledSlotUtc.HasValue && request.ScheduledSlotUtc.Value > DateTime.UtcNow);
        if (!suppressDispatch && !eventPublisher.IsConnected()) {
            _metrics.IncrementCounter(Constants.Metrics.Service.RunCreateRejected, tags: [("reason", "mq_disconnected")]);
            return ResultFactory.CreateFailure<JobRunRes>(LogAndReturnApiError("Could not connect to Message Queue Service", ApiErrCodes.MessageQueueConnectionIssue));
        }

        // Concurrency enforcement + creation are serialized per definition via a transaction-scoped Postgres advisory lock, so two concurrent
        // CreateJobRun calls cannot both pass the MaxConcurrentRuns check (the previous read-then-write was racy). The lock is released when
        // the transaction commits/disposes.
        await using var guardDb = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using var guardTx = await guardDb.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        var lockKey = BitConverter.ToInt64(request.JobDefinitionId.ToByteArray(), 0);
        await guardDb.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock({0})", [lockKey], ct).ConfigureAwait(false);

        var def = await guardDb.JobDefinitions.AsNoTracking().FirstOrDefaultAsync(d => d.Id == request.JobDefinitionId, ct).ConfigureAwait(false);
        if (def is null) {
            _metrics.IncrementCounter(Constants.Metrics.Service.RunCreateRejected, tags: [("reason", "definition_not_found")]);
            return ResultFactory.CreateFailure<JobRunRes>(LogAndReturnApiError("Job definition not found", ApiErrCodes.NotFound));
        }

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey)) {
            var existing = await FindRunByIdempotencyKeyAsync(guardDb, request.JobDefinitionId, request.IdempotencyKey, ct).ConfigureAwait(false);
            if (existing is not null) {
                await guardTx.CommitAsync(CancellationToken.None).ConfigureAwait(false);
                return ResultFactory.CreateSuccess(existing);
            }
        }

        if (def.MaxRunsPerHour > 0) {
            var hourAgo = DateTime.UtcNow.AddHours(-1);
            var recentCount = await guardDb.JobRuns
                .CountAsync(r => r.JobDefinitionId == request.JobDefinitionId && r.CreatedTimestamp >= hourAgo, ct)
                .ConfigureAwait(false);

            if (recentCount >= def.MaxRunsPerHour) {
                _metrics.IncrementCounter(Constants.Metrics.Service.RunCreateRejected, tags: [("reason", "max_runs_per_hour")]);
                return ResultFactory.CreateFailure<JobRunRes>(
                    LogAndReturnApiError($"Job definition has reached its hourly run limit ({def.MaxRunsPerHour}).", ApiErrCodes.InvalidRequest));
            }
        }

        if (def.MaxConcurrentRuns > 0) {
            var activeCount = await guardDb.JobRuns
                .CountAsync(r => r.JobDefinitionId == request.JobDefinitionId && (r.State == JobState.Queued || r.State == JobState.Running), ct)
                .ConfigureAwait(false);

            if (activeCount >= def.MaxConcurrentRuns) {
                _metrics.IncrementCounter(Constants.Metrics.Service.RunCreateRejected, tags: [("reason", "max_concurrent_runs")]);
                return ResultFactory.CreateFailure<JobRunRes>(
                    LogAndReturnApiError($"Job definition has reached its concurrent run limit ({def.MaxConcurrentRuns}).", ApiErrCodes.InvalidRequest));
            }
        }

        var traceId = request.TraceId ?? Activity.Current?.TraceId.ToString();
        var defParams = await guardDb.JobParameters.AsNoTracking()
            .Where(p => p.JobDefinitionId == request.JobDefinitionId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        CreateResult<JobRunRes> result;
        try {
            result = await createService.CreateAsync<JobRunReq, JobRun, JobRunRes>(
                    request, ctx => {
                        ctx.Entity.Id = LyoGuid.CreateCombPostgres();
                        ctx.Entity.State = JobState.Queued;
                        ctx.Entity.CreatedTimestamp = DateTime.UtcNow;
                        ctx.Entity.Priority = request.Priority ?? def.Priority;
                        ctx.Entity.DefinitionAuditVersion = def.DefinitionVersion;
                        ctx.Entity.TraceId = traceId;
                        foreach (var j in ctx.Entity.JobRunParameters)
                            j.Id = LyoGuid.CreateCombPostgres();

                        EncryptRunParameters(ctx.Entity.JobRunParameters, defParams);
                    }, ctx => {
                        ctx.DbContext.Entry(ctx.Entity).Navigation("JobDefinition").Load();
                    }, ct: ct)
                .ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505" } pgEx) {
            if (pgEx.ConstraintName == "ix_job_run_schedule_slot_unique") {
                logger.LogInformation(
                    "Duplicate job run for schedule {ScheduleId} slot {Slot:u} suppressed (constraint {Constraint})", request.JobScheduleId, request.ScheduledSlotUtc,
                    pgEx.ConstraintName);

                _metrics.IncrementCounter(Constants.Metrics.Service.RunCreateRejected, tags: [("reason", "duplicate_slot")]);
                return ResultFactory.CreateFailure<JobRunRes>(LogAndReturnApiError("A job run already exists for this scheduled slot.", ApiErrCodes.Conflict));
            }

            if (pgEx.ConstraintName == "ix_job_run_idempotency_key_unique" && !string.IsNullOrWhiteSpace(request.IdempotencyKey)) {
                // No commit here — the finally block commits the guard transaction exactly once.
                var existing = await FindRunByIdempotencyKeyAsync(guardDb, request.JobDefinitionId, request.IdempotencyKey, ct).ConfigureAwait(false);
                if (existing is not null)
                    return ResultFactory.CreateSuccess(existing);
            }

            throw;
        }
        finally {
            // Release the advisory lock as soon as the create attempt is over (commit of an otherwise-empty transaction).
            await guardTx.CommitAsync(CancellationToken.None).ConfigureAwait(false);
        }

        if (!result.IsSuccess)
            return result;

        activity?.SetTag("job.run.id", result.Data!.Id);
        _metrics.IncrementCounter(Constants.Metrics.Service.RunCreated, tags: [("definition", result.Data!.JobDefinition?.Name ?? "unknown")]);
        if (suppressDispatch) {
            logger.LogDebug("Dispatch suppressed for run {RunId} (SuppressDispatch={Suppress}, ScheduledSlotUtc={Slot:u})",
                result.Data!.Id, request.SuppressDispatch, request.ScheduledSlotUtc);

            return ResultFactory.CreateSuccess(MaskRunResponse(result.Data!));
        }

        var notified = await TryPublishAsync(
                () => eventPublisher.PublishRunCreatedAsync(result.Data!.Id, result.Data!.JobDefinition!.WorkerType, result.Data!.Priority, ct),
                "Failed to publish run {RunId} created", result.Data!.Id)
            .ConfigureAwait(false);

        if (!notified) {
            // The run is already persisted as Queued; the maintenance service's stuck-queued recovery will redispatch it, so the create still succeeds.
            _metrics.IncrementCounter(Constants.Metrics.Service.RunDispatchDeferred);
            logger.LogWarning("Run {RunId} was created but the dispatch publish failed; maintenance will redispatch it", result.Data!.Id);
        }

        return ResultFactory.CreateSuccess(MaskRunResponse(result.Data!));
    }

    /// <summary>Creates child runs for a parent batch, stamping batch metadata on each request.</summary>
    public async Task<IReadOnlyList<JobRunRes>> CreateChildRunsAsync(Guid parentRunId, JobCreateChildRunsReq request, CancellationToken ct = default)
    {
        // Build the template from the entity (not the masked API response) so encrypted parameter values survive, and so the parent's
        // IdempotencyKey/ScheduledSlotUtc are not copied onto children (a copied key would silently return the parent as every "child").
        var template = await BuildRunRequestFromEntityAsync(parentRunId, ct).ConfigureAwait(false);
        if (template is null)
            throw new NotFoundException($"Parent job run {parentRunId} was not found.");

        var children = request.Children.Select(child => {
            var req = CloneRunRequest(template);
            req.AllowTriggers = false;
            req.BatchIndex = child.BatchIndex;
            if (child.Parameters.Count > 0) {
                req.JobRunParameters.Clear();
                req.JobRunParameters.AddRange(child.Parameters);
            }

            return req;
        }).ToList();

        return await CreateChildRunsAsync(parentRunId, children, ct).ConfigureAwait(false);
    }

    /// <summary>Creates child runs for a parent batch, stamping batch metadata on each request.</summary>
    public async Task<IReadOnlyList<JobRunRes>> CreateChildRunsAsync(Guid parentRunId, IReadOnlyList<JobRunReq> children, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (!await db.JobRuns.AnyAsync(r => r.Id == parentRunId, ct).ConfigureAwait(false))
            throw new NotFoundException($"Parent job run {parentRunId} was not found.");

        var batchTotal = children.Count;
        var results = new List<JobRunRes>(batchTotal);
        for (var i = 0; i < children.Count; i++) {
            var child = children[i];
            child.ParentJobRunId = parentRunId;
            child.BatchIndex = i;
            child.BatchTotal = batchTotal;

            var created = await CreateJobRun(child, ct).ConfigureAwait(false);
            if (!created.IsSuccess)
                throw new InvalidOperationException(created.Error?.Detail ?? "Failed to create child job run.");

            results.Add(created.Data!);
        }

        return results;
    }

    /// <summary>Returns the next scheduled run times for a definition, merged across all enabled schedules.</summary>
    public async Task<IReadOnlyList<DateTime>> GetNextRuns(Guid definitionId, int count = 20, CancellationToken ct = default)
    {
        if (count <= 0)
            return [];

        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var schedules = await db.JobSchedules.AsNoTracking()
            .Include(s => s.JobBlackoutCalendar!)
            .ThenInclude(c => c.JobBlackoutWindows)
            .Where(s => s.JobDefinitionId == definitionId && s.Enabled)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (schedules.Count == 0)
            return [];

        var perSchedule = Math.Max(count, (int)Math.Ceiling(count / (double)schedules.Count));
        var merged = new List<DateTime>();
        foreach (var schedule in schedules) {
            var definition = schedule.ToScheduleDefinition();
            foreach (var runAt in ScheduleCalculator.GetNextRuns(definition, maxCount: perSchedule)) {
                if (!schedule.IsWithinScheduleWindow(runAt) || !schedule.IsAllowedByBlackoutCalendar(runAt))
                    continue;

                merged.Add(runAt);
            }
        }

        return merged.OrderBy(t => t).Distinct().Take(count).ToList();
    }

    /// <summary>Updates heartbeat and optional progress fields on a running job.</summary>
    public async Task<(JobRunRes? Result, LyoProblemDetails? Error)> HeartbeatJobRun(Guid jobRunId, JobRunHeartbeatReq? request, CancellationToken ct = default)
    {
        var existing = await queryService.Get<JobRun, JobRunRes>([jobRunId], null).ConfigureAwait(false);
        if (existing is null)
            return (null, LogAndReturnApiError("Job run not found", ApiErrCodes.NotFound));

        if (existing.State is not (JobState.Running or JobState.Cancelling))
            return (null, LogAndReturnApiError("Job is not in a heartbeat-eligible state (must be Running or Cancelling)", ApiErrCodes.InvalidRequest));

        var patchRequest = PatchRequestBuilder.ForId(jobRunId).SetProperty("LastHeartbeatUtc", DateTime.UtcNow);
        if (request?.ProgressPercent is int percent)
            patchRequest.SetProperty("ProgressPercent", percent);

        if (request?.ProgressMessage is not null)
            patchRequest.SetProperty("ProgressMessage", request.ProgressMessage);

        var result = await patchService.PatchAsync<JobRun, JobRunRes>(patchRequest.Build(), ct: ct).ConfigureAwait(false);
        return !result.IsSuccess
            ? (null, LogAndReturnApiError("Failed to patch job heartbeat", ApiErrCodes.InvalidPatchRequest))
            : (result.NewData, null);
    }

    /// <summary>
    /// Transitions a run from <c>Queued</c> to <c>Running</c> using an atomic compare-and-swap. Any other state (already <c>Running</c> from a duplicate delivery,
    /// <c>Cancelling</c> from a queued-run cancel, or <c>Finished</c>) is rejected so redelivered dispatch messages never execute a run twice. This is a worker-trusted
    /// endpoint: the returned run has encrypted parameter values decrypted so workers can execute with real values (all other read endpoints stay masked).
    /// </summary>
    public async Task<(JobRunRes? Result, LyoProblemDetails? Error)> StartedJobRun(Guid jobRunId)
    {
        using var activity = JobTracing.StartRun(jobRunId);
        if (!eventPublisher.IsConnected())
            return (null, LogAndReturnApiError("Could not connect to Message Queue Service", ApiErrCodes.MessageQueueConnectionIssue));

        var existing = await queryService.Get<JobRun, JobRunRes>([jobRunId], ["JobRunParameters", "JobDefinition"]).ConfigureAwait(false);
        if (existing is null)
            return (null, LogAndReturnApiError("Job run not found", ApiErrCodes.NotFound));

        var startedAt = DateTime.UtcNow;
        var slaBreached = CheckStartSla(existing, startedAt);

        // Compare-and-swap: only a Queued run may start. The WHERE clause makes the transition atomic, so a redelivered dispatch message
        // (or a second worker instance) loses the race instead of double-executing, and a Cancelling queued run is never resurrected.
        await using var db = await dbFactory.CreateDbContextAsync().ConfigureAwait(false);
        var updatedRows = await db.JobRuns
            .Where(r => r.Id == jobRunId && r.State == JobState.Queued)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.State, JobState.Running)
                .SetProperty(r => r.StartedTimestamp, startedAt)
                .SetProperty(r => r.UpdatedTimestamp, startedAt)
                .SetProperty(r => r.SlaBreached, r => r.SlaBreached || slaBreached))
            .ConfigureAwait(false);

        if (updatedRows == 0) {
            _metrics.IncrementCounter(Constants.Metrics.Service.RunStartRejected);
            return (null, LogAndReturnApiError(
                $"Job run cannot start: it is not in the Queued state (current state may be Running, Cancelling, or Finished). Run {jobRunId}.", ApiErrCodes.InvalidRequest));
        }

        if (slaBreached)
            await PublishSlaAlertAsync(existing.JobDefinitionId, jobRunId, existing.JobDefinition?.Name, "Run exceeded MustStartBy SLA").ConfigureAwait(false);

        _metrics.IncrementCounter(Constants.Metrics.Service.RunStarted);
        _metrics.RecordTiming(Constants.Metrics.Service.RunQueueLatency, startedAt - existing.CreatedTimestamp);
        var notified = await TryPublishAsync(() => eventPublisher.PublishRunStartedAsync(jobRunId), "Failed to publish run {RunId} started", jobRunId).ConfigureAwait(false);
        if (!notified)
            return (null, LogAndReturnApiError("Could not notify to start job", ApiErrCodes.MessageQueueConnectionIssue));

        // Re-fetch with includes so workers get a fully-loaded run (the CAS update does not return the entity).
        await InvalidateRunCacheAsync(jobRunId).ConfigureAwait(false);
        var savedResult = await queryService.Get<JobRun, JobRunRes>(
                [jobRunId],
                ["JobRunParameters", "JobRunResults", "JobDefinition", "JobDefinition.JobParameters"])
            .ConfigureAwait(false);

        // Worker-trusted path: decrypt parameter values (the mapper masks them for every other endpoint).
        return (await DecryptRunParametersAsync(savedResult!).ConfigureAwait(false), null);
    }

    public async Task<(JobRunRes? Result, LyoProblemDetails? Error)> CancelJobRun(Guid jobRunId)
    {
        if (!eventPublisher.IsConnected())
            return (null, LogAndReturnApiError("Could not connect to Message Queue Service", ApiErrCodes.MessageQueueConnectionIssue));

        var existing = await queryService.Get<JobRun, JobRunRes>([jobRunId], ["JobRunParameters"]).ConfigureAwait(false);
        if (existing is null)
            return (null, LogAndReturnApiError("Job run not found", ApiErrCodes.NotFound));

        if (existing.State is not (JobState.Running or JobState.Queued))
            return (null, LogAndReturnApiError("Job is not in a cancellable state (must be Running or Queued)", ApiErrCodes.InvalidRequest));

        if (existing.State == JobState.Queued) {
            // A queued run has no worker to confirm the cancel (and StartedJobRun's CAS guard will reject its dispatch message), so finalize
            // it directly. The CAS keeps this race-safe: if a worker started the run in the meantime the update misses and we fall through
            // to the Cancelling path below.
            var now = DateTime.UtcNow;
            await using var db = await dbFactory.CreateDbContextAsync().ConfigureAwait(false);
            var cancelledRows = await db.JobRuns
                .Where(r => r.Id == jobRunId && r.State == JobState.Queued)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.State, JobState.Finished)
                    .SetProperty(r => r.Result, Models.Enums.JobRunResult.Cancelled)
                    .SetProperty(r => r.FinishedTimestamp, now)
                    .SetProperty(r => r.UpdatedTimestamp, now))
                .ConfigureAwait(false);

            if (cancelledRows > 0) {
                _metrics.IncrementCounter(Constants.Metrics.Service.RunCancelled);
                await TryPublishAsync(() => eventPublisher.PublishRunCancelledAsync(jobRunId), "Failed to publish run {RunId} cancelled", jobRunId).ConfigureAwait(false);
                await TryPublishAsync(() => eventPublisher.PublishRunFinishedAsync(jobRunId), "Failed to publish run {RunId} finished", jobRunId).ConfigureAwait(false);
                await InvalidateRunCacheAsync(jobRunId).ConfigureAwait(false);
                var cancelled = await queryService.Get<JobRun, JobRunRes>([jobRunId], ["JobRunParameters"]).ConfigureAwait(false);
                return (MaskRunResponse(cancelled!), null);
            }
        }

        // Transition to Cancelling so callers can poll the state until the worker confirms.
        var patchRequest = PatchRequestBuilder.ForId(jobRunId).SetProperty("State", JobState.Cancelling).Build();
        var patched = await patchService.PatchAsync<JobRun, JobRunRes>(patchRequest).ConfigureAwait(false);
        if (!patched.IsSuccess)
            return (null, LogAndReturnApiError("Failed to update job state to Cancelling", ApiErrCodes.InvalidPatchRequest));

        _metrics.IncrementCounter(Constants.Metrics.Service.RunCancelled);
        var notified = await TryPublishAsync(() => eventPublisher.PublishRunCancelledAsync(jobRunId), "Failed to publish run {RunId} cancelled", jobRunId).ConfigureAwait(false);
        if (!notified)
            return (null, LogAndReturnApiError("Could not notify to cancel job", ApiErrCodes.MessageQueueConnectionIssue));

        return (patched.NewData, null);
    }

    /// <summary>
    /// Transitions a run from <c>Running</c> back to <c>Queued</c> using an atomic compare-and-swap. Used by workers during graceful host shutdown to hand the run back for
    /// redelivery instead of terminally cancelling it. <c>Cancelling</c> runs are intentionally rejected — a pending user cancellation must not be forgotten by a restart.
    /// </summary>
    public async Task<(JobRunRes? Result, LyoProblemDetails? Error)> RequeueJobRun(Guid jobRunId)
    {
        var existing = await queryService.Get<JobRun, JobRunRes>([jobRunId], null).ConfigureAwait(false);
        if (existing is null)
            return (null, LogAndReturnApiError("Job run not found", ApiErrCodes.NotFound));

        var now = DateTime.UtcNow;
        await using var db = await dbFactory.CreateDbContextAsync().ConfigureAwait(false);
        var updatedRows = await db.JobRuns
            .Where(r => r.Id == jobRunId && r.State == JobState.Running)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.State, JobState.Queued)
                .SetProperty(r => r.StartedTimestamp, (DateTime?)null)
                .SetProperty(r => r.LastHeartbeatUtc, (DateTime?)null)
                .SetProperty(r => r.UpdatedTimestamp, now))
            .ConfigureAwait(false);

        if (updatedRows == 0)
            return (null, LogAndReturnApiError($"Job run cannot be requeued: it is not in the Running state. Run {jobRunId}.", ApiErrCodes.InvalidRequest));

        _metrics.IncrementCounter(Constants.Metrics.Service.RunRequeued);
        logger.LogInformation("Run {RunId} requeued (worker shutdown hand-back)", jobRunId);
        await InvalidateRunCacheAsync(jobRunId).ConfigureAwait(false);
        var saved = await queryService.Get<JobRun, JobRunRes>([jobRunId], null).ConfigureAwait(false);
        return (MaskRunResponse(saved!), null);
    }

    public async Task<(JobRunRes? Result, LyoProblemDetails? Error)> FinishedJobRun(Guid jobRunId, IReadOnlyList<JobRunResultReq> results)
    {
        using var activity = JobTracing.FinishRun(jobRunId);
        if (!eventPublisher.IsConnected())
            return (null, LogAndReturnApiError("Could not connect to Message Queue Service", ApiErrCodes.MessageQueueConnectionIssue));

        var existing = await queryService.Get<JobRun, JobRunRes>([jobRunId], ["JobRunParameters", "JobDefinition"]).ConfigureAwait(false);
        if (existing is null)
            return (null, LogAndReturnApiError("Job run not found", ApiErrCodes.NotFound));

        if (existing.State is not (JobState.Running or JobState.Cancelling))
            return (null, LogAndReturnApiError("Job is not in a finishable state (must be Running or Cancelling)", ApiErrCodes.InvalidRequest));

        var resultStr = results.FirstOrDefault(i => i.Key == Constants.Data.JobRunResultKey.Result)?.Value ?? nameof(JobRunResult.Unknown);
        var resultEnum = TypeConversion.EnumOrDefault(resultStr, JobRunResult.Unknown);
        var finishedAt = DateTime.UtcNow;
        var durationSlaBreached = CheckDurationSla(existing, finishedAt);
        var request = PatchRequestBuilder.ForId(jobRunId)
            .SetProperty("State", JobState.Finished)
            .SetProperty("FinishedTimestamp", finishedAt)
            .SetProperty("Result", resultEnum);

        if (durationSlaBreached || existing.SlaBreached)
            request.SetProperty("SlaBreached", true);

        var patchRequest = request.Build();

        var result = await patchService.PatchAsync<JobRun, JobRunRes>(
                patchRequest, ctx => {
                    foreach (var res in results) {
                        var r = mapper.Map<Database.JobRunResult>(res);
                        r.Id = LyoGuid.CreateCombPostgres();
                        r.JobRunId = jobRunId;
                        ctx.DbContext.JobRunResults.Add(r);
                    }
                })
            .ConfigureAwait(false);

        if (!result.IsSuccess)
            return (null, LogAndReturnApiError("Failed to patch finished job", ApiErrCodes.InvalidPatchRequest));

        _metrics.IncrementCounter(Constants.Metrics.Service.RunFinished, tags: [("result", resultEnum.ToString())]);
        if (existing.StartedTimestamp.HasValue)
            _metrics.RecordTiming(Constants.Metrics.Service.RunDuration, finishedAt - existing.StartedTimestamp.Value, [("result", resultEnum.ToString())]);

        if (durationSlaBreached)
            await PublishSlaAlertAsync(existing.JobDefinitionId, jobRunId, existing.JobDefinition?.Name, "Run exceeded ExpectedDuration SLA").ConfigureAwait(false);

        if (resultEnum == JobRunResult.Failure && existing.JobDefinition is { AlertOnFailure: true, AlertAfterConsecutiveFailures: 0 })
            await PublishFailureAlertAsync(existing.JobDefinitionId, jobRunId, existing.JobDefinition.Name).ConfigureAwait(false);

        var notified = await TryPublishAsync(() => eventPublisher.PublishRunFinishedAsync(jobRunId), "Failed to publish run {RunId} finished", jobRunId).ConfigureAwait(false);
        if (!notified)
            return (null, LogAndReturnApiError("Could not notify to finish job", ApiErrCodes.MessageQueueConnectionIssue));

        var savedResult = await queryService.Get<JobRun, JobRunRes>(
                [jobRunId],
                [
                    "JobRunResults", "JobRunLogs", "JobRunParameters", "JobTrigger", "JobSchedule", "JobDefinition", "JobDefinition.JobSchedules",
                    "JobDefinition.JobTriggerTriggersJobDefinitions"
                ])
            .ConfigureAwait(false);

        return (MaskRunResponse(savedResult!), null);
    }

    public async Task<CreateResult<JobRunRes>?> RerunJob(Guid jobRunId)
    {
        // Build the request from the entity (not the masked API response) so encrypted parameter values survive, and the original run's
        // IdempotencyKey/ScheduledSlotUtc are not copied (a copied key would violate the unique index and fail the rerun).
        var request = await BuildRunRequestFromEntityAsync(jobRunId).ConfigureAwait(false);
        if (request is null)
            return ResultFactory.CreateFailure<JobRunRes>(LogAndReturnApiError("Existing job not found", ApiErrCodes.NotFound));

        request.CreatedBy = httpContextAccessor?.HttpContext?.User.Identity?.Name ?? "Unknown";
        request.ReRanFromJobRunId = jobRunId;

        // Route through CreateJobRun so reruns get parameter validation, rate/concurrency limits, encryption, and the advisory lock.
        var result = await CreateJobRun(request).ConfigureAwait(false);
        if (result.IsSuccess)
            _metrics.IncrementCounter(Constants.Metrics.Service.RunRerun);

        return result;
    }

    /// <summary>
    /// Returns the latest run, latest successful run, and latest failed run per definition in a single call. Used by the scheduler's definition refresh so it does not need
    /// three HTTP queries per definition.
    /// </summary>
    public async Task<IReadOnlyList<JobDefinitionLatestRunsRes>> GetLatestRuns(IReadOnlyList<Guid> definitionIds, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var results = new List<JobDefinitionLatestRunsRes>(definitionIds.Count);
        foreach (var definitionId in definitionIds.Distinct()) {
            var lastRun = await QueryLatestRunAsync(db, definitionId, null, ct).ConfigureAwait(false);
            var lastSuccess = await QueryLatestRunAsync(db, definitionId, [JobRunResult.Success, JobRunResult.SuccessWithWarnings], ct).ConfigureAwait(false);
            var lastFailed = await QueryLatestRunAsync(db, definitionId, [JobRunResult.Failure], ct).ConfigureAwait(false);
            results.Add(new() {
                JobDefinitionId = definitionId,
                LastRun = lastRun,
                LastSuccessfulRun = lastSuccess,
                LastFailedRun = lastFailed
            });
        }

        return results;
    }

    private async Task<JobRunRes?> QueryLatestRunAsync(JobContext db, Guid definitionId, JobRunResult?[]? resultFilter, CancellationToken ct)
    {
        var query = db.JobRuns.AsNoTracking().Where(r => r.JobDefinitionId == definitionId);
        // The filter array is nullable-typed because EF cannot translate Contains over a non-nullable array against the nullable Result column.
        if (resultFilter is not null)
            query = query.Where(r => r.Result != null && resultFilter.Contains(r.Result));

        var entity = await query
            .OrderByDescending(r => r.CreatedTimestamp)
            .Include(r => r.JobRunParameters)
            .Include(r => r.JobRunResults)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return entity is null ? null : mapper.Map<JobRunRes>(entity);
    }

    /// <summary>
    /// Builds a <see cref="JobRunReq" /> from the stored run entity for rerun/child-run cloning. Reads raw parameter values (including ciphertext) instead of the masked API
    /// response, and deliberately does not copy <c>IdempotencyKey</c>, <c>ScheduledSlotUtc</c>, or lineage fields — those must be unique per run.
    /// </summary>
    private async Task<JobRunReq?> BuildRunRequestFromEntityAsync(Guid runId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var run = await db.JobRuns.AsNoTracking()
            .Include(r => r.JobRunParameters)
            .FirstOrDefaultAsync(r => r.Id == runId, ct)
            .ConfigureAwait(false);

        if (run is null)
            return null;

        var request = new JobRunReq {
            JobDefinitionId = run.JobDefinitionId,
            CreatedBy = run.CreatedBy,
            AllowTriggers = run.AllowTriggers,
            Priority = run.Priority,
            TraceId = null
        };

        foreach (var parameter in run.JobRunParameters)
            request.JobRunParameters.Add(BuildParameterRequest(parameter));

        return request;
    }

    private JobRunParameterReq BuildParameterRequest(JobRunParameter parameter)
    {
        var request = new JobRunParameterReq {
            Key = parameter.Key,
            Description = parameter.Description,
            Type = TypeConversion.EnumOrDefault(parameter.Type, JobParameterType.String),
            Value = parameter.Value,
            EncryptedValue = parameter.EncryptedValue,
            Enabled = true
        };

        if (_parameterEncryption?.UsesEncryptedStorage(parameter.EncryptedValue) != true)
            return request;

        // Stored value is ciphertext. Decrypt to plaintext and flag for re-encryption (empty marker) so CreateJobRun's encryption step
        // produces fresh valid ciphertext instead of double-encrypting the stored bytes. If decryption is unavailable, pass the
        // ciphertext through untouched — without an encryption service it is stored verbatim and stays decryptable later.
        var plaintext = _parameterEncryption.DecryptValue(parameter.EncryptedValue);
        if (plaintext is not null) {
            request.Value = plaintext;
            request.EncryptedValue = [];
        }

        return request;
    }

    private static JobRunReq CloneRunRequest(JobRunReq template)
    {
        var clone = new JobRunReq {
            JobDefinitionId = template.JobDefinitionId,
            CreatedBy = template.CreatedBy,
            AllowTriggers = template.AllowTriggers,
            Priority = template.Priority,
            TraceId = template.TraceId
        };

        foreach (var parameter in template.JobRunParameters) {
            clone.JobRunParameters.Add(new() {
                Key = parameter.Key,
                Description = parameter.Description,
                Type = parameter.Type,
                Value = parameter.Value,
                EncryptedValue = parameter.EncryptedValue,
                Enabled = parameter.Enabled
            });
        }

        return clone;
    }

    /// <summary>Replaces masked parameter values with decrypted plaintext for the worker-trusted Started response.</summary>
    private async Task<JobRunRes> DecryptRunParametersAsync(JobRunRes run, CancellationToken ct = default)
    {
        if (_parameterEncryption is null || run.JobRunParameters is null || run.JobRunParameters.Count == 0)
            return MaskRunResponse(run);

        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entities = await db.JobRunParameters.AsNoTracking().Where(p => p.JobRunId == run.Id).ToListAsync(ct).ConfigureAwait(false);
        var entitiesById = entities.ToDictionary(e => e.Id);
        var parameters = run.JobRunParameters.Select(p => {
            if (!entitiesById.TryGetValue(p.Id, out var entity) || !_parameterEncryption.UsesEncryptedStorage(entity.EncryptedValue))
                return p;

            var plaintext = _parameterEncryption.DecryptValue(entity.EncryptedValue);
            return plaintext is null ? p : p with { Value = plaintext, EncryptedValue = null };
        }).ToList();

        return run with { JobRunParameters = parameters };
    }

    /// <summary>Aggregates run statistics for a job definition over the last <paramref name="days" /> days. Returns null when the definition is not found.</summary>
    public async Task<JobDefinitionStatsRes?> GetDefinitionStats(Guid definitionId, int days = 30, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var def = await db.JobDefinitions.FindAsync([definitionId], ct).ConfigureAwait(false);
        if (def is null)
            return null;

        var runs = await db.JobRuns.Where(r => r.JobDefinitionId == definitionId && r.CreatedTimestamp >= since)
            .Select(r => new {
                r.Result,
                r.StartedTimestamp,
                r.FinishedTimestamp,
                r.CreatedTimestamp
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var total = runs.Count;
        var successCount = runs.Count(r => r.Result is JobRunResult.Success or JobRunResult.SuccessWithWarnings or JobRunResult.PartialSuccess);
        var failureCount = runs.Count(r => r.Result == JobRunResult.Failure);
        var durations = runs.Where(r => r.StartedTimestamp.HasValue && r.FinishedTimestamp.HasValue)
            .Select(r => (r.FinishedTimestamp!.Value - r.StartedTimestamp!.Value).TotalMilliseconds)
            .OrderBy(ms => ms)
            .ToList();

        double? avgMs = durations.Count > 0 ? durations.Average() : null;
        double? p95Ms = durations.Count >= 20 ? durations[(int)Math.Ceiling(durations.Count * 0.95) - 1] : null;

        // Count current consecutive failures from the most recent runs.
        var orderedResults = await db.JobRuns.Where(r => r.JobDefinitionId == definitionId && r.Result != null)
            .OrderByDescending(r => r.CreatedTimestamp)
            .Select(r => r.Result)
            .Take(100)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var consecutiveFailures = 0;
        foreach (var res in orderedResults) {
            if (res == JobRunResult.Failure)
                consecutiveFailures++;
            else
                break;
        }

        var lastRun = runs.Count > 0 ? runs.Max(r => r.CreatedTimestamp) : (DateTime?)null;
        var lastSuccess = runs.Where(r => r.Result is JobRunResult.Success or JobRunResult.SuccessWithWarnings or JobRunResult.PartialSuccess)
            .Select(r => r.FinishedTimestamp ?? r.CreatedTimestamp)
            .DefaultIfEmpty()
            .Max();

        return new() {
            JobDefinitionId = definitionId,
            TotalRuns = total,
            SuccessCount = successCount,
            FailureCount = failureCount,
            SuccessRate = total > 0 ? Math.Round(successCount * 100.0 / total, 2) : null,
            AvgDurationMs = avgMs.HasValue ? Math.Round(avgMs.Value, 2) : null,
            P95DurationMs = p95Ms.HasValue ? Math.Round(p95Ms.Value, 2) : null,
            LastRunAt = lastRun,
            LastSuccessAt = lastSuccess == default ? null : lastSuccess,
            ConsecutiveFailures = consecutiveFailures,
            WindowDays = days
        };
    }

    private async Task<JobRunRes> BuildDryRunResponseAsync(JobRunReq request, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var def = await db.JobDefinitions.AsNoTracking().FirstOrDefaultAsync(d => d.Id == request.JobDefinitionId, ct).ConfigureAwait(false);
        if (def is null)
            throw new NotFoundException($"Job definition {request.JobDefinitionId} was not found.");

        var now = DateTime.UtcNow;
        return new() {
            Id = Guid.Empty,
            State = JobState.Queued,
            CreatedTimestamp = now,
            AllowTriggers = request.AllowTriggers,
            JobDefinitionId = request.JobDefinitionId,
            JobDefinition = mapper.Map<JobDefinitionRes>(def),
            JobScheduleId = request.JobScheduleId,
            JobTriggerId = request.JobTriggerId,
            ScheduledSlotUtc = request.ScheduledSlotUtc,
            RetryAttempt = request.RetryAttempt,
            Priority = request.Priority ?? def.Priority,
            IdempotencyKey = request.IdempotencyKey,
            DryRun = true,
            TraceId = request.TraceId ?? Activity.Current?.TraceId.ToString(),
            ParentJobRunId = request.ParentJobRunId,
            BatchIndex = request.BatchIndex,
            BatchTotal = request.BatchTotal,
            DefinitionAuditVersion = def.DefinitionVersion,
            JobRunParameters = request.JobRunParameters
                .Select(p => new JobRunParameterRes(Guid.Empty, Guid.Empty, p.Key, p.Type, p.Value, p.Description, p.EncryptedValue, false))
                .ToList()
        };
    }

    private async Task<JobRunRes?> FindRunByIdempotencyKeyAsync(JobContext db, Guid definitionId, string idempotencyKey, CancellationToken ct)
    {
        var run = await db.JobRuns.AsNoTracking()
            .Include(r => r.JobDefinition)
            .Include(r => r.JobRunParameters)
            .FirstOrDefaultAsync(r => r.JobDefinitionId == definitionId && r.IdempotencyKey == idempotencyKey, ct)
            .ConfigureAwait(false);

        return run is null ? null : mapper.Map<JobRunRes>(run);
    }

    private async Task<LyoProblemDetails?> ValidateRunParametersAsync(JobRunReq request, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var defParams = await db.JobParameters.Where(p => p.JobDefinitionId == request.JobDefinitionId).ToListAsync(ct).ConfigureAwait(false);
        if (defParams.Count == 0)
            return null;

        var errors = new List<string>();
        var runParamsByKey = request.JobRunParameters.GroupBy(p => p.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        foreach (var defParam in defParams) {
            runParamsByKey.TryGetValue(defParam.Key, out var provided);

            // Required check.
            if (defParam.Required && (provided is null || provided.Count == 0 || provided.All(p => string.IsNullOrEmpty(p.Value)))) {
                errors.Add($"Parameter '{defParam.Key}' is required.");
                continue;
            }

            // Skip further validation if not provided and not required.
            if (provided is null || provided.Count == 0)
                continue;

            foreach (var runParam in provided) {
                var value = runParam.Value ?? string.Empty;
                if (defParam.MinLength.HasValue && value.Length < defParam.MinLength.Value)
                    errors.Add($"Parameter '{defParam.Key}' must be at least {defParam.MinLength} characters.");

                if (defParam.MaxLength.HasValue && value.Length > defParam.MaxLength.Value)
                    errors.Add($"Parameter '{defParam.Key}' must not exceed {defParam.MaxLength} characters.");

                if (!string.IsNullOrEmpty(defParam.ValidationRegex) && !Regex.IsMatch(value, defParam.ValidationRegex))
                    errors.Add($"Parameter '{defParam.Key}' does not match the required pattern.");

                if (!string.IsNullOrEmpty(defParam.AllowedValues)) {
                    var allowed = defParam.AllowedValues.Split('|', StringSplitOptions.RemoveEmptyEntries);
                    if (!allowed.Contains(value, StringComparer.OrdinalIgnoreCase))
                        errors.Add($"Parameter '{defParam.Key}' value '{value}' is not one of the allowed values: {string.Join(", ", allowed)}.");
                }
            }
        }

        if (errors.Count == 0)
            return null;

        return LyoProblemDetailsBuilder.CreateWithTrace(Activity.Current?.TraceId.ToString(), Activity.Current?.SpanId.ToString())
            .WithErrorCode(ApiErrCodes.InvalidRequest)
            .WithMessage("One or more job run parameters failed validation.")
            .AddErrors(errors.Select(e => new ApiError(ApiErrCodes.InvalidRequest, e)))
            .Build();
    }

    private LyoProblemDetails LogAndReturnApiError(string message, string code = ApiErrCodes.Unknown, LogLevel level = LogLevel.Warning)
    {
        logger.Log(level, message);
        return LyoProblemDetailsBuilder.CreateWithTrace(Activity.Current?.TraceId.ToString(), Activity.Current?.SpanId.ToString()).WithErrorCode(code).WithMessage(message).Build();
    }

    private async Task<bool> TryPublishAsync(Func<Task> publish, string errorTemplate, object? arg = null)
    {
        try {
            await publish().ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) {
            logger.LogError(ex, errorTemplate, arg);
            return false;
        }
    }

    private void EncryptRunParameters(IEnumerable<JobRunParameter> parameters, IReadOnlyList<JobParameter> definitionParameters)
    {
        if (_parameterEncryption is null)
            return;

        var encryptedKeys = definitionParameters
            .Where(p => _parameterEncryption.UsesEncryptedStorage(p.EncryptedValue))
            .Select(p => p.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var parameter in parameters) {
            if (!encryptedKeys.Contains(parameter.Key) && !_parameterEncryption.UsesEncryptedStorage(parameter.EncryptedValue))
                continue;

            var value = parameter.Value;
            var encrypted = parameter.EncryptedValue;
            _parameterEncryption.EncryptParameterValue(ref value, ref encrypted);
            parameter.Value = value;
            parameter.EncryptedValue = encrypted ?? parameter.EncryptedValue;
        }
    }

    private JobRunRes MaskRunResponse(JobRunRes run)
    {
        if (_parameterEncryption is null || run.JobRunParameters is null)
            return run;

        var maskedParams = run.JobRunParameters
            .Select(p => p with {
                Value = _parameterEncryption.MaskValue(p.Value, p.EncryptedValue),
                EncryptedValue = _parameterEncryption.UsesEncryptedStorage(p.EncryptedValue) ? null : p.EncryptedValue
            })
            .ToList();

        return run with { JobRunParameters = maskedParams };
    }

    private static bool CheckStartSla(JobRunRes run, DateTime startedAt)
    {
        var mustStartByMinutes = run.JobDefinition?.MustStartByMinutes ?? 0;
        return mustStartByMinutes > 0 && startedAt > run.CreatedTimestamp.AddMinutes(mustStartByMinutes);
    }

    private static bool CheckDurationSla(JobRunRes run, DateTime finishedAt)
    {
        var expectedMinutes = run.JobDefinition?.ExpectedDurationMinutes ?? 0;
        if (expectedMinutes <= 0 || !run.StartedTimestamp.HasValue)
            return false;

        return finishedAt > run.StartedTimestamp.Value.AddMinutes(expectedMinutes);
    }

    private Task PublishFailureAlertAsync(Guid definitionId, Guid runId, string? definitionName)
        => TryPublishAsync(
            () => eventPublisher.PublishAlertAsync(
                definitionId, runId, JobAlertType.Failure, $"Job '{definitionName ?? definitionId.ToString()}' failed (run {runId})"),
            "Failed to publish failure alert for run {RunId}", runId);

    private Task PublishSlaAlertAsync(Guid definitionId, Guid runId, string? definitionName, string detail)
        => TryPublishAsync(
            () => eventPublisher.PublishAlertAsync(
                definitionId, runId, JobAlertType.SlaBreach, $"{detail} for '{definitionName ?? definitionId.ToString()}' (run {runId})"),
            "Failed to publish SLA alert for run {RunId}", runId);
}