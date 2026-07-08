using Lyo.Common.Enums;
using Lyo.Exceptions;
using Lyo.Job.Models.Enums;
using Lyo.Job.Models.Request;
using Lyo.Schedule.Models;
#if NET6_0_OR_GREATER
using TimeOnly = System.TimeOnly;

#else
using TimeOnly = Lyo.DateAndTime.TimeOnlyModel;
#endif

namespace Lyo.Job.Models.Builders;

public class JobScheduleBuilder
{
    private readonly JobScheduleReq _schedule = new();

    public JobScheduleBuilder AddScheduleParameter(string key, JobParameterType type, object? value = null, string? description = null, bool enabled = true)
    {
        _schedule.CreateScheduleParameters.Add(
            new() {
                Key = key,
                Type = type,
                Value = value?.ToString(),
                Description = description,
                Enabled = enabled
            });

        return this;
    }

    public JobScheduleBuilder SetMonths(MonthFlags months)
    {
        _schedule.MonthFlags = months;
        return this;
    }

    public JobScheduleBuilder SetDays(DayFlags days)
    {
        _schedule.DayFlags = days;
        return this;
    }

    public JobScheduleBuilder EveryDay()
    {
        _schedule.DayFlags = DayFlags.EveryDay;
        _schedule.MonthFlags = MonthFlags.EveryMonth;
        return this;
    }

    public JobScheduleBuilder Weekdays()
    {
        _schedule.DayFlags = DayFlags.Weekdays;
        _schedule.MonthFlags = MonthFlags.EveryMonth;
        return this;
    }

    public JobScheduleBuilder SetTimes(params string[] times)
    {
        _schedule.Type = ScheduleType.SetTimes;
        _schedule.Times = times.Select(i => TimeOnly.Parse(i)).ToList();
        return this;
    }

    public JobScheduleBuilder SetTimes(params TimeOnly[] times)
    {
        _schedule.Type = ScheduleType.SetTimes;
        _schedule.Times = times.ToList();
        return this;
    }

    public JobScheduleBuilder SetInterval(string startTime, string endTime, int intervalMinutes)
    {
        _schedule.Type = ScheduleType.Interval;
        _schedule.StartTime = TimeOnly.Parse(startTime);
        _schedule.EndTime = TimeOnly.Parse(endTime);
        _schedule.IntervalMinutes = intervalMinutes;
        return this;
    }

    public JobScheduleBuilder SetInterval(TimeOnly startTime, TimeOnly endTime, int intervalMinutes)
    {
        _schedule.Type = ScheduleType.Interval;
        _schedule.StartTime = startTime;
        _schedule.EndTime = endTime;
        _schedule.IntervalMinutes = intervalMinutes;
        return this;
    }

    public JobScheduleBuilder WithDescription(string description)
    {
        _schedule.Description = description;
        return this;
    }

    public JobScheduleBuilder Enabled(bool enabled = true)
    {
        _schedule.Enabled = enabled;
        return this;
    }

    /// <summary>Sets a standard cron expression (5- or 6-field) and switches the schedule type to Cron.</summary>
    public JobScheduleBuilder SetCron(string cronExpression)
    {
        _schedule.Type = ScheduleType.Cron;
        _schedule.CronExpression = cronExpression;
        return this;
    }

    /// <summary>Sets how slots missed while no scheduler was running are handled.</summary>
    public JobScheduleBuilder WithMisfirePolicy(JobMisfirePolicy policy)
    {
        _schedule.MisfirePolicy = policy;
        return this;
    }

    /// <summary>Restricts the schedule to a UTC validity window. Either bound may be null.</summary>
    public JobScheduleBuilder WithValidityWindow(DateTime? startDateUtc, DateTime? endDateUtc)
    {
        _schedule.StartDateUtc = startDateUtc;
        _schedule.EndDateUtc = endDateUtc;
        return this;
    }

    /// <summary>Sets the IANA/Windows time zone id used when evaluating this schedule's times.</summary>
    public JobScheduleBuilder WithTimeZone(string timeZoneId)
    {
        _schedule.TimeZoneId = timeZoneId;
        return this;
    }

    /// <summary>Associates an existing blackout calendar by id.</summary>
    public JobScheduleBuilder WithBlackoutCalendar(Guid jobBlackoutCalendarId)
    {
        _schedule.JobBlackoutCalendarId = jobBlackoutCalendarId;
        _schedule.CreateBlackoutCalendar = null;
        return this;
    }

    /// <summary>Creates and links a new inline blackout calendar when this schedule is persisted.</summary>
    public JobScheduleBuilder WithBlackoutCalendar(string name, Action<JobBlackoutCalendarBuilder> configure)
    {
        ArgumentHelpers.ThrowIfNull(configure);
        var builder = JobBlackoutCalendarBuilder.New(name);
        configure(builder);
        _schedule.CreateBlackoutCalendar = builder.Build();
        _schedule.JobBlackoutCalendarId = null;
        return this;
    }

    /// <summary>Creates and links a new inline blackout calendar when this schedule is persisted.</summary>
    public JobScheduleBuilder WithBlackoutCalendar(Action<JobBlackoutCalendarBuilder> configure)
        => WithBlackoutCalendar("Blackout", configure);

    /// <summary>Adds a do-not-run window to this schedule's inline blackout calendar.</summary>
    public JobScheduleBuilder AddBlackoutWindow(string name, DayFlags days, string startTime, string endTime, JobBlackoutPolicy policy = JobBlackoutPolicy.Skip, bool enabled = true)
        => AddBlackoutWindow(name, days, TimeOnly.Parse(startTime), TimeOnly.Parse(endTime), policy, enabled);

    /// <summary>Adds a do-not-run window to this schedule's inline blackout calendar.</summary>
    public JobScheduleBuilder AddBlackoutWindow(string name, DayFlags days, TimeOnly startTime, TimeOnly endTime, JobBlackoutPolicy policy = JobBlackoutPolicy.Skip, bool enabled = true)
    {
        EnsureScheduleBlackoutCalendar();
        _schedule.CreateBlackoutCalendar!.CreateBlackoutWindows.Add(new() {
            Name = name,
            DayFlags = days,
            StartTime = startTime,
            EndTime = endTime,
            Policy = policy,
            Enabled = enabled
        });
        return this;
    }

    /// <summary>Builds the request for API/DB persistence.</summary>
    public JobScheduleReq Build() => _schedule;

    /// <summary>Builds a ScheduleDefinition for use with Lyo.Scheduler.AddSchedule.</summary>
    public ScheduleDefinition BuildScheduleDefinition() => _schedule.ToScheduleDefinition();

    private void EnsureScheduleBlackoutCalendar()
    {
        _schedule.CreateBlackoutCalendar ??= new() { Name = "Blackout", Enabled = true };
        _schedule.JobBlackoutCalendarId = null;
    }
}