#if NET6_0_OR_GREATER
using TimeOnly = System.TimeOnly;
#else
using TimeOnly = Lyo.DateAndTime.TimeOnlyModel;
#endif
using Lyo.Common.Enums;

namespace Lyo.Schedule.Models;

/// <summary>Fluent builder for creating ScheduleDefinition instances.</summary>
public sealed class ScheduleDefinitionBuilder
{
    private DayFlags _dayFlags = DayFlags.EveryDay;
    private string? _description;
    private bool _enabled = true;
    private TimeOnly? _endTime;
    private DateTime? _executeAt;
    private int? _intervalMinutes;
    private MonthFlags _monthFlags = MonthFlags.EveryMonth;
    private TimeOnly? _startTime;
    private List<TimeOnly>? _times;
    private TimeZoneInfo? _timeZone;
    private ScheduleType _type;

    /// <summary>Sets the description.</summary>
    public ScheduleDefinitionBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    /// <summary>Sets the time zone for schedule interpretation. Uses system local time if not set.</summary>
    public ScheduleDefinitionBuilder WithTimeZone(TimeZoneInfo timeZone)
    {
        _timeZone = timeZone;
        return this;
    }

    /// <summary>Enables or disables the schedule.</summary>
    public ScheduleDefinitionBuilder Enabled(bool enabled = true)
    {
        _enabled = enabled;
        return this;
    }

    /// <summary>Sets the schedule to run every day.</summary>
    public ScheduleDefinitionBuilder EveryDay()
    {
        _dayFlags = DayFlags.EveryDay;
        _monthFlags = MonthFlags.EveryMonth;
        return this;
    }

    /// <summary>Sets the schedule to run on weekdays only.</summary>
    public ScheduleDefinitionBuilder Weekdays()
    {
        _dayFlags = DayFlags.Weekdays;
        _monthFlags = MonthFlags.EveryMonth;
        return this;
    }

    /// <summary>Sets which days of the week the schedule runs.</summary>
    public ScheduleDefinitionBuilder SetDays(DayFlags days)
    {
        _dayFlags = days;
        return this;
    }

    /// <summary>Sets which months the schedule runs.</summary>
    public ScheduleDefinitionBuilder SetMonths(MonthFlags months)
    {
        _monthFlags = months;
        return this;
    }

    /// <summary>Sets specific times to run (SetTimes schedule).</summary>
    public ScheduleDefinitionBuilder SetTimes(params string[] times)
    {
        _type = ScheduleType.SetTimes;
        _times = times.Select(s => TimeOnly.Parse(s)).ToList();
        return this;
    }

    /// <summary>Sets specific times to run (SetTimes schedule).</summary>
    public ScheduleDefinitionBuilder SetTimes(params TimeOnly[] times)
    {
        _type = ScheduleType.SetTimes;
        _times = times.ToList();
        return this;
    }

    /// <summary>Sets an interval-based schedule within a time window.</summary>
    public ScheduleDefinitionBuilder SetInterval(string startTime, string endTime, int intervalMinutes)
    {
        _type = ScheduleType.Interval;
        _startTime = TimeOnly.Parse(startTime);
        _endTime = TimeOnly.Parse(endTime);
        _intervalMinutes = intervalMinutes;
        return this;
    }

    /// <summary>Sets an interval-based schedule within a time window.</summary>
    public ScheduleDefinitionBuilder SetInterval(TimeOnly startTime, TimeOnly endTime, int intervalMinutes)
    {
        _type = ScheduleType.Interval;
        _startTime = startTime;
        _endTime = endTime;
        _intervalMinutes = intervalMinutes;
        return this;
    }

    /// <summary>Sets a one-shot schedule to run at a specific date/time.</summary>
    public ScheduleDefinitionBuilder SetExecuteAt(DateTime executeAt)
    {
        _type = ScheduleType.OneShot;
        _executeAt = executeAt.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(executeAt, DateTimeKind.Utc) : executeAt.ToUniversalTime();
        return this;
    }

    /// <summary>Builds the ScheduleDefinition.</summary>
    public ScheduleDefinition Build() => new(_type, _dayFlags, _monthFlags, _times, _startTime, _endTime, _intervalMinutes, _executeAt, _timeZone, _enabled, _description);
}