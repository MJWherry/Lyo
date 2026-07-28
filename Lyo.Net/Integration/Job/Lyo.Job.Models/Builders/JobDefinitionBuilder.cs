#if NET6_0_OR_GREATER
using TimeOnly = System.TimeOnly;
#else
using TimeOnly = Lyo.DateAndTime.TimeOnlyModel;
#endif
using Lyo.Common.Enums;
using Lyo.Common.Records;
using Lyo.Exceptions;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
using Lyo.Query.Models.Enums;
using Lyo.Schedule.Models;

namespace Lyo.Job.Models.Builders;

public class JobDefinitionBuilder(JobDefinitionReq? request = null)
{
    private readonly JobDefinitionReq _request = request ?? new JobDefinitionReq("New Job");

    public JobDefinitionBuilder(string definitionName, string? description = null)
        : this(new(definitionName, description)) { }

    public JobDefinitionBuilder SetDescription(string description)
    {
        _request.Description = description;
        return this;
    }

    public JobDefinitionBuilder SetType(string jobType)
    {
        _request.Type = jobType;
        return this;
    }

    public JobDefinitionBuilder ForCSharpWorker()
    {
        _request.WorkerType = ProgrammingLanguageInfo.CSharp.ShortName;
        return this;
    }

    public JobDefinitionBuilder ForPythonWorker()
    {
        _request.WorkerType = ProgrammingLanguageInfo.Python.ShortName;
        return this;
    }

    public JobDefinitionBuilder AsImportInCSharp()
    {
        _request.WorkerType = ProgrammingLanguageInfo.CSharp.ShortName;
        _request.Type = "Import";
        return this;
    }

    public JobDefinitionBuilder AddSchedule(Action<JobScheduleBuilder> configureSchedule)
    {
        var builder = new JobScheduleBuilder();
        configureSchedule(builder);
        _request.CreateSchedules.Add(ApplyScheduleBlackoutDefaults(builder.Build()));
        return this;
    }

    /// <summary>Links an existing blackout calendar to every schedule on this definition (unless a schedule overrides it).</summary>
    public JobDefinitionBuilder WithBlackoutCalendar(Guid jobBlackoutCalendarId)
    {
        _request.JobBlackoutCalendarId = jobBlackoutCalendarId;
        _request.CreateBlackoutCalendar = null;
        CascadeBlackoutToExistingSchedules();
        return this;
    }

    /// <summary>Creates an inline blackout calendar on this definition and applies it to every schedule (unless a schedule overrides it).</summary>
    public JobDefinitionBuilder WithBlackoutCalendar(string name, Action<JobBlackoutCalendarBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        var builder = JobBlackoutCalendarBuilder.New(name);
        configure(builder);
        _request.CreateBlackoutCalendar = builder.Build();
        _request.JobBlackoutCalendarId = null;
        CascadeBlackoutToExistingSchedules();
        return this;
    }

    /// <summary>Creates an inline blackout calendar on this definition and applies it to every schedule (unless a schedule overrides it).</summary>
    public JobDefinitionBuilder WithBlackoutCalendar(Action<JobBlackoutCalendarBuilder> configure) => WithBlackoutCalendar("Blackout", configure);

    /// <summary>Adds a do-not-run window to the definition-level inline blackout calendar.</summary>
    public JobDefinitionBuilder AddBlackoutWindow(
        string name,
        DayFlags days,
        string startTime,
        string endTime,
        JobBlackoutPolicy policy = JobBlackoutPolicy.Skip,
        bool enabled = true)
        => AddBlackoutWindow(name, days, TimeOnly.Parse(startTime), TimeOnly.Parse(endTime), policy, enabled);

    /// <summary>Adds a do-not-run window to the definition-level inline blackout calendar.</summary>
    public JobDefinitionBuilder AddBlackoutWindow(
        string name,
        DayFlags days,
        TimeOnly startTime,
        TimeOnly endTime,
        JobBlackoutPolicy policy = JobBlackoutPolicy.Skip,
        bool enabled = true)
    {
        EnsureDefinitionBlackoutCalendar();
        _request.CreateBlackoutCalendar!.CreateBlackoutWindows.Add(
            new() {
                Name = name,
                DayFlags = days,
                StartTime = startTime,
                EndTime = endTime,
                Policy = policy,
                Enabled = enabled
            });

        CascadeBlackoutToExistingSchedules();
        return this;
    }

    public JobDefinitionBuilder AddSchedule(MonthFlags months, DayFlags days, params string[] times)
    {
        var schedule = new JobScheduleReq {
            MonthFlags = months,
            DayFlags = days,
            Times = times.Select(i => TimeOnly.Parse(i)).ToList(),
            Enabled = true,
            Description = "",
            Type = ScheduleType.SetTimes
        };

        _request.CreateSchedules.Add(ApplyScheduleBlackoutDefaults(schedule));
        return this;
    }

    public JobDefinitionBuilder AddSchedule(MonthFlags months, DayFlags days, TimeOnly startTime, TimeOnly endTime, int intervalMinutes, string? description = null)
    {
        var schedule = new JobScheduleReq {
            MonthFlags = months,
            DayFlags = days,
            Type = ScheduleType.Interval,
            StartTime = startTime,
            EndTime = endTime,
            IntervalMinutes = intervalMinutes,
            Enabled = true,
            Description = description
        };

        _request.CreateSchedules.Add(ApplyScheduleBlackoutDefaults(schedule));
        return this;
    }

    public JobDefinitionBuilder AddDailySchedule(TimeOnly startTime, TimeOnly endTime, int intervalMinutes, string? description = null)
        => AddSchedule(MonthFlags.EveryMonth, DayFlags.EveryDay, startTime, endTime, intervalMinutes, description);

    public JobDefinitionBuilder AddDailySchedule(string startTime, string endTime, int intervalMinutes, string? description = null)
        => AddSchedule(MonthFlags.EveryMonth, DayFlags.EveryDay, TimeOnly.Parse(startTime), TimeOnly.Parse(endTime), intervalMinutes, description);

    public JobDefinitionBuilder AddWorkDaySchedule(TimeOnly startTime, TimeOnly endTime, int intervalMinutes, string? description = null)
        => AddSchedule(MonthFlags.EveryMonth, DayFlags.Weekdays, startTime, endTime, intervalMinutes, description);

    public JobDefinitionBuilder AddWorkDaySchedule(string startTime, string endTime, int intervalMinutes, string? description = null)
        => AddSchedule(MonthFlags.EveryMonth, DayFlags.Weekdays, TimeOnly.Parse(startTime), TimeOnly.Parse(endTime), intervalMinutes, description);

    public JobDefinitionBuilder AddJobParameter(string key, JobParameterType type, object? value = null, string? description = null, bool required = true)
    {
        var parameter = new JobParameterReq {
            Description = description,
            Key = key,
            Type = type,
            Value = value?.ToString(),
            Required = required
        };

        _request.CreateParameters.Add(parameter);
        return this;
    }

    public JobDefinitionBuilder AddEncryptedJobParameter(string key, JobParameterType type, byte[]? value = null, string? description = null)
    {
        var parameter = new JobParameterReq {
            Description = description,
            Key = key,
            Type = type,
            EncryptedValue = value
        };

        _request.CreateParameters.Add(parameter);
        return this;
    }

    public JobDefinitionBuilder AddJobTrigger(
        Guid triggersJobDefinitionId,
        string jobResultKey,
        ComparisonOperatorEnum comparator,
        string? jobResultValue = null,
        string? description = null,
        bool? enabled = null)
    {
        var trigger = new JobTriggerReq {
            TriggersJobDefinitionId = triggersJobDefinitionId,
            JobResultKey = jobResultKey,
            Comparison = comparator,
            JobResultValue = jobResultValue,
            Description = description,
            Enabled = enabled ?? true
        };

        _request.CreateTriggers.Add(trigger);
        return this;
    }

    public JobDefinitionBuilder AddJobTrigger(Guid triggersJobDefinitionId, Action<JobTriggerBuilder> configureTrigger)
    {
        var builder = new JobTriggerBuilder();
        configureTrigger(builder);
        var trigger = builder.Build();
        trigger.TriggersJobDefinitionId = triggersJobDefinitionId;
        _request.CreateTriggers.Add(trigger);
        return this;
    }

    public JobDefinitionBuilder AddJobParallelRestriction(Guid forbidsJobDefinitionId, string? description = null, bool? enabled = true)
    {
        _request.CreateParallelRestrictions.Add(new(forbidsJobDefinitionId, description, enabled));
        return this;
    }

    public JobDefinitionBuilder AddPaginationAmount(int pageAmount, bool required = false)
        => AddJobParameter(Constants.Data.JobRunParameterKey.PaginationAmount, JobParameterType.Int, pageAmount, required: required);

    public JobDefinitionBuilder AddEmailTo(string email) => AddJobParameter($"{Constants.Data.JobRunParameterKey.EmailToPrefix}{Guid.NewGuid()}", JobParameterType.String, email);

    public JobDefinitionBuilder AddEmailCc(string email) => AddJobParameter($"{Constants.Data.JobRunParameterKey.EmailCcPrefix}{Guid.NewGuid()}", JobParameterType.String, email);

    public JobDefinitionBuilder AddEmailBcc(string email) => AddJobParameter($"{Constants.Data.JobRunParameterKey.EmailBccPrefix}{Guid.NewGuid()}", JobParameterType.String, email);

    public JobDefinitionBuilder AddEmailAttachment(Guid fileId, string? fileName)
    {
        var id = Guid.NewGuid();
        if (!string.IsNullOrEmpty(fileName))
            AddJobParameter($"{Constants.Data.JobRunParameterKey.EmailAttachmentNamePrefix}{id}", JobParameterType.String, fileId);

        return AddJobParameter($"{Constants.Data.JobRunParameterKey.EmailAttachmentPrefix}{id}", JobParameterType.String, fileId);
    }

    /// <summary>Configures automatic retries: <paramref name="maxRetryCount" /> attempts with <paramref name="backoffSeconds" /> base delay growing per <paramref name="backoffType" />.</summary>
    public JobDefinitionBuilder WithRetries(int maxRetryCount, int backoffSeconds = 0, JobRetryBackoffType backoffType = JobRetryBackoffType.Linear)
    {
        _request.MaxRetryCount = maxRetryCount;
        _request.RetryBackoffSeconds = backoffSeconds;
        _request.RetryBackoffType = backoffType;
        return this;
    }

    /// <summary>Sets the heartbeat timeout in minutes after which a Running job is considered dead.</summary>
    public JobDefinitionBuilder WithTimeout(int timeoutMinutes)
    {
        _request.TimeoutMinutes = timeoutMinutes;
        return this;
    }

    /// <summary>Limits the number of concurrent active runs (Queued + Running). 0 = unlimited.</summary>
    public JobDefinitionBuilder WithMaxConcurrentRuns(int maxConcurrentRuns)
    {
        _request.MaxConcurrentRuns = maxConcurrentRuns;
        return this;
    }

    /// <summary>Enables the circuit breaker: auto-disable after <paramref name="threshold" /> consecutive failures, auto-reset after <paramref name="resetMinutes" /> minutes.</summary>
    public JobDefinitionBuilder WithCircuitBreaker(int threshold, int resetMinutes = 0)
    {
        _request.CircuitBreakerThreshold = threshold;
        _request.CircuitBreakerResetMinutes = resetMinutes;
        return this;
    }

    /// <summary>Sets the message priority (0-9) applied to runs of this definition. Higher values are consumed first.</summary>
    public JobDefinitionBuilder WithPriority(int priority)
    {
        _request.Priority = priority;
        return this;
    }

    /// <summary>Sets how many days finished runs are kept before the maintenance service purges them. 0 = host default.</summary>
    public JobDefinitionBuilder WithRetention(int retentionDays)
    {
        _request.RetentionDays = retentionDays;
        return this;
    }

    /// <summary>Limits how many runs may be created per hour. 0 = unlimited.</summary>
    public JobDefinitionBuilder WithMaxRunsPerHour(int maxRunsPerHour)
    {
        _request.MaxRunsPerHour = maxRunsPerHour;
        return this;
    }

    /// <summary>Configures SLA expectations for runs of this definition.</summary>
    public JobDefinitionBuilder WithSla(int expectedDurationMinutes, int mustStartByMinutes = 0)
    {
        _request.ExpectedDurationMinutes = expectedDurationMinutes;
        _request.MustStartByMinutes = mustStartByMinutes;
        return this;
    }

    /// <summary>Enables failure alerting with optional consecutive-failure threshold and webhook URL.</summary>
    public JobDefinitionBuilder WithAlerts(bool alertOnFailure = true, int afterConsecutiveFailures = 0, string? webhookUrl = null)
    {
        _request.AlertOnFailure = alertOnFailure;
        _request.AlertAfterConsecutiveFailures = afterConsecutiveFailures;
        _request.AlertWebhookUrl = webhookUrl;
        return this;
    }

    public JobDefinitionReq Build()
    {
        for (var i = 0; i < _request.CreateSchedules.Count; i++)
            _request.CreateSchedules[i] = ApplyScheduleBlackoutDefaults(_request.CreateSchedules[i]);

        return _request;
    }

    public static JobDefinitionBuilder New(string definitionName, string? description = null) => new(definitionName, description);

    private void EnsureDefinitionBlackoutCalendar()
    {
        _request.CreateBlackoutCalendar ??= new() { Name = "Blackout", Enabled = true };
        _request.JobBlackoutCalendarId = null;
    }

    private void CascadeBlackoutToExistingSchedules()
    {
        for (var i = 0; i < _request.CreateSchedules.Count; i++)
            _request.CreateSchedules[i] = ApplyScheduleBlackoutDefaults(_request.CreateSchedules[i]);
    }

    private JobScheduleReq ApplyScheduleBlackoutDefaults(JobScheduleReq schedule)
    {
        if (schedule.JobBlackoutCalendarId.HasValue || HasScheduleInlineBlackoutOverride(schedule))
            return schedule;

        if (_request.JobBlackoutCalendarId.HasValue)
            schedule.JobBlackoutCalendarId = _request.JobBlackoutCalendarId;
        else if (_request.CreateBlackoutCalendar != null)
            schedule.CreateBlackoutCalendar = _request.CreateBlackoutCalendar;

        return schedule;
    }

    private bool HasScheduleInlineBlackoutOverride(JobScheduleReq schedule)
        => schedule.CreateBlackoutCalendar != null && !ReferenceEquals(schedule.CreateBlackoutCalendar, _request.CreateBlackoutCalendar);
}