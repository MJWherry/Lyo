using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Lyo.Api.Mapping;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Common.Response;
using Lyo.Api.Models.Error;
using Lyo.Api.Services.Crud.Create;
using Lyo.Api.Services.Crud.Read.Query;
using Lyo.Api.Services.Crud.Update;
using Lyo.Cache;
using Lyo.Common.Core.Conversion;
using Lyo.Common.Core.Enums;
using Lyo.Common.Core.Identifiers;
using Lyo.Common.Metadata.Records;
using Lyo.Exceptions;
using Lyo.Job.Models;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Events;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Job.Models.Security;
using Lyo.Job.Postgres.Database;
using Lyo.MessageQueue;
using Lyo.Metrics;
using Lyo.Parameters;
using Lyo.Query.Models.Parameters;
using Lyo.Scheduler;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    CacheOptions? cacheOptions = null,
    IMqService? mqService = null,
    LyoTemplateResolver? templateResolver = null)
{
    private const string ScheduleSlotUniqueConstraint = "ix_job_run_schedule_slot_unique";
    private const string IdempotencyKeyUniqueConstraint = "ix_job_run_idempotency_key_unique";

    private readonly IMetrics _metrics = metrics ?? NullMetrics.Instance;
    private readonly IJobParameterEncryptionService? _parameterEncryption = parameterEncryption;

    /// <summary>
    /// The CAS transitions below use <c>ExecuteUpdateAsync</c>, which bypasses the CRUD services (and therefore their query-cache invalidation), so any cached
    /// <see cref="JobRun" /> GET and list entries must be invalidated by hand. Definition grids that project last-run columns are tagged as
    /// <c>entity:jobdefinition</c>, not <c>entity:jobrun</c>, so both type tags are cleared.
    /// </summary>
    private Task InvalidateRunCacheAsync(Guid jobRunId)
    {
        if (cache is null)
            return Task.CompletedTask;

        var options = cacheOptions ?? new CacheOptions();
        return Task.WhenAll(
            QueryCacheInvalidation.InvalidateQueryCachesForBroadEntityTypeAsync<JobRun, JobDefinition>(cache),
            QueryCacheInvalidation.InvalidateQueryCachesForEntityKeysAsync(cache, options, typeof(JobRun), [new object?[] { jobRunId }]));
    }

    public async Task<CreateResult<JobRunLogRes>> Log(Guid jobRunId, JobRunLogReq request, CancellationToken ct = default)
    {
        var result = await createService.CreateAsync<JobRunLogReq, JobRunLog, JobRunLogRes>(
                request, ctx => {
                    ctx.Entity.Id = LyoGuid.CreateCombPostgres();
                    ctx.Entity.JobRunId = jobRunId;
                }, ct: ct)
            .ConfigureAwait(false);
        if (result.IsSuccess)
            await QueryCacheInvalidation.InvalidateQueryCachesForBroadEntityTypeAsync<JobRun, JobDefinition>(cache).ConfigureAwait(false);

        return result;
    }

    public async Task<CreateResult<JobRunRes>> CreateJobRun(JobRunReq request, CancellationToken ct = default)
    {
        using var activity = JobTracing.StartCreateRun(request.JobDefinitionId);
        var validationError = await PrepareRunParametersAsync(request, ct).ConfigureAwait(false);
        if (validationError is not null) {
            _metrics.IncrementCounter(Constants.Metrics.Service.RunCreateRejected, tags: [("reason", "invalid_parameters")]);
            return ResultFactory.CreateFailure<JobRunRes>(validationError);
        }

        if (request.DryRun)
            return ResultFactory.CreateSuccess(await BuildDryRunResponseAsync(request, ct).ConfigureAwait(false));

        // Dispatch is suppressed when the caller asked for it explicitly, or when the run targets a future slot (delayed retry). The caller
        // (scheduler delayed-MQ envelope, workflow engine) or the maintenance service's stuck-queued recovery does the eventual dispatch.
        var suppressDispatch = request.SuppressDispatch || (request.ScheduledSlotUtc.HasValue && request.ScheduledSlotUtc.Value > DateTime.UtcNow);
        if (!suppressDispatch && !eventPublisher.IsConnected()) {
            _metrics.IncrementCounter(Constants.Metrics.Service.RunCreateRejected, tags: [("reason", "mq_disconnected")]);
            return ResultFactory.CreateFailure<JobRunRes>(LogAndReturnApiError("Could not connect to Message Queue Service", ApiErrCodes.MessageQueueConnectionIssue));
        }

        // Concurrency enforcement and creation are serialized per definition via a transaction-scoped Postgres advisory lock, so two concurrent
        // CreateJobRun calls cannot both pass the MaxConcurrentRuns check (the previous read-then-write was racy). The lock is released when
        // the transaction commits or disposes.
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

        if (request.JobScheduleId.HasValue && request.ScheduledSlotUtc.HasValue) {
            var existingSlot = await FindRunByScheduleSlotAsync(guardDb, request.JobScheduleId.Value, request.ScheduledSlotUtc.Value, ct).ConfigureAwait(false);
            if (existingSlot is not null) {
                logger.LogInformation(
                    "Duplicate job run for schedule {ScheduleId} slot {Slot:u} returned existing run {RunId}", request.JobScheduleId, request.ScheduledSlotUtc, existingSlot.Id);
                await guardTx.CommitAsync(CancellationToken.None).ConfigureAwait(false);
                return ResultFactory.CreateSuccess(MaskRunResponse(existingSlot));
            }
        }

        if (def.MaxRunsPerHour > 0) {
            var hourAgo = DateTime.UtcNow.AddHours(-1);
            var recentCount = await guardDb.JobRuns.CountAsync(r => r.JobDefinitionId == request.JobDefinitionId && r.CreatedTimestamp >= hourAgo, ct).ConfigureAwait(false);
            if (recentCount >= def.MaxRunsPerHour) {
                _metrics.IncrementCounter(Constants.Metrics.Service.RunCreateRejected, tags: [("reason", "max_runs_per_hour")]);
                return ResultFactory.CreateFailure<JobRunRes>(
                    LogAndReturnApiError($"Job definition has reached its hourly run limit ({def.MaxRunsPerHour}).", ApiErrCodes.InvalidRequest));
            }
        }

        if (def.MaxConcurrentRuns > 0) {
            var activeCount = await guardDb.JobRuns.CountAsync(r => r.JobDefinitionId == request.JobDefinitionId && (r.State == JobState.Queued || r.State == JobState.Running), ct)
                .ConfigureAwait(false);

            if (activeCount >= def.MaxConcurrentRuns) {
                _metrics.IncrementCounter(Constants.Metrics.Service.RunCreateRejected, tags: [("reason", "max_concurrent_runs")]);
                return ResultFactory.CreateFailure<JobRunRes>(
                    LogAndReturnApiError($"Job definition has reached its concurrent run limit ({def.MaxConcurrentRuns}).", ApiErrCodes.InvalidRequest));
            }
        }

        var traceId = request.TraceId ?? Activity.Current?.TraceId.ToString();
        var defParams = await guardDb.JobParameters.AsNoTracking().Where(p => p.JobDefinitionId == request.JobDefinitionId).ToListAsync(ct).ConfigureAwait(false);
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
        finally {
            // Release the advisory lock as soon as the create attempt finishes (commit of an otherwise-empty transaction).
            await guardTx.CommitAsync(CancellationToken.None).ConfigureAwait(false);
        }

        // CreateService swallows DbUpdateException and returns a failure result, so unique-constraint recovery has to inspect that
        // result rather than catch. Returning the existing run (success) keeps the scheduler from 500-looping on a slot that already fired.
        if (!result.IsSuccess && MentionsConstraint(result, ScheduleSlotUniqueConstraint) && request.JobScheduleId.HasValue && request.ScheduledSlotUtc.HasValue) {
            var existingSlot = await FindRunByScheduleSlotAsync(guardDb, request.JobScheduleId.Value, request.ScheduledSlotUtc.Value, ct).ConfigureAwait(false);
            if (existingSlot is not null) {
                logger.LogInformation(
                    "Duplicate job run for schedule {ScheduleId} slot {Slot:u} returned existing run {RunId}", request.JobScheduleId, request.ScheduledSlotUtc, existingSlot.Id);
                return ResultFactory.CreateSuccess(MaskRunResponse(existingSlot));
            }

            _metrics.IncrementCounter(Constants.Metrics.Service.RunCreateRejected, tags: [("reason", "duplicate_slot")]);
            return ResultFactory.CreateFailure<JobRunRes>(LogAndReturnApiError("A job run already exists for this scheduled slot.", ApiErrCodes.Conflict));
        }

        if (!result.IsSuccess && MentionsConstraint(result, IdempotencyKeyUniqueConstraint) && !string.IsNullOrWhiteSpace(request.IdempotencyKey)) {
            var existing = await FindRunByIdempotencyKeyAsync(guardDb, request.JobDefinitionId, request.IdempotencyKey, ct).ConfigureAwait(false);
            if (existing is not null)
                return ResultFactory.CreateSuccess(existing);
        }

        if (!result.IsSuccess)
            return result;

        await QueryCacheInvalidation.InvalidateQueryCachesForBroadEntityTypeAsync<JobRun, JobDefinition>(cache).ConfigureAwait(false);

        activity?.SetTag("job.run.id", result.Data!.Id);
        _metrics.IncrementCounter(Constants.Metrics.Service.RunCreated, tags: [("definition", result.Data!.JobDefinition?.Name ?? "unknown")]);
        if (suppressDispatch) {
            logger.LogDebug(
                "Dispatch suppressed for run {RunId} (SuppressDispatch={Suppress}, ScheduledSlotUtc={Slot:u})", result.Data!.Id, request.SuppressDispatch,
                request.ScheduledSlotUtc);

            return ResultFactory.CreateSuccess(MaskRunResponse(result.Data!));
        }

        var notified = await TryPublishAsync(
                () => eventPublisher.PublishRunCreatedAsync(result.Data!.Id, result.Data!.JobDefinition!.WorkerType, result.Data!.Priority, ct),
                "Failed to publish run {RunId} created", result.Data!.Id)
            .ConfigureAwait(false);

        if (!notified) {
            // The run is already persisted as Queued. The maintenance service's stuck-queued recovery will redispatch it, so the create still succeeds.
            _metrics.IncrementCounter(Constants.Metrics.Service.RunDispatchDeferred);
            logger.LogWarning("Run {RunId} was created but the dispatch publish failed; maintenance will redispatch it", result.Data!.Id);
        }

        return ResultFactory.CreateSuccess(MaskRunResponse(result.Data!));
    }

    /// <summary>Creates child runs for a parent batch and stamps batch metadata on each request.</summary>
    public async Task<IReadOnlyList<JobRunRes>> CreateChildRunsAsync(Guid parentRunId, JobCreateChildRunsReq request, CancellationToken ct = default)
    {
        // Build the template from the entity (not the masked API response) so encrypted parameter values survive, and so the parent's
        // IdempotencyKey/ScheduledSlotUtc are not copied onto children. A copied key would silently return the parent as every "child".
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
            })
            .ToList();

        return await CreateChildRunsAsync(parentRunId, children, ct).ConfigureAwait(false);
    }

    /// <summary>Creates child runs for a parent batch and writes batch metadata onto each request.</summary>
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

    /// <summary>Returns the next scheduled run times for a definition, merged across every enabled schedule.</summary>
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
                if (!schedule.IsWithinScheduleWindow(runAt))
                    continue;

                var adjusted = schedule.AdjustSlotForBlackout(runAt);
                if (!adjusted.HasValue)
                    continue;

                merged.Add(adjusted.Value);
            }
        }

        return merged.OrderBy(t => t).Distinct().Take(count).ToList();
    }

    /// <summary>Updates heartbeat and optional progress fields on a job that is running.</summary>
    public async Task<(JobRunRes? Result, LyoProblemDetails? Error)> HeartbeatJobRun(Guid jobRunId, JobRunHeartbeatReq? request, CancellationToken ct = default)
    {
        var existing = await queryService.Get<JobRun, JobRunRes>([jobRunId]).ConfigureAwait(false);
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
        return !result.IsSuccess ? (null, LogAndReturnApiError("Failed to patch job heartbeat", ApiErrCodes.InvalidPatchRequest)) : (result.NewData, null);
    }

    /// <summary>
    /// Moves a run from <c>Queued</c> to <c>Running</c> using an atomic compare-and-swap. Any other state (already <c>Running</c> from a duplicate delivery,
    /// <c>Cancelling</c> from a queued-run cancel, or <c>Finished</c>) is rejected so redelivered dispatch messages never execute a run twice. This is a worker-trusted
    /// endpoint: the returned run has encrypted parameter values decrypted so workers can execute with real values (all other read endpoints stay masked).
    /// </summary>
    public async Task<(JobRunRes? Result, LyoProblemDetails? Error)> StartedJobRun(Guid jobRunId, JobRunStartedReq? request = null, CancellationToken ct = default)
    {
        using var activity = JobTracing.StartRun(jobRunId);
        if (!eventPublisher.IsConnected())
            return (null, LogAndReturnApiError("Could not connect to Message Queue Service", ApiErrCodes.MessageQueueConnectionIssue));

        var existing = await queryService.Get<JobRun, JobRunRes>([jobRunId], ["JobRunParameters", "JobDefinition"]).ConfigureAwait(false);
        if (existing is null)
            return (null, LogAndReturnApiError("Job run not found", ApiErrCodes.NotFound));

        var startedAt = DateTime.UtcNow;
        var slaBreached = CheckStartSla(existing, startedAt);
        var workerInstanceId = request?.WorkerInstanceId;
        var workerMachineName = string.IsNullOrWhiteSpace(request?.MachineName) ? null : request.MachineName.Trim();
        var workerProcessId = request?.ProcessId;

        // Compare-and-swap: only a Queued run may start. The WHERE clause makes the transition atomic, so a redelivered dispatch message
        // (or a second worker instance) loses the race instead of double-executing, and a Cancelling queued run is never brought back.
        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (workerInstanceId is { } instanceId) {
            var worker = await db.JobWorkerInstances.AsNoTracking().FirstOrDefaultAsync(w => w.Id == instanceId, ct).ConfigureAwait(false);
            if (worker != null) {
                workerMachineName ??= worker.MachineName;
                workerProcessId ??= worker.ProcessId;
            }
        }

        var updatedRows = await db.JobRuns.Where(r => r.Id == jobRunId && r.State == JobState.Queued)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.State, JobState.Running)
                .SetProperty(r => r.StartedTimestamp, startedAt)
                .SetProperty(r => r.UpdatedTimestamp, startedAt)
                .SetProperty(r => r.SlaBreached, r => r.SlaBreached || slaBreached)
                .SetProperty(r => r.WorkerInstanceId, workerInstanceId)
                .SetProperty(r => r.WorkerMachineName, workerMachineName)
                .SetProperty(r => r.WorkerProcessId, workerProcessId), ct)
            .ConfigureAwait(false);

        if (updatedRows == 0) {
            _metrics.IncrementCounter(Constants.Metrics.Service.RunStartRejected);
            return (null,
                LogAndReturnApiError(
                    $"Job run cannot start: it is not in the Queued state (current state may be Running, Cancelling, or Finished). Run {jobRunId}.", ApiErrCodes.InvalidRequest));
        }

        if (slaBreached)
            await PublishSlaAlertAsync(existing.JobDefinitionId, jobRunId, existing.JobDefinition?.Name, "Run exceeded MustStartBy SLA").ConfigureAwait(false);

        _metrics.IncrementCounter(Constants.Metrics.Service.RunStarted);
        _metrics.RecordTiming(Constants.Metrics.Service.RunQueueLatency, startedAt - existing.CreatedTimestamp);

        // The run is already committed as Running and the CAS will reject a second start, so a failed publish must not be reported as a
        // failed start. That told the worker to abandon a run the database says it owns. The started event is advisory (progress/UI
        // notification). The worker proceeds and its heartbeats keep dead-job detection at bay.
        var notified = await TryPublishAsync(() => eventPublisher.PublishRunStartedAsync(jobRunId), "Failed to publish run {RunId} started", jobRunId).ConfigureAwait(false);
        if (!notified)
            _metrics.IncrementCounter(Constants.Metrics.Service.RunStartedPublishFailed);

        // Re-fetch with includes so workers get a fully loaded run (the CAS update does not return the entity).
        await InvalidateRunCacheAsync(jobRunId).ConfigureAwait(false);
        var savedResult = await queryService.Get<JobRun, JobRunRes>([jobRunId], ["JobRunParameters", "JobRunResults", "JobDefinition", "JobDefinition.JobParameters"])
            .ConfigureAwait(false);

        // Worker-trusted path: decrypt parameter values. The mapper masks them for every other endpoint.
        return (await DecryptRunParametersAsync(savedResult!).ConfigureAwait(false), null);
    }

    public async Task<(JobRunRes? Result, LyoProblemDetails? Error)> CancelJobRun(Guid jobRunId, CancellationToken ct = default)
    {
        if (!eventPublisher.IsConnected())
            return (null, LogAndReturnApiError("Could not connect to Message Queue Service", ApiErrCodes.MessageQueueConnectionIssue));

        var existing = await queryService.Get<JobRun, JobRunRes>([jobRunId], ["JobRunParameters"]).ConfigureAwait(false);
        if (existing is null)
            return (null, LogAndReturnApiError("Job run not found", ApiErrCodes.NotFound));

        if (existing.State is not (JobState.Running or JobState.Queued))
            return (null, LogAndReturnApiError("Job is not in a cancellable state (must be Running or Queued)", ApiErrCodes.InvalidRequest));

        if (existing.State == JobState.Queued) {
            // A queued run has no worker to confirm the cancel, and StartedJobRun's CAS guard will reject its dispatch message, so finalize
            // it directly. The CAS keeps this race-safe: if a worker started the run in the meantime the update misses and we fall through
            // to the Cancelling path below.
            var now = DateTime.UtcNow;
            await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
            var cancelledRows = await db.JobRuns.Where(r => r.Id == jobRunId && r.State == JobState.Queued)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.State, JobState.Finished)
                    .SetProperty(r => r.Result, JobRunResult.Cancelled)
                    .SetProperty(r => r.FinishedTimestamp, now)
                    .SetProperty(r => r.UpdatedTimestamp, now), ct)
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

        // Move to Cancelling so callers can poll the state until the worker confirms.
        var patchRequest = PatchRequestBuilder.ForId(jobRunId).SetProperty("State", JobState.Cancelling).Build();
        var patched = await patchService.PatchAsync<JobRun, JobRunRes>(patchRequest, ct: ct).ConfigureAwait(false);
        if (!patched.IsSuccess)
            return (null, LogAndReturnApiError("Failed to update job state to Cancelling", ApiErrCodes.InvalidPatchRequest));

        _metrics.IncrementCounter(Constants.Metrics.Service.RunCancelled);
        var notified = await TryPublishAsync(() => eventPublisher.PublishRunCancelledAsync(jobRunId), "Failed to publish run {RunId} cancelled", jobRunId).ConfigureAwait(false);
        if (!notified)
            return (null, LogAndReturnApiError("Could not notify to cancel job", ApiErrCodes.MessageQueueConnectionIssue));

        return (patched.NewData, null);
    }

    /// <summary>
    /// Moves a run from <c>Running</c> back to <c>Queued</c> using an atomic compare-and-swap. Used by workers during graceful host shutdown to hand the run back for
    /// redelivery instead of terminally cancelling it. <c>Cancelling</c> runs are intentionally rejected. A pending user cancellation must not be forgotten by a restart.
    /// </summary>
    public async Task<(JobRunRes? Result, LyoProblemDetails? Error)> RequeueJobRun(Guid jobRunId, CancellationToken ct = default)
    {
        var existing = await queryService.Get<JobRun, JobRunRes>([jobRunId]).ConfigureAwait(false);
        if (existing is null)
            return (null, LogAndReturnApiError("Job run not found", ApiErrCodes.NotFound));

        var now = DateTime.UtcNow;
        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        // Clear the worker attribution along with the timestamps. The run is going back on the queue and a different worker will pick it
        // up, so leaving the old worker's instance/machine/process on the row misattributes the next attempt in the UI and in queries.
        var updatedRows = await db.JobRuns.Where(r => r.Id == jobRunId && r.State == JobState.Running)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.State, JobState.Queued)
                .SetProperty(r => r.StartedTimestamp, (DateTime?)null)
                .SetProperty(r => r.LastHeartbeatUtc, (DateTime?)null)
                .SetProperty(r => r.WorkerInstanceId, (Guid?)null)
                .SetProperty(r => r.WorkerMachineName, (string?)null)
                .SetProperty(r => r.WorkerProcessId, (int?)null)
                .SetProperty(r => r.UpdatedTimestamp, now), ct)
            .ConfigureAwait(false);

        if (updatedRows == 0)
            return (null, LogAndReturnApiError($"Job run cannot be requeued: it is not in the Running state. Run {jobRunId}.", ApiErrCodes.InvalidRequest));

        _metrics.IncrementCounter(Constants.Metrics.Service.RunRequeued);
        logger.LogInformation("Run {RunId} requeued (worker shutdown hand-back)", jobRunId);
        await InvalidateRunCacheAsync(jobRunId).ConfigureAwait(false);
        var saved = await queryService.Get<JobRun, JobRunRes>([jobRunId]).ConfigureAwait(false);
        return (MaskRunResponse(saved!), null);
    }

    /// <summary>
    /// Finalizes a run and records its results. The state transition is an atomic compare-and-swap over <c>Running</c>/<c>Cancelling</c> inside a transaction with the result
    /// inserts, so two concurrent finishes (a redelivered completion, or a worker finishing as maintenance times the run out) cannot both append result rows and publish
    /// duplicate finished events. The loser is rejected.
    /// </summary>
    public async Task<(JobRunRes? Result, LyoProblemDetails? Error)> FinishedJobRun(Guid jobRunId, IReadOnlyList<JobRunResultReq> results, CancellationToken ct = default)
    {
        using var activity = JobTracing.FinishRun(jobRunId);
        if (!eventPublisher.IsConnected())
            return (null, LogAndReturnApiError("Could not connect to Message Queue Service", ApiErrCodes.MessageQueueConnectionIssue));

        var existing = await queryService.Get<JobRun, JobRunRes>([jobRunId], ["JobRunParameters", "JobDefinition"]).ConfigureAwait(false);
        if (existing is null)
            return (null, LogAndReturnApiError("Job run not found", ApiErrCodes.NotFound));

        var resultStr = results.FirstOrDefault(i => i.Key == Constants.Data.JobRunResultKey.Result)?.Value ?? nameof(JobRunResult.Unknown);
        if (resultStr.Length >= 2 && resultStr[0] == '"') {
            try {
                resultStr = JsonSerializer.Deserialize<string>(resultStr) ?? resultStr;
            }
            catch (JsonException) {
                // Stored as a JSON string when Type is System.String. Leave plaintext as-is.
            }
        }

        var resultEnum = TypeConversion.EnumOrDefault(resultStr, JobRunResult.Unknown);
        var finishedAt = DateTime.UtcNow;
        var durationSlaBreached = CheckDurationSla(existing, finishedAt);
        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using var transaction = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        var claimed = await db.JobRuns.Where(r => r.Id == jobRunId && (r.State == JobState.Running || r.State == JobState.Cancelling))
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(r => r.State, JobState.Finished)
                    .SetProperty(r => r.Result, resultEnum)
                    .SetProperty(r => r.FinishedTimestamp, finishedAt)
                    .SetProperty(r => r.UpdatedTimestamp, finishedAt)
                    .SetProperty(r => r.SlaBreached, r => r.SlaBreached || durationSlaBreached), ct)
            .ConfigureAwait(false);

        if (claimed == 0) {
            await transaction.RollbackAsync(ct).ConfigureAwait(false);
            _metrics.IncrementCounter(Constants.Metrics.Service.RunFinishRejected);
            return (null,
                LogAndReturnApiError(
                    $"Job is not in a finishable state (must be Running or Cancelling); it may already be Finished. Run {jobRunId}.", ApiErrCodes.InvalidRequest));
        }

        foreach (var res in results) {
            var entity = mapper.Map<Database.JobRunResult>(res);
            entity.Id = LyoGuid.CreateCombPostgres();
            entity.JobRunId = jobRunId;
            db.JobRunResults.Add(entity);
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await transaction.CommitAsync(ct).ConfigureAwait(false);
        await InvalidateRunCacheAsync(jobRunId).ConfigureAwait(false);

        _metrics.IncrementCounter(Constants.Metrics.Service.RunFinished, tags: [("result", resultEnum.ToString())]);
        if (existing.StartedTimestamp.HasValue)
            _metrics.RecordTiming(Constants.Metrics.Service.RunDuration, finishedAt - existing.StartedTimestamp.Value, [("result", resultEnum.ToString())]);

        if (durationSlaBreached)
            await PublishSlaAlertAsync(existing.JobDefinitionId, jobRunId, existing.JobDefinition?.Name, "Run exceeded ExpectedDuration SLA").ConfigureAwait(false);

        if (resultEnum == JobRunResult.Failure && existing.JobDefinition is { AlertOnFailure: true, AlertAfterConsecutiveFailures: 0 })
            await PublishFailureAlertAsync(existing.JobDefinitionId, jobRunId, existing.JobDefinition.Name).ConfigureAwait(false);

        var notified = await TryPublishAsync(() => eventPublisher.PublishRunFinishedAsync(jobRunId), "Failed to publish run {RunId} finished", jobRunId).ConfigureAwait(false);
        if (!notified) {
            // The run is committed as Finished. The caller must not retry the finish (the CAS would reject it). Surface the publish failure
            // so operators know downstream consumers were not notified. The scheduler's own recovery re-reads run state.
            logger.LogError("Run {RunId} finished but the finished event could not be published; downstream consumers were not notified", jobRunId);
            return (null, LogAndReturnApiError("Job finished but could not notify subscribers", ApiErrCodes.MessageQueueConnectionIssue));
        }

        var savedResult = await queryService.Get<JobRun, JobRunRes>(
                [jobRunId],
                [
                    "JobRunResults", "JobRunLogs", "JobRunParameters", "JobTrigger", "JobSchedule", "JobDefinition", "JobDefinition.JobSchedules",
                    "JobDefinition.JobTriggerTriggersJobDefinitions"
                ])
            .ConfigureAwait(false);

        return (MaskRunResponse(savedResult!), null);
    }

    public async Task<CreateResult<JobRunRes>?> RerunJob(Guid jobRunId, CancellationToken ct = default)
    {
        // Build the request from the entity (not the masked API response) so encrypted parameter values survive, and the original run's
        // IdempotencyKey/ScheduledSlotUtc are not copied. A copied key would violate the unique index and fail the rerun.
        var request = await BuildRunRequestFromEntityAsync(jobRunId, ct).ConfigureAwait(false);
        if (request is null)
            return ResultFactory.CreateFailure<JobRunRes>(LogAndReturnApiError("Existing job not found", ApiErrCodes.NotFound));

        request.CreatedBy = httpContextAccessor?.HttpContext?.User.Identity?.Name ?? "Unknown";
        request.ReRanFromJobRunId = jobRunId;

        // Route through CreateJobRun so reruns get parameter validation, rate and concurrency limits, encryption, and the advisory lock.
        var result = await CreateJobRun(request, ct).ConfigureAwait(false);
        if (result.IsSuccess)
            _metrics.IncrementCounter(Constants.Metrics.Service.RunRerun);

        return result;
    }

    /// <summary>
    /// Republishes due <c>Queued</c> runs whose dispatch message is not already on the worker queue. The Runs grid uses this when an operator needs to refill RabbitMQ after a
    /// queue was emptied. Runs already present on <c>job.run.{workerType}</c> or its <c>.wait</c> delay queue are skipped. Duplicate publishes are harmless because
    /// <c>StartedJobRun</c> only transitions <c>Queued -&gt; Running</c> once.
    /// </summary>
    /// <param name="definitionId">When set, only runs of this definition are considered.</param>
    /// <param name="ct">Token used to cancel the resync.</param>
    public async Task<(JobRunResyncRes? Result, LyoProblemDetails? Error)> ResyncQueuedRunsAsync(Guid? definitionId = null, CancellationToken ct = default)
    {
        if (!eventPublisher.IsConnected())
            return (null, LogAndReturnApiError("Could not connect to Message Queue Service", ApiErrCodes.MessageQueueConnectionIssue));

        var now = DateTime.UtcNow;
        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var query = db.JobRuns.AsNoTracking().Where(r => r.State == JobState.Queued && !r.DryRun && (r.ScheduledSlotUtc == null || r.ScheduledSlotUtc <= now));
        if (definitionId is { } defId)
            query = query.Where(r => r.JobDefinitionId == defId);

        // Only the dispatch fields are needed. The batch is capped so a large queued backlog cannot pull every run (with its whole definition) into memory in one request.
        var candidates = await query.OrderBy(r => r.CreatedTimestamp)
            .Take(ResyncBatchLimit + 1)
            .Select(r => new ResyncCandidate(r.Id, r.JobDefinition.WorkerType, r.Priority))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var truncated = candidates.Count > ResyncBatchLimit;
        if (truncated)
            candidates.RemoveRange(ResyncBatchLimit, candidates.Count - ResyncBatchLimit);

        if (candidates.Count == 0)
            return (new JobRunResyncRes(), null);

        var alreadyQueued = await new JobQueuedRunInspector(mqService, logger).GetQueuedRunIdsAsync(candidates.Select(c => c.WorkerType), candidates.Count, ct)
            .ConfigureAwait(false);
        var republished = 0;
        var failed = 0;
        var alreadyInQueue = 0;
        foreach (var run in candidates) {
            if (alreadyQueued.Contains(run.Id)) {
                alreadyInQueue++;
                continue;
            }

            try {
                await eventPublisher.PublishRunCreatedAsync(run.Id, run.WorkerType, run.Priority, ct).ConfigureAwait(false);
                republished++;
            }
            catch (Exception ex) {
                failed++;
                logger.LogError(ex, "Failed to resync queued run {RunId}", run.Id);
            }
        }

        if (republished > 0)
            _metrics.IncrementCounter(Constants.Metrics.Service.RunResynced, republished);

        return (new JobRunResyncRes {
            Queued = candidates.Count,
            AlreadyInQueue = alreadyInQueue,
            Republished = republished,
            Failed = failed,
            Truncated = truncated
        }, null);
    }

    /// <summary>
    /// Returns the latest run, latest successful run, and latest failed run per definition in one call. Used by the scheduler's definition refresh so it does not need three
    /// HTTP queries per definition.
    /// </summary>
    public async Task<IReadOnlyList<JobDefinitionLatestRunsRes>> GetLatestRuns(IReadOnlyList<Guid> definitionIds, CancellationToken ct = default)
    {
        var ids = definitionIds.Distinct().ToArray();
        if (ids.Length == 0)
            return [];

        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        // One query picks all three run ids per definition through correlated subqueries, instead of three round trips per definition. The scheduler calls this on every refresh
        // with every definition it owns, so the old shape made refresh cost grow with job count.
        var picks = await db.JobDefinitions.AsNoTracking()
            .Where(d => ids.Contains(d.Id))
            .Select(d => new {
                DefinitionId = d.Id,
                Last = db.JobRuns.Where(r => r.JobDefinitionId == d.Id).OrderByDescending(r => r.CreatedTimestamp).Select(r => (Guid?)r.Id).FirstOrDefault(),
                LastSuccess = db.JobRuns
                    .Where(r => r.JobDefinitionId == d.Id && (r.Result == JobRunResult.Success || r.Result == JobRunResult.SuccessWithWarnings))
                    .OrderByDescending(r => r.CreatedTimestamp)
                    .Select(r => (Guid?)r.Id)
                    .FirstOrDefault(),
                LastFailed = db.JobRuns.Where(r => r.JobDefinitionId == d.Id && r.Result == JobRunResult.Failure)
                    .OrderByDescending(r => r.CreatedTimestamp)
                    .Select(r => (Guid?)r.Id)
                    .FirstOrDefault()
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var runIds = picks.SelectMany(p => new[] { p.Last, p.LastSuccess, p.LastFailed }).OfType<Guid>().Distinct().ToArray();
        var runsById = new Dictionary<Guid, JobRunRes>();
        if (runIds.Length > 0) {
            var entities = await db.JobRuns.AsNoTracking()
                .Where(r => runIds.Contains(r.Id))
                .Include(r => r.JobRunParameters)
                .Include(r => r.JobRunResults)
                .AsSplitQuery()
                .ToListAsync(ct)
                .ConfigureAwait(false);

            foreach (var entity in entities)
                runsById[entity.Id] = mapper.Map<JobRunRes>(entity);
        }

        return picks.Select(p => new JobDefinitionLatestRunsRes {
                JobDefinitionId = p.DefinitionId,
                LastRun = Resolve(p.Last),
                LastSuccessfulRun = Resolve(p.LastSuccess),
                LastFailedRun = Resolve(p.LastFailed)
            })
            .ToList();

        JobRunRes? Resolve(Guid? id) => id is { } value && runsById.TryGetValue(value, out var run) ? run : null;
    }

    /// <summary>
    /// Builds a <see cref="JobRunReq" /> from the stored run entity for rerun/child-run cloning. Reads raw parameter values (including ciphertext) instead of the masked API
    /// response, and deliberately does not copy <c>IdempotencyKey</c>, <c>ScheduledSlotUtc</c>, or lineage fields. Those must be unique per run.
    /// </summary>
    private async Task<JobRunReq?> BuildRunRequestFromEntityAsync(Guid runId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var run = await db.JobRuns.AsNoTracking().Include(r => r.JobRunParameters).FirstOrDefaultAsync(r => r.Id == runId, ct).ConfigureAwait(false);
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
            Type = LyoTypeInfo.NormalizeFullName(parameter.Type),
            Value = parameter.Value,
            EncryptedValue = parameter.EncryptedValue,
            Enabled = true
        };

        if (_parameterEncryption?.UsesEncryptedStorage(parameter.EncryptedValue) != true)
            return request;

        // Stored value is ciphertext. Decrypt to plaintext and flag for re-encryption (empty marker) so CreateJobRun's encryption step
        // produces fresh valid ciphertext instead of double-encrypting the stored bytes. If decryption is unavailable, pass the
        // ciphertext through untouched. Without an encryption service it is stored verbatim and stays decryptable later.
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
            clone.JobRunParameters.Add(
                new() {
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

    /// <summary>Swaps masked parameter values for decrypted plaintext on the worker-trusted Started response.</summary>
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
            })
            .ToList();

        return run with { JobRunParameters = parameters };
    }

    /// <summary>
    /// Aggregates run statistics for a job definition over the last <paramref name="days" /> days, plus current <c>Running</c> / <c>Queued</c> counts. Returns null when that
    /// definition is not found.
    /// </summary>
    /// <param name="definitionId">Definition being reported on.</param>
    /// <param name="days">Window length in days.</param>
    /// <param name="ct">Token used to cancel the query.</param>
    public Task<JobDefinitionStatsRes?> GetDefinitionStats(Guid definitionId, int days = 30, CancellationToken ct = default)
        => new JobDefinitionStatsQuery(dbFactory).GetAsync(definitionId, days, ct);

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
            JobRunParameters = request.JobRunParameters.Select(p => new JobRunParameterRes(Guid.Empty, Guid.Empty, p.Key, p.Type, p.Value, p.Description, p.EncryptedValue, false))
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

    private async Task<JobRunRes?> FindRunByScheduleSlotAsync(JobContext db, Guid scheduleId, DateTime scheduledSlotUtc, CancellationToken ct)
    {
        var run = await db.JobRuns.AsNoTracking()
            .Include(r => r.JobDefinition)
            .Include(r => r.JobRunParameters)
            .FirstOrDefaultAsync(r => r.JobScheduleId == scheduleId && r.ScheduledSlotUtc == scheduledSlotUtc, ct)
            .ConfigureAwait(false);

        return run is null ? null : mapper.Map<JobRunRes>(run);
    }

    /// <summary>
    /// <see cref="ICreateService{TContext}" /> catches unique-violation <see cref="DbUpdateException" /> and copies the Postgres message into the failure problem. Constraint
    /// recovery has to match on that text because the exception never leaves Create.
    /// </summary>
    private static bool MentionsConstraint(CreateResult<JobRunRes> result, string constraintName)
        => !result.IsSuccess && result.Error?.GetFullMessage().Contains(constraintName, StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// Fills in schedule-parameter overrides when the run names a schedule, then definition defaults the caller omitted, then validates the resulting parameter set. A "run this
    /// schedule" request can send only <see cref="JobRunReq.JobScheduleId" />. The stored schedule parameters are applied before definition back-fill so they win over definition
    /// defaults the same way the scheduler does. The scheduler still pre-populates values itself.
    /// </summary>
    /// <param name="request">Run request, mutated in place with the back-filled parameters.</param>
    /// <param name="ct">Token used to cancel the back-fill.</param>
    /// <returns>Problem details when the parameters are unusable, otherwise null.</returns>
    private async Task<LyoProblemDetails?> PrepareRunParametersAsync(JobRunReq request, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var errors = new List<string>();
        await ApplyScheduleParameterOverridesAsync(request, db, errors, ct).ConfigureAwait(false);

        var defParams = await db.JobParameters.Where(p => p.JobDefinitionId == request.JobDefinitionId).ToListAsync(ct).ConfigureAwait(false);
        if (defParams.Count > 0) {
            var specs = defParams.Select(JobDefinitionWriteValidator.ToSpec).ToList();
            BackFillDefinitionDefaults(request, defParams, specs, errors);
            errors.AddRange(LyoParameterValidator.Validate(specs, [.. request.JobRunParameters.Select(LyoParameterValueSpec.From)]));
        }

        if (errors.Count == 0)
            return null;

        return LyoProblemDetailsBuilder.CreateWithTrace(Activity.Current?.TraceId.ToString(), Activity.Current?.SpanId.ToString())
            .WithErrorCode(ApiErrCodes.InvalidRequest)
            .WithMessage("One or more job run parameters failed validation.")
            .AddErrors(errors.Select(e => new ApiError(ApiErrCodes.InvalidRequest, e)))
            .Build();
    }

    /// <summary>
    /// Copies enabled schedule parameters onto the run for keys the caller did not supply. Invoke this before definition back-fill so a schedule value replaces the definition
    /// default, matching the scheduler's override order.
    /// </summary>
    private static async Task ApplyScheduleParameterOverridesAsync(JobRunReq request, JobContext db, List<string> errors, CancellationToken ct)
    {
        if (request.JobScheduleId is not { } scheduleId)
            return;

        var schedule = await db.JobSchedules.AsNoTracking()
            .Include(s => s.JobScheduleParameters)
            .FirstOrDefaultAsync(s => s.Id == scheduleId, ct)
            .ConfigureAwait(false);
        if (schedule is null) {
            errors.Add($"Job schedule '{scheduleId}' was not found.");
            return;
        }

        if (schedule.JobDefinitionId != request.JobDefinitionId) {
            errors.Add("Job schedule does not belong to this job definition.");
            return;
        }

        var supplied = request.JobRunParameters.Select(p => p.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var sp in schedule.JobScheduleParameters) {
            if (!sp.Enabled || supplied.Contains(sp.Key))
                continue;

            request.JobRunParameters.Add(
                new() {
                    Key = sp.Key,
                    Type = LyoTypeInfo.NormalizeFullName(sp.Type),
                    Value = sp.Value,
                    Description = sp.Description,
                    Enabled = true
                });
        }
    }

    /// <summary>
    /// Adds a run parameter for each declared parameter the caller omitted that carries a default. Expression defaults are rendered through the registered
    /// <see cref="LyoTemplateResolver" /> and normalized to the declared type, so validation sees a value of that type instead of a template.
    /// </summary>
    /// <param name="request">Run request, mutated in place.</param>
    /// <param name="defParams">Declared parameters on the definition.</param>
    /// <param name="specs">Specs projected from <paramref name="defParams" />, in the same order.</param>
    /// <param name="errors">List that collects error messages.</param>
    private void BackFillDefinitionDefaults(JobRunReq request, List<JobParameter> defParams, List<LyoParameterSpec> specs, List<string> errors)
    {
        var supplied = request.JobRunParameters.Select(p => p.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < defParams.Count; i++) {
            var def = defParams[i];
            if (supplied.Contains(def.Key))
                continue;

            var spec = specs[i];
            if (!def.Required && !LyoParameterDefaults.HasDefault(spec, def.Value, def.EncryptedValue is not null))
                continue;

            if (!LyoParameterDefaults.TryResolve(spec, def.Value, templateResolver, out var value, out var error)) {
                errors.Add(error!);
                continue;
            }

            var parameter = new JobRunParameterReq {
                Key = def.Key,
                Description = def.Description,
                Type = LyoTypeInfo.NormalizeFullName(def.Type),
                Value = value,
                EncryptedValue = def.EncryptedValue,
                Enabled = true
            };

            // Ciphertext copied from the definition would be double-encrypted on save, so take the same decrypt-and-remark path reruns use.
            if (_parameterEncryption?.UsesEncryptedStorage(def.EncryptedValue) == true) {
                var plaintext = _parameterEncryption.DecryptValue(def.EncryptedValue);
                if (plaintext is not null) {
                    parameter.Value = plaintext;
                    parameter.EncryptedValue = [];
                }
            }

            request.JobRunParameters.Add(parameter);
        }
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

        var encryptedKeys = definitionParameters.Where(p => _parameterEncryption.UsesEncryptedStorage(p.EncryptedValue))
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

        var maskedParams = run.JobRunParameters.Select(p => p with {
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

    /// <summary>Most due <c>Queued</c> runs a single resync call will republish. Callers repeat the call to work through a larger backlog.</summary>
    private const int ResyncBatchLimit = 2_000;

    /// <summary>A due <c>Queued</c> run reduced to the fields required to republish its dispatch message.</summary>
    private sealed record ResyncCandidate(Guid Id, string WorkerType, int Priority);

    private Task PublishFailureAlertAsync(Guid definitionId, Guid runId, string? definitionName)
        => TryPublishAsync(
            () => eventPublisher.PublishAlertAsync(definitionId, runId, JobAlertType.Failure, $"Job '{definitionName ?? definitionId.ToString()}' failed (run {runId})"),
            "Failed to publish failure alert for run {RunId}", runId);

    private Task PublishSlaAlertAsync(Guid definitionId, Guid runId, string? definitionName, string detail)
        => TryPublishAsync(
            () => eventPublisher.PublishAlertAsync(definitionId, runId, JobAlertType.SlaBreach, $"{detail} for '{definitionName ?? definitionId.ToString()}' (run {runId})"),
            "Failed to publish SLA alert for run {RunId}", runId);
}