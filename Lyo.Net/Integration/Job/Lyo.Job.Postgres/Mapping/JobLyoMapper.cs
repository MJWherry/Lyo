using System.Globalization;
using Lyo.Api.Mapping;
using Lyo.Common.Enums;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
using Lyo.Job.Models.Response;
using Lyo.Job.Postgres.Database;
using Lyo.Query.Models.Enums;
using Lyo.Schedule.Models;
using JobRunResultEntity = Lyo.Job.Postgres.Database.JobRunResult;

namespace Lyo.Job.Postgres.Mapping;

/// <summary>
/// Hand-rolled <see cref="ILyoMapper" /> for job Req/entity/Res. Explicit property assignments — no Mapster.
/// </summary>
public sealed class JobLyoMapper : ILyoMapper
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public TResult Map<TResult>(object source)
        => source switch {
            JobDefinitionReq req when typeof(TResult) == typeof(JobDefinition) => (TResult)(object)ReqToNew(req),
            JobParameterReq req when typeof(TResult) == typeof(JobParameter) => (TResult)(object)ReqToNew(req),
            JobScheduleReq req when typeof(TResult) == typeof(JobSchedule) => (TResult)(object)ReqToNew(req),
            JobScheduleParameterReq req when typeof(TResult) == typeof(JobScheduleParameter) => (TResult)(object)ReqToNew(req),
            JobTriggerReq req when typeof(TResult) == typeof(JobTrigger) => (TResult)(object)ReqToNew(req),
            JobTriggerParameterReq req when typeof(TResult) == typeof(JobTriggerParameter) => (TResult)(object)ReqToNew(req),
            JobParallelRestrictionReq req when typeof(TResult) == typeof(JobParallelRestriction) => (TResult)(object)ReqToNew(req),
            JobBlackoutCalendarReq req when typeof(TResult) == typeof(JobBlackoutCalendar) => (TResult)(object)ReqToNew(req),
            JobBlackoutWindowReq req when typeof(TResult) == typeof(JobBlackoutWindow) => (TResult)(object)ReqToNew(req),
            JobWorkflowReq req when typeof(TResult) == typeof(JobWorkflow) => (TResult)(object)ReqToNew(req),
            JobWorkflowStepReq req when typeof(TResult) == typeof(JobWorkflowStep) => (TResult)(object)ReqToNew(req),
            JobWorkflowRunReq req when typeof(TResult) == typeof(JobWorkflowRun) => (TResult)(object)ReqToNew(req),
            JobWorkflowRunStepReq req when typeof(TResult) == typeof(JobWorkflowRunStep) => (TResult)(object)ReqToNew(req),
            JobRunReq req when typeof(TResult) == typeof(JobRun) => (TResult)(object)ReqToNew(req),
            JobRunParameterReq req when typeof(TResult) == typeof(JobRunParameter) => (TResult)(object)ReqToNew(req),
            JobRunResultReq req when typeof(TResult) == typeof(JobRunResultEntity) => (TResult)(object)ReqToNew(req),
            JobRunLogReq req when typeof(TResult) == typeof(JobRunLog) => (TResult)(object)ReqToNew(req),
            JobWorkerInstanceReq req when typeof(TResult) == typeof(JobWorkerInstance) => (TResult)(object)ReqToNew(req),

            JobDefinition e when typeof(TResult) == typeof(JobDefinitionRes) => (TResult)(object)ToRes(e),
            JobParameter e when typeof(TResult) == typeof(JobParameterRes) => (TResult)(object)ToRes(e),
            JobSchedule e when typeof(TResult) == typeof(JobScheduleRes) => (TResult)(object)ToRes(e),
            JobScheduleParameter e when typeof(TResult) == typeof(JobScheduleParameterRes) => (TResult)(object)ToRes(e),
            JobTrigger e when typeof(TResult) == typeof(JobTriggerRes) => (TResult)(object)ToRes(e),
            JobTriggerParameter e when typeof(TResult) == typeof(JobTriggerParameterRes) => (TResult)(object)ToRes(e),
            JobParallelRestriction e when typeof(TResult) == typeof(JobParallelRestrictionRes) => (TResult)(object)ToRes(e),
            JobBlackoutCalendar e when typeof(TResult) == typeof(JobBlackoutCalendarRes) => (TResult)(object)ToRes(e),
            JobBlackoutWindow e when typeof(TResult) == typeof(JobBlackoutWindowRes) => (TResult)(object)ToRes(e),
            JobWorkflow e when typeof(TResult) == typeof(JobWorkflowRes) => (TResult)(object)ToRes(e),
            JobWorkflowStep e when typeof(TResult) == typeof(JobWorkflowStepRes) => (TResult)(object)ToRes(e),
            JobWorkflowRun e when typeof(TResult) == typeof(JobWorkflowRunRes) => (TResult)(object)ToRes(e),
            JobWorkflowRunStep e when typeof(TResult) == typeof(JobWorkflowRunStepRes) => (TResult)(object)ToRes(e),
            JobRun e when typeof(TResult) == typeof(JobRunRes) => (TResult)(object)ToRes(e),
            JobRunParameter e when typeof(TResult) == typeof(JobRunParameterRes) => (TResult)(object)ToRes(e),
            JobRunResultEntity e when typeof(TResult) == typeof(JobRunResultRes) => (TResult)(object)ToRes(e),
            JobRunLog e when typeof(TResult) == typeof(JobRunLogRes) => (TResult)(object)ToRes(e),
            JobWorkerInstance e when typeof(TResult) == typeof(JobWorkerInstanceRes) => (TResult)(object)ToRes(e),

            JobRunRes res when typeof(TResult) == typeof(JobRunReq) => (TResult)(object)ResToReq(res),

            _ => throw Unmapped(source.GetType(), typeof(TResult))
        };

    public void Map<TSource, TDest>(TSource source, TDest destination)
    {
        switch (source, destination) {
            case (JobDefinitionReq req, JobDefinition e):
                Apply(req, e);
                break;
            case (JobParameterReq req, JobParameter e):
                Apply(req, e);
                break;
            case (JobScheduleReq req, JobSchedule e):
                Apply(req, e);
                break;
            case (JobScheduleParameterReq req, JobScheduleParameter e):
                Apply(req, e);
                break;
            case (JobTriggerReq req, JobTrigger e):
                Apply(req, e);
                break;
            case (JobTriggerParameterReq req, JobTriggerParameter e):
                Apply(req, e);
                break;
            case (JobParallelRestrictionReq req, JobParallelRestriction e):
                Apply(req, e);
                break;
            case (JobBlackoutCalendarReq req, JobBlackoutCalendar e):
                Apply(req, e);
                break;
            case (JobBlackoutWindowReq req, JobBlackoutWindow e):
                Apply(req, e);
                break;
            case (JobWorkflowReq req, JobWorkflow e):
                Apply(req, e);
                break;
            case (JobWorkflowStepReq req, JobWorkflowStep e):
                Apply(req, e);
                break;
            case (JobWorkflowRunReq req, JobWorkflowRun e):
                Apply(req, e);
                break;
            case (JobWorkflowRunStepReq req, JobWorkflowRunStep e):
                Apply(req, e);
                break;
            case (JobRunReq req, JobRun e):
                Apply(req, e);
                break;
            case (JobRunParameterReq req, JobRunParameter e):
                Apply(req, e);
                break;
            case (JobRunResultReq req, JobRunResultEntity e):
                Apply(req, e);
                break;
            case (JobRunLogReq req, JobRunLog e):
                Apply(req, e);
                break;
            case (JobWorkerInstanceReq req, JobWorkerInstance e):
                Apply(req, e);
                break;
            default:
                throw Unmapped(typeof(TSource), typeof(TDest));
        }
    }

    private static InvalidOperationException Unmapped(Type source, Type dest)
        => new($"No mapping configured from {source.Name} to {dest.Name}.");

    private static DateTime UtcNow() => DateTime.UtcNow;

    private static string FormatTime(TimeOnly value) => value.ToString("HH:mm:ss", Invariant);

    private static string? FormatTime(TimeOnly? value) => value?.ToString("HH:mm:ss", Invariant);

    private static TimeOnly ParseTime(string value) => TimeOnly.Parse(value, Invariant);

    private static TimeOnly? ParseTimeOrNull(string? value) => value is null ? null : TimeOnly.Parse(value, Invariant);

    private static string? MaskParameterValue(string? value, byte[]? encryptedValue) => encryptedValue is not null ? "***" : value;

    private static byte[]? MaskParameterEncryptedValue(byte[]? encryptedValue) => encryptedValue is not null ? null : encryptedValue;

    private static DateTime ToUtcDateTime(DateTime value) => value.Kind switch {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    // ─── Req → new entity ───────────────────────────────────────────────────

    internal static JobDefinition ReqToNew(JobDefinitionReq r)
    {
        var now = UtcNow();
        var dest = new JobDefinition {
            Name = r.Name,
            Description = r.Description,
            Type = r.Type,
            WorkerType = r.WorkerType,
            Enabled = r.Enabled,
            MaxRetryCount = r.MaxRetryCount,
            RetryBackoffSeconds = r.RetryBackoffSeconds,
            TimeoutMinutes = r.TimeoutMinutes,
            MaxConcurrentRuns = r.MaxConcurrentRuns,
            CircuitBreakerThreshold = r.CircuitBreakerThreshold,
            CircuitBreakerResetMinutes = r.CircuitBreakerResetMinutes,
            RetryBackoffType = r.RetryBackoffType.ToString(),
            Priority = r.Priority,
            RetentionDays = r.RetentionDays,
            MaxRunsPerHour = r.MaxRunsPerHour,
            ExpectedDurationMinutes = r.ExpectedDurationMinutes,
            MustStartByMinutes = r.MustStartByMinutes,
            AlertOnFailure = r.AlertOnFailure,
            AlertAfterConsecutiveFailures = r.AlertAfterConsecutiveFailures,
            AlertWebhookUrl = r.AlertWebhookUrl,
            CreatedTimestamp = now,
            JobParameters = r.CreateParameters.Select(ReqToNew).ToList(),
            JobSchedules = r.CreateSchedules.Select(ReqToNew).ToList(),
            JobTriggerJobDefinitions = r.CreateTriggers.Select(ReqToNew).ToList(),
            JobParallelRestrictionBaseJobDefinitions = r.CreateParallelRestrictions.Select(ReqToNew).ToList()
        };
        JobBlackoutCalendarEntityHelper.ApplyDefinitionBlackoutDefaults(r, dest);
        return dest;
    }

    internal static JobParameter ReqToNew(JobParameterReq r)
        => new() {
            JobDefinitionId = r.JobDefinitionId,
            Key = r.Key,
            Description = r.Description,
            Type = r.Type.ToString(),
            Value = r.Value,
            EncryptedValue = r.EncryptedValue,
            AllowMultiple = r.AllowMultiple,
            Required = r.Required,
            ValidationRegex = r.ValidationRegex,
            MinLength = r.MinLength,
            MaxLength = r.MaxLength,
            AllowedValues = r.AllowedValues,
            CreatedTimestamp = UtcNow()
        };

    internal static JobSchedule ReqToNew(JobScheduleReq r)
        => new() {
            JobDefinitionId = r.JobDefinitionId,
            MonthFlags = r.MonthFlags.ToString(),
            DayFlags = r.DayFlags.ToString(),
            Type = r.Type.ToString(),
            Times = (r.Times ?? []).Select(FormatTime).ToList(),
            StartTime = FormatTime(r.StartTime),
            EndTime = FormatTime(r.EndTime),
            IntervalMinutes = r.IntervalMinutes,
            CronExpression = r.CronExpression,
            MisfirePolicy = r.MisfirePolicy.ToString(),
            StartDateUtc = r.StartDateUtc,
            EndDateUtc = r.EndDateUtc,
            TimeZoneId = r.TimeZoneId,
            JobBlackoutCalendarId = r.JobBlackoutCalendarId,
            JobBlackoutCalendar = r.CreateBlackoutCalendar is null ? null : ReqToNew(r.CreateBlackoutCalendar),
            Description = r.Description,
            Enabled = r.Enabled,
            CreatedTimestamp = UtcNow(),
            JobScheduleParameters = r.CreateScheduleParameters.Select(ReqToNew).ToList()
        };

    internal static JobScheduleParameter ReqToNew(JobScheduleParameterReq r)
        => new() {
            JobScheduleId = r.JobScheduleId,
            Key = r.Key,
            Description = r.Description,
            Type = r.Type.ToString(),
            Value = r.Value,
            Enabled = r.Enabled,
            CreatedTimestamp = UtcNow()
        };

    internal static JobTrigger ReqToNew(JobTriggerReq r)
        => new() {
            JobDefinitionId = r.JobDefinitionId,
            TriggersJobDefinitionId = r.TriggersJobDefinitionId,
            TriggerJobResultKey = r.JobResultKey,
            TriggerComparator = r.Comparison.ToString(),
            TriggerJobResultValue = r.JobResultValue,
            Description = r.Description,
            Enabled = r.Enabled,
            CreatedTimestamp = UtcNow(),
            JobTriggerParameters = r.CreateTriggerParameters.Select(ReqToNew).ToList()
        };

    internal static JobTriggerParameter ReqToNew(JobTriggerParameterReq r)
        => new() {
            JobTriggerId = r.JobTriggerId,
            Key = r.Key,
            Description = r.Description,
            Type = r.Type.ToString(),
            Value = r.Value,
            Enabled = r.Enabled,
            CreatedTimestamp = UtcNow()
        };

    internal static JobParallelRestriction ReqToNew(JobParallelRestrictionReq r)
        => new() {
            OtherJobDefinitionId = r.OtherJobDefinitionId,
            Description = r.Description,
            Enabled = r.Enabled,
            CreatedTimestamp = UtcNow()
        };

    internal static JobBlackoutCalendar ReqToNew(JobBlackoutCalendarReq r)
    {
        var now = UtcNow();
        return new() {
            Name = r.Name,
            Description = r.Description,
            Enabled = r.Enabled,
            CreatedTimestamp = now,
            JobBlackoutWindows = r.CreateBlackoutWindows.Select(w => {
                var window = ReqToNew(w);
                window.CreatedTimestamp = now;
                return window;
            }).ToList()
        };
    }

    internal static JobBlackoutWindow ReqToNew(JobBlackoutWindowReq r)
        => new() {
            JobBlackoutCalendarId = r.JobBlackoutCalendarId,
            Name = r.Name,
            DayFlags = r.DayFlags.ToString(),
            StartDateUtc = r.StartDateUtc,
            EndDateUtc = r.EndDateUtc,
            StartTime = FormatTime(r.StartTime),
            EndTime = FormatTime(r.EndTime),
            Policy = r.Policy.ToString(),
            Enabled = r.Enabled,
            CreatedTimestamp = UtcNow()
        };

    internal static JobWorkflow ReqToNew(JobWorkflowReq r)
        => new() {
            Name = r.Name,
            Description = r.Description,
            Enabled = r.Enabled,
            CreatedTimestamp = UtcNow(),
            JobWorkflowSteps = r.CreateSteps.Select(ReqToNew).ToList()
        };

    internal static JobWorkflowStep ReqToNew(JobWorkflowStepReq r)
        => new() {
            JobWorkflowId = r.JobWorkflowId,
            JobDefinitionId = r.JobDefinitionId,
            StepName = r.StepName,
            StepOrder = r.StepOrder,
            DependsOnStepIds = r.DependsOnStepIds,
            FailurePolicy = r.FailurePolicy.ToString(),
            ParametersJson = r.ParametersJson,
            Enabled = r.Enabled,
            CreatedTimestamp = UtcNow()
        };

    internal static JobWorkflowRun ReqToNew(JobWorkflowRunReq r)
        => new() {
            JobWorkflowId = r.JobWorkflowId,
            State = r.State,
            StartedTimestamp = r.StartedTimestamp,
            FinishedTimestamp = r.FinishedTimestamp,
            CreatedTimestamp = UtcNow(),
            JobWorkflowRunSteps = r.CreateRunSteps.Select(ReqToNew).ToList()
        };

    internal static JobWorkflowRunStep ReqToNew(JobWorkflowRunStepReq r)
        => new() {
            JobWorkflowRunId = r.JobWorkflowRunId,
            JobWorkflowStepId = r.JobWorkflowStepId,
            JobRunId = r.JobRunId,
            State = r.State,
            CreatedTimestamp = UtcNow()
        };

    internal static JobRun ReqToNew(JobRunReq r)
        => new() {
            JobDefinitionId = r.JobDefinitionId,
            JobScheduleId = r.JobScheduleId,
            JobTriggerId = r.JobTriggerId,
            TriggeredByJobRunId = r.TriggeredByJobRunId,
            ReRanFromJobRunId = r.ReRanFromJobRunId,
            CreatedBy = r.CreatedBy,
            AllowTriggers = r.AllowTriggers,
            Result = r.Result,
            ScheduledSlotUtc = r.ScheduledSlotUtc,
            RetryAttempt = r.RetryAttempt,
            Priority = r.Priority ?? 0,
            IdempotencyKey = r.IdempotencyKey,
            DryRun = r.DryRun,
            TraceId = r.TraceId,
            ParentJobRunId = r.ParentJobRunId,
            BatchIndex = r.BatchIndex,
            BatchTotal = r.BatchTotal,
            CreatedTimestamp = UtcNow(),
            JobRunParameters = r.JobRunParameters.Select(ReqToNew).ToList()
        };

    internal static JobRunParameter ReqToNew(JobRunParameterReq r)
        => new() {
            Key = r.Key,
            Description = r.Description,
            Type = r.Type.ToString(),
            Value = r.Value,
            EncryptedValue = r.EncryptedValue
        };

    internal static JobRunResultEntity ReqToNew(JobRunResultReq r)
        => new() {
            Key = r.Key,
            Type = r.Type.ToString(),
            Value = r.Value
        };

    internal static JobRunLog ReqToNew(JobRunLogReq r)
        => new() {
            Level = r.Level.ToString(),
            Message = r.Message,
            Context = r.Context,
            StackTrace = r.StackTrace,
            Timestamp = r.Timestamp
        };

    internal static JobWorkerInstance ReqToNew(JobWorkerInstanceReq r)
        => new() {
            WorkerType = r.WorkerType,
            MachineName = r.MachineName,
            ProcessId = r.ProcessId,
            State = r.State.ToString(),
            InFlightCount = r.InFlightCount,
            StartedTimestamp = ToUtcDateTime(r.StartedTimestamp),
            LastHeartbeatUtc = ToUtcDateTime(r.LastHeartbeatUtc),
            CreatedTimestamp = UtcNow()
        };

    // ─── Entity → Res ───────────────────────────────────────────────────────

    internal static JobDefinitionRes ToRes(JobDefinition e)
        => new(
            e.Id, e.Name, e.Description, e.Type, e.WorkerType, e.Enabled,
            e.JobParameters.Select(ToRes).ToList(),
            e.JobSchedules.Select(ToRes).ToList(),
            e.JobTriggerJobDefinitions.Select(ToRes).ToList(),
            e.JobParallelRestrictionBaseJobDefinitions.Select(ToRes).ToList(),
            e.MaxRetryCount, e.RetryBackoffSeconds, e.TimeoutMinutes, e.MaxConcurrentRuns, e.CircuitBreakerThreshold,
            e.CircuitBreakerResetMinutes, e.CircuitBreakerTrippedAt, Enum.Parse<JobRetryBackoffType>(e.RetryBackoffType), e.Priority, e.RetentionDays,
            e.MaxRunsPerHour, e.ExpectedDurationMinutes, e.MustStartByMinutes, e.AlertOnFailure, e.AlertAfterConsecutiveFailures, e.AlertWebhookUrl,
            e.DefinitionVersion);

    internal static JobParameterRes ToRes(JobParameter e)
        => new(
            e.Id, e.JobDefinitionId, e.Key, e.Description, Enum.Parse<JobParameterType>(e.Type), MaskParameterValue(e.Value, e.EncryptedValue),
            MaskParameterEncryptedValue(e.EncryptedValue), e.AllowMultiple, true, e.Required, e.ValidationRegex, e.MinLength, e.MaxLength, e.AllowedValues);

    internal static JobScheduleRes ToRes(JobSchedule e)
        => new(
            e.Id, e.JobDefinitionId, Enum.Parse<MonthFlags>(e.MonthFlags), Enum.Parse<DayFlags>(e.DayFlags), Enum.Parse<ScheduleType>(e.Type),
            (e.Times ?? []).Select(ParseTime).ToList(), ParseTimeOrNull(e.StartTime), ParseTimeOrNull(e.EndTime), e.IntervalMinutes, e.Description, e.Enabled,
            e.JobScheduleParameters.Select(ToRes).ToList(), e.CronExpression, Enum.Parse<JobMisfirePolicy>(e.MisfirePolicy), e.StartDateUtc, e.EndDateUtc,
            e.TimeZoneId, e.JobBlackoutCalendarId, e.JobBlackoutCalendar is null ? null : ToRes(e.JobBlackoutCalendar));

    internal static JobScheduleParameterRes ToRes(JobScheduleParameter e)
        => new(e.Id, e.JobScheduleId, e.Key, Enum.Parse<JobParameterType>(e.Type), e.Value, e.Description, null, e.Enabled);

    internal static JobTriggerRes ToRes(JobTrigger e)
        => new(
            e.Id, e.TriggersJobDefinitionId, e.TriggerJobResultKey, Enum.Parse<ComparisonOperatorEnum>(e.TriggerComparator), e.TriggerJobResultValue,
            e.Description, e.Enabled, null, e.JobTriggerParameters.Select(ToRes).ToList(), null);

    internal static JobTriggerParameterRes ToRes(JobTriggerParameter e)
        => new(e.Id, e.JobTriggerId, e.Key, Enum.Parse<JobParameterType>(e.Type), e.Value, e.Description, null, e.Enabled);

    internal static JobParallelRestrictionRes ToRes(JobParallelRestriction e)
        => new(e.Id, e.BaseJobDefinitionId, e.OtherJobDefinitionId, e.Description, e.Enabled, null);

    internal static JobBlackoutCalendarRes ToRes(JobBlackoutCalendar e)
        => new(e.Id, e.Name, e.Description, e.Enabled, e.JobBlackoutWindows.Select(ToRes).ToList());

    internal static JobBlackoutWindowRes ToRes(JobBlackoutWindow e)
        => new(
            e.Id, e.JobBlackoutCalendarId, e.Name, Enum.Parse<DayFlags>(e.DayFlags), ParseTime(e.StartTime), ParseTime(e.EndTime),
            Enum.Parse<JobBlackoutPolicy>(e.Policy), e.Enabled, e.StartDateUtc, e.EndDateUtc);

    internal static JobWorkflowRes ToRes(JobWorkflow e)
        => new(e.Id, e.Name, e.Description, e.Enabled, e.JobWorkflowSteps.Select(ToRes).ToList());

    internal static JobWorkflowStepRes ToRes(JobWorkflowStep e)
        => new(
            e.Id, e.JobWorkflowId, e.JobDefinitionId, e.StepName, e.StepOrder, e.DependsOnStepIds, Enum.Parse<JobWorkflowFailurePolicy>(e.FailurePolicy),
            e.ParametersJson, e.Enabled, null);

    internal static JobWorkflowRunRes ToRes(JobWorkflowRun e)
        => new(e.Id, e.JobWorkflowId, e.State, e.StartedTimestamp, e.FinishedTimestamp, e.CreatedTimestamp, e.JobWorkflowRunSteps.Select(ToRes).ToList(), null);

    internal static JobWorkflowRunStepRes ToRes(JobWorkflowRunStep e)
        => new(e.Id, e.JobWorkflowRunId, e.JobWorkflowStepId, e.JobRunId, e.State, null, null);

    internal static JobRunRes ToRes(JobRun e)
        => new() {
            Id = e.Id,
            State = e.State,
            Result = e.Result,
            CreatedTimestamp = e.CreatedTimestamp,
            StartedTimestamp = e.StartedTimestamp,
            FinishedTimestamp = e.FinishedTimestamp,
            AllowTriggers = e.AllowTriggers,
            JobDefinitionId = e.JobDefinitionId,
            JobDefinition = e.JobDefinition is null ? null : ToRes(e.JobDefinition),
            JobScheduleId = e.JobScheduleId,
            JobSchedule = null,
            JobTriggerId = e.JobTriggerId,
            JobTrigger = null,
            ReRanFromJobRun = null,
            ScheduledSlotUtc = e.ScheduledSlotUtc,
            RetryAttempt = e.RetryAttempt,
            LastHeartbeatUtc = e.LastHeartbeatUtc,
            Priority = e.Priority,
            ProgressPercent = e.ProgressPercent,
            ProgressMessage = e.ProgressMessage,
            IdempotencyKey = e.IdempotencyKey,
            DryRun = e.DryRun,
            SlaBreached = e.SlaBreached,
            TraceId = e.TraceId,
            ParentJobRunId = e.ParentJobRunId,
            BatchIndex = e.BatchIndex,
            BatchTotal = e.BatchTotal,
            DefinitionAuditVersion = e.DefinitionAuditVersion,
            JobRunParameters = e.JobRunParameters.Select(ToRes).ToList(),
            JobRunResults = e.JobRunResults.Select(ToRes).ToList(),
            JobRunLogs = e.JobRunLogs.Select(ToRes).ToList()
        };

    internal static JobRunParameterRes ToRes(JobRunParameter e)
        => new(
            e.Id, e.JobRunId, e.Key, Enum.Parse<JobParameterType>(e.Type), MaskParameterValue(e.Value, e.EncryptedValue), e.Description,
            MaskParameterEncryptedValue(e.EncryptedValue), false);

    internal static JobRunResultRes ToRes(JobRunResultEntity e)
        => new(e.Id, e.JobRunId, e.Key, Enum.Parse<JobParameterType>(e.Type), e.Value);

    internal static JobRunLogRes ToRes(JobRunLog e)
        => new(e.Id, e.JobRunId, Enum.Parse<JobLogLevel>(e.Level), e.Message, e.Context, e.StackTrace, e.Timestamp);

    internal static JobWorkerInstanceRes ToRes(JobWorkerInstance e)
        => new(e.Id, e.WorkerType, e.MachineName, e.ProcessId, Enum.Parse<JobWorkerInstanceState>(e.State), e.InFlightCount, e.StartedTimestamp, e.LastHeartbeatUtc);

    internal static JobRunReq ResToReq(JobRunRes r)
        => new() {
            JobDefinitionId = r.JobDefinitionId,
            JobScheduleId = r.JobScheduleId,
            JobTriggerId = r.JobTriggerId,
            ReRanFromJobRunId = null,
            CreatedBy = "Unknown",
            AllowTriggers = r.AllowTriggers,
            Result = r.Result,
            // IdempotencyKey and ScheduledSlotUtc are unique per run (both are backed by unique indexes) and must never be copied when
            // cloning a run for rerun/child creation — a copied key silently resolves to the original run instead of creating a new one.
            ScheduledSlotUtc = null,
            RetryAttempt = r.RetryAttempt,
            Priority = r.Priority,
            IdempotencyKey = null,
            DryRun = r.DryRun,
            TraceId = r.TraceId,
            ParentJobRunId = r.ParentJobRunId,
            BatchIndex = r.BatchIndex,
            BatchTotal = r.BatchTotal,
            JobRunParameters = (r.JobRunParameters ?? []).Select(p => new JobRunParameterReq {
                Key = p.Key,
                Description = p.Description,
                Type = p.Type,
                Value = p.Value,
                EncryptedValue = p.EncryptedValue,
                Enabled = p.Enabled
            }).ToList()
        };

    // ─── Req → existing entity (update/patch) ───────────────────────────────

    private static void Apply(JobDefinitionReq r, JobDefinition e)
    {
        e.Name = r.Name;
        e.Description = r.Description;
        e.Type = r.Type;
        e.WorkerType = r.WorkerType;
        e.Enabled = r.Enabled;
        e.MaxRetryCount = r.MaxRetryCount;
        e.RetryBackoffSeconds = r.RetryBackoffSeconds;
        e.TimeoutMinutes = r.TimeoutMinutes;
        e.MaxConcurrentRuns = r.MaxConcurrentRuns;
        e.CircuitBreakerThreshold = r.CircuitBreakerThreshold;
        e.CircuitBreakerResetMinutes = r.CircuitBreakerResetMinutes;
        e.RetryBackoffType = r.RetryBackoffType.ToString();
        e.Priority = r.Priority;
        e.RetentionDays = r.RetentionDays;
        e.MaxRunsPerHour = r.MaxRunsPerHour;
        e.ExpectedDurationMinutes = r.ExpectedDurationMinutes;
        e.MustStartByMinutes = r.MustStartByMinutes;
        e.AlertOnFailure = r.AlertOnFailure;
        e.AlertAfterConsecutiveFailures = r.AlertAfterConsecutiveFailures;
        e.AlertWebhookUrl = r.AlertWebhookUrl;
    }

    private static void Apply(JobParameterReq r, JobParameter e)
    {
        e.JobDefinitionId = r.JobDefinitionId;
        e.Key = r.Key;
        e.Description = r.Description;
        e.Type = r.Type.ToString();
        e.Value = r.Value;
        e.EncryptedValue = r.EncryptedValue;
        e.AllowMultiple = r.AllowMultiple;
        e.Required = r.Required;
        e.ValidationRegex = r.ValidationRegex;
        e.MinLength = r.MinLength;
        e.MaxLength = r.MaxLength;
        e.AllowedValues = r.AllowedValues;
    }

    private static void Apply(JobScheduleReq r, JobSchedule e)
    {
        e.JobDefinitionId = r.JobDefinitionId;
        e.MonthFlags = r.MonthFlags.ToString();
        e.DayFlags = r.DayFlags.ToString();
        e.Type = r.Type.ToString();
        e.Times = (r.Times ?? []).Select(FormatTime).ToList();
        e.StartTime = FormatTime(r.StartTime);
        e.EndTime = FormatTime(r.EndTime);
        e.IntervalMinutes = r.IntervalMinutes;
        e.CronExpression = r.CronExpression;
        e.MisfirePolicy = r.MisfirePolicy.ToString();
        e.StartDateUtc = r.StartDateUtc;
        e.EndDateUtc = r.EndDateUtc;
        e.TimeZoneId = r.TimeZoneId;
        e.JobBlackoutCalendarId = r.JobBlackoutCalendarId;
        e.Description = r.Description;
        e.Enabled = r.Enabled;
    }

    private static void Apply(JobScheduleParameterReq r, JobScheduleParameter e)
    {
        e.JobScheduleId = r.JobScheduleId;
        e.Key = r.Key;
        e.Description = r.Description;
        e.Type = r.Type.ToString();
        e.Value = r.Value;
        e.Enabled = r.Enabled;
    }

    private static void Apply(JobTriggerReq r, JobTrigger e)
    {
        e.JobDefinitionId = r.JobDefinitionId;
        e.TriggersJobDefinitionId = r.TriggersJobDefinitionId;
        e.TriggerJobResultKey = r.JobResultKey;
        e.TriggerComparator = r.Comparison.ToString();
        e.TriggerJobResultValue = r.JobResultValue;
        e.Description = r.Description;
        e.Enabled = r.Enabled;
    }

    private static void Apply(JobTriggerParameterReq r, JobTriggerParameter e)
    {
        e.JobTriggerId = r.JobTriggerId;
        e.Key = r.Key;
        e.Description = r.Description;
        e.Type = r.Type.ToString();
        e.Value = r.Value;
        e.Enabled = r.Enabled;
    }

    private static void Apply(JobParallelRestrictionReq r, JobParallelRestriction e)
    {
        e.OtherJobDefinitionId = r.OtherJobDefinitionId;
        e.Description = r.Description;
        e.Enabled = r.Enabled;
    }

    private static void Apply(JobBlackoutCalendarReq r, JobBlackoutCalendar e)
    {
        e.Name = r.Name;
        e.Description = r.Description;
        e.Enabled = r.Enabled;
    }

    private static void Apply(JobBlackoutWindowReq r, JobBlackoutWindow e)
    {
        e.JobBlackoutCalendarId = r.JobBlackoutCalendarId;
        e.Name = r.Name;
        e.DayFlags = r.DayFlags.ToString();
        e.StartDateUtc = r.StartDateUtc;
        e.EndDateUtc = r.EndDateUtc;
        e.StartTime = FormatTime(r.StartTime);
        e.EndTime = FormatTime(r.EndTime);
        e.Policy = r.Policy.ToString();
        e.Enabled = r.Enabled;
    }

    private static void Apply(JobWorkflowReq r, JobWorkflow e)
    {
        e.Name = r.Name;
        e.Description = r.Description;
        e.Enabled = r.Enabled;
    }

    private static void Apply(JobWorkflowStepReq r, JobWorkflowStep e)
    {
        e.JobWorkflowId = r.JobWorkflowId;
        e.JobDefinitionId = r.JobDefinitionId;
        e.StepName = r.StepName;
        e.StepOrder = r.StepOrder;
        e.DependsOnStepIds = r.DependsOnStepIds;
        e.FailurePolicy = r.FailurePolicy.ToString();
        e.ParametersJson = r.ParametersJson;
        e.Enabled = r.Enabled;
    }

    private static void Apply(JobWorkflowRunReq r, JobWorkflowRun e)
    {
        e.JobWorkflowId = r.JobWorkflowId;
        e.State = r.State;
        e.StartedTimestamp = r.StartedTimestamp;
        e.FinishedTimestamp = r.FinishedTimestamp;
    }

    private static void Apply(JobWorkflowRunStepReq r, JobWorkflowRunStep e)
    {
        e.JobWorkflowRunId = r.JobWorkflowRunId;
        e.JobWorkflowStepId = r.JobWorkflowStepId;
        e.JobRunId = r.JobRunId;
        e.State = r.State;
    }

    private static void Apply(JobRunReq r, JobRun e)
    {
        e.JobDefinitionId = r.JobDefinitionId;
        e.JobScheduleId = r.JobScheduleId;
        e.JobTriggerId = r.JobTriggerId;
        e.TriggeredByJobRunId = r.TriggeredByJobRunId;
        e.ReRanFromJobRunId = r.ReRanFromJobRunId;
        e.CreatedBy = r.CreatedBy;
        e.AllowTriggers = r.AllowTriggers;
        e.Result = r.Result;
        e.ScheduledSlotUtc = r.ScheduledSlotUtc;
        e.RetryAttempt = r.RetryAttempt;
        e.Priority = r.Priority ?? e.Priority;
        e.IdempotencyKey = r.IdempotencyKey;
        e.DryRun = r.DryRun;
        e.TraceId = r.TraceId;
        e.ParentJobRunId = r.ParentJobRunId;
        e.BatchIndex = r.BatchIndex;
        e.BatchTotal = r.BatchTotal;
    }

    private static void Apply(JobRunParameterReq r, JobRunParameter e)
    {
        e.Key = r.Key;
        e.Description = r.Description;
        e.Type = r.Type.ToString();
        e.Value = r.Value;
        e.EncryptedValue = r.EncryptedValue;
    }

    private static void Apply(JobRunResultReq r, JobRunResultEntity e)
    {
        e.Key = r.Key;
        e.Type = r.Type.ToString();
        e.Value = r.Value;
    }

    private static void Apply(JobRunLogReq r, JobRunLog e)
    {
        e.Level = r.Level.ToString();
        e.Message = r.Message;
        e.Context = r.Context;
        e.StackTrace = r.StackTrace;
        e.Timestamp = r.Timestamp;
    }

    private static void Apply(JobWorkerInstanceReq r, JobWorkerInstance e)
    {
        e.WorkerType = r.WorkerType;
        e.MachineName = r.MachineName;
        e.ProcessId = r.ProcessId;
        e.State = r.State.ToString();
        e.InFlightCount = r.InFlightCount;
        e.StartedTimestamp = ToUtcDateTime(r.StartedTimestamp);
        e.LastHeartbeatUtc = ToUtcDateTime(r.LastHeartbeatUtc);
    }
}
