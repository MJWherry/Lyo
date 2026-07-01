using Lyo.Common.Enums;
using Lyo.Common.Records;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
using Lyo.Query.Models.Enums;
using Lyo.Schedule.Models;
#if NET6_0_OR_GREATER
using TimeOnly = System.TimeOnly;

#else
using TimeOnly = Lyo.DateAndTime.TimeOnlyModel;
#endif

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
        _request.CreateSchedules.Add(builder.Build());
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

        _request.CreateSchedules.Add(schedule);
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

        _request.CreateSchedules.Add(schedule);
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

    public JobDefinitionReq Build() => _request;

    public static JobDefinitionBuilder New(string definitionName, string? description = null) => new(definitionName, description);
}