using Lyo.Common.Enums;
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

    /// <summary>Builds the request for API/DB persistence.</summary>
    public JobScheduleReq Build() => _schedule;

    /// <summary>Builds a ScheduleDefinition for use with Lyo.Scheduler.AddSchedule.</summary>
    public ScheduleDefinition BuildScheduleDefinition() => _schedule.ToScheduleDefinition();
}