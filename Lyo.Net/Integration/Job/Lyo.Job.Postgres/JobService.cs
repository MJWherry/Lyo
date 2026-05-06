using System.Diagnostics;
using System.Text.RegularExpressions;
using Lyo.Api.Mapping;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Common.Response;
using Lyo.Api.Models.Error;
using Lyo.Api.Services.Crud.Create;
using Lyo.Api.Services.Crud.Read.Query;
using Lyo.Api.Services.Crud.Update;
using Lyo.Common.Identifiers;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Events;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Job.Postgres.Database;
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
    IHttpContextAccessor? httpContextAccessor = null)
{
    public async Task<CreateResult<JobRunLogRes>> Log(Guid jobRunId, JobRunLogReq request)
        => await createService.CreateAsync<JobRunLogReq, JobRunLog, JobRunLogRes>(
                request, ctx => {
                    ctx.Entity.Id = LyoGuid.CreateCombPostgres();
                    ctx.Entity.JobRunId = jobRunId;
                })
            .ConfigureAwait(false);

    public async Task<CreateResult<JobRunRes>> CreateJobRun(JobRunReq request, CancellationToken ct = default)
    {
        if (!eventPublisher.IsConnected())
            return ResultFactory.CreateFailure<JobRunRes>(LogAndReturnApiError("Could not connect to Message Queue Service", ApiErrCodes.MessageQueueConnectionIssue));

        // Enforce MaxConcurrentRuns if configured on the definition.
        await using (var db = await dbFactory.CreateDbContextAsync(ct).ConfigureAwait(false)) {
            var def = await db.JobDefinitions.FindAsync([request.JobDefinitionId], ct).ConfigureAwait(false);
            if (def is { MaxConcurrentRuns: > 0 }) {
                var activeCount = await db.JobRuns.CountAsync(r => r.JobDefinitionId == request.JobDefinitionId && (r.State == JobState.Queued || r.State == JobState.Running), ct)
                    .ConfigureAwait(false);

                if (activeCount >= def.MaxConcurrentRuns) {
                    return ResultFactory.CreateFailure<JobRunRes>(
                        LogAndReturnApiError($"Job definition has reached its concurrent run limit ({def.MaxConcurrentRuns}).", ApiErrCodes.InvalidRequest));
                }
            }
        }

        // Validate run parameters against definition parameter constraints.
        var validationError = await ValidateRunParametersAsync(request, ct).ConfigureAwait(false);
        if (validationError is not null)
            return ResultFactory.CreateFailure<JobRunRes>(validationError);

        CreateResult<JobRunRes> result;
        try {
            result = await createService.CreateAsync<JobRunReq, JobRun, JobRunRes>(
                    request, ctx => {
                        ctx.Entity.Id = LyoGuid.CreateCombPostgres();
                        ctx.Entity.State = JobState.Queued;
                        ctx.Entity.CreatedTimestamp = DateTime.UtcNow;
                        foreach (var j in ctx.Entity.JobRunParameters)
                            j.Id = LyoGuid.CreateCombPostgres();
                    }, ctx => {
                        ctx.DbContext.Entry(ctx.Entity).Navigation("JobDefinition").Load();
                    }, ct: ct)
                .ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: "23505", ConstraintName: "ix_job_run_schedule_slot_unique" } pgEx) {
            // Another scheduler instance already created a run for this (schedule, slot) pair — idempotent.
            logger.LogInformation(
                "Duplicate job run for schedule {ScheduleId} slot {Slot:u} suppressed (constraint {Constraint})", request.JobScheduleId, request.ScheduledSlotUtc,
                pgEx.ConstraintName);

            return ResultFactory.CreateFailure<JobRunRes>(LogAndReturnApiError("A job run already exists for this scheduled slot.", ApiErrCodes.Conflict));
        }

        if (!result.IsSuccess)
            return result;

        var notified = await TryPublishAsync(
                () => eventPublisher.PublishRunCreatedAsync(result.Data!.Id, result.Data!.JobDefinition!.WorkerType, ct), "Failed to publish run {RunId} created", result.Data!.Id)
            .ConfigureAwait(false);

        return !notified
            ? ResultFactory.CreateFailure<JobRunRes>(LogAndReturnApiError("Could not connect to Message Queue Service", ApiErrCodes.MessageQueueConnectionIssue))
            : result;
    }

    public async Task<(JobRunRes? Result, LyoProblemDetails? Error)> StartedJobRun(Guid jobRunId)
    {
        if (!eventPublisher.IsConnected())
            return (null, LogAndReturnApiError("Could not connect to Message Queue Service", ApiErrCodes.MessageQueueConnectionIssue));

        var existing = await queryService.Get<JobRun, JobRunRes>([jobRunId], ["JobRunParameters"]).ConfigureAwait(false);
        if (existing is null)
            return (null, LogAndReturnApiError("Job run not found", ApiErrCodes.NotFound));

        var patchRequest = PatchRequestBuilder.ForId(jobRunId).SetProperty("State", JobState.Running).SetProperty("StartedTimestamp", DateTime.UtcNow);
        var result = await patchService.PatchAsync<JobRun, JobRunRes>(patchRequest.Build()).ConfigureAwait(false);
        if (!result.IsSuccess)
            return (null, LogAndReturnApiError("Failed to patch start job", ApiErrCodes.InvalidPatchRequest));

        var notified = await TryPublishAsync(() => eventPublisher.PublishRunStartedAsync(jobRunId), "Failed to publish run {RunId} started", jobRunId).ConfigureAwait(false);
        if (!notified)
            return (null, LogAndReturnApiError("Could not notify to start job", ApiErrCodes.MessageQueueConnectionIssue));

        return (result.NewData, null);
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

        // Transition to Cancelling so callers can poll the state until the worker confirms.
        var patchRequest = PatchRequestBuilder.ForId(jobRunId).SetProperty("State", JobState.Cancelling).Build();
        var patched = await patchService.PatchAsync<JobRun, JobRunRes>(patchRequest).ConfigureAwait(false);
        if (!patched.IsSuccess)
            return (null, LogAndReturnApiError("Failed to update job state to Cancelling", ApiErrCodes.InvalidPatchRequest));

        var notified = await TryPublishAsync(() => eventPublisher.PublishRunCancelledAsync(jobRunId), "Failed to publish run {RunId} cancelled", jobRunId).ConfigureAwait(false);
        if (!notified)
            return (null, LogAndReturnApiError("Could not notify to cancel job", ApiErrCodes.MessageQueueConnectionIssue));

        return (patched.NewData, null);
    }

    public async Task<(JobRunRes? Result, LyoProblemDetails? Error)> FinishedJobRun(Guid jobRunId, IReadOnlyList<JobRunResultReq> results)
    {
        if (!eventPublisher.IsConnected())
            return (null, LogAndReturnApiError("Could not connect to Message Queue Service", ApiErrCodes.MessageQueueConnectionIssue));

        var existing = await queryService.Get<JobRun, JobRunRes>([jobRunId], ["JobRunParameters"]).ConfigureAwait(false);
        if (existing is null)
            return (null, LogAndReturnApiError("Job run not found", ApiErrCodes.NotFound));

        if (existing.State is not (JobState.Running or JobState.Cancelling))
            return (null, LogAndReturnApiError("Job is not in a finishable state (must be Running or Cancelling)", ApiErrCodes.InvalidRequest));

        var resultStr = results.FirstOrDefault(i => i.Key == Constants.Data.JobRunResultKey.Result)?.Value ?? nameof(JobRunResult.Unknown);
        var resultEnum = Enum.TryParse<JobRunResult>(resultStr, true, out var parsedResult) ? parsedResult : JobRunResult.Unknown;
        var request = PatchRequestBuilder.ForId(jobRunId)
            .SetProperty("State", JobState.Finished)
            .SetProperty("FinishedTimestamp", DateTime.UtcNow)
            .SetProperty("Result", resultEnum)
            .Build();

        var result = await patchService.PatchAsync<JobRun, JobRunRes>(
                request, ctx => {
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

        return (savedResult, null);
    }

    public async Task<CreateResult<JobRunRes>?> RerunJob(Guid jobRunId)
    {
        if (!eventPublisher.IsConnected())
            return ResultFactory.CreateFailure<JobRunRes>(LogAndReturnApiError("Could not connect to Message Queue Service", ApiErrCodes.MessageQueueConnectionIssue));

        var existing = await queryService.Get<JobRun, JobRunRes>([jobRunId], ["JobRunParameters"]).ConfigureAwait(false);
        if (existing is null)
            return ResultFactory.CreateFailure<JobRunRes>(LogAndReturnApiError("Existing job not found", ApiErrCodes.NotFound));

        var request = mapper.Map<JobRunReq>(existing);
        request.CreatedBy = httpContextAccessor?.HttpContext?.User.Identity?.Name ?? "Unknown";
        request.Result = null;
        request.JobScheduleId = null;
        request.JobTriggerId = null;
        request.TriggeredByJobRunId = null;
        request.ReRanFromJobRunId = jobRunId;
        var result = await createService.CreateAsync<JobRunReq, JobRun, JobRunRes>(
                request, ctx => {
                    ctx.Entity.Id = LyoGuid.CreateCombPostgres();
                    ctx.Entity.State = JobState.Queued;
                    ctx.Entity.CreatedTimestamp = DateTime.UtcNow;
                    foreach (var j in ctx.Entity.JobRunParameters)
                        j.Id = LyoGuid.CreateCombPostgres();
                }, ctx => {
                    ctx.DbContext.Entry(ctx.Entity).Navigation("JobDefinition").Load();
                })
            .ConfigureAwait(false);

        if (!result.IsSuccess)
            return result;

        var notified = await TryPublishAsync(
                () => eventPublisher.PublishRunCreatedAsync(result.Data!.Id, result.Data!.JobDefinition!.WorkerType), "Failed to publish run {RunId} created (rerun)",
                result.Data!.Id)
            .ConfigureAwait(false);

        return !notified ? ResultFactory.CreateFailure<JobRunRes>(LogAndReturnApiError("Could not notify to create job", ApiErrCodes.MessageQueueConnectionIssue)) : result;
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
}