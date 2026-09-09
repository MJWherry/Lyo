#if NET6_0_OR_GREATER
using TimeOnly = System.TimeOnly;
#else
using TimeOnly = Lyo.DateAndTime.TimeOnlyModel;
#endif
using System.Diagnostics;
using Lyo.Common.Core.Enums;

namespace Lyo.Schedule.Models;

/// <summary>Fluent builder that assembles <see cref="ScheduleDefinition" /> instances.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ScheduleDefinitionBuilder
{
    private string? _cronExpression;
    private DayFlags _dayFlags = DayFlags.EveryDay;
    private string? _description;
    private bool _enabled = true;
    private TimeOnly? _endTime;
    private DateTime? _executeAt;
    private int? _intervalMinutes;
    private MonthFlags _monthFlags = MonthFlags.EveryMonth;
    private TimeOnly? _startTime;
    private TimeZoneInfo? _timeZone;
    private List<TimeOnly>? _times;
    private ScheduleType _type;

    /// <summary>Sets the optional description.</summary>
    public ScheduleDefinitionBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    /// <summary>Sets the time zone used to interpret times. Falls back to system local time when unset.</summary>
    public ScheduleDefinitionBuilder WithTimeZone(TimeZoneInfo timeZone)
    {
        _timeZone = timeZone;
        return this;
    }

    /// <summary>Turns the schedule on or off.</summary>
    public ScheduleDefinitionBuilder Enabled(bool enabled = true)
    {
        _enabled = enabled;
        return this;
    }

    /// <summary>Runs the schedule every day.</summary>
    public ScheduleDefinitionBuilder EveryDay()
    {
        _dayFlags = DayFlags.EveryDay;
        _monthFlags = MonthFlags.EveryMonth;
        return this;
    }

    /// <summary>Runs the schedule on weekdays only.</summary>
    public ScheduleDefinitionBuilder Weekdays()
    {
        _dayFlags = DayFlags.Weekdays;
        _monthFlags = MonthFlags.EveryMonth;
        return this;
    }

    /// <summary>Sets the days of the week the schedule runs.</summary>
    public ScheduleDefinitionBuilder SetDays(DayFlags days)
    {
        _dayFlags = days;
        return this;
    }

    /// <summary>Sets the months the schedule runs.</summary>
    public ScheduleDefinitionBuilder SetMonths(MonthFlags months)
    {
        _monthFlags = months;
        return this;
    }

    /// <summary>Sets clock times to fire (SetTimes schedule) from time strings.</summary>
    public ScheduleDefinitionBuilder SetTimes(params string[] times)
    {
        _type = ScheduleType.SetTimes;
        _times = times.Select(s => TimeOnly.Parse(s)).ToList();
        return this;
    }

    /// <summary>Sets clock times to fire (SetTimes schedule).</summary>
    public ScheduleDefinitionBuilder SetTimes(params TimeOnly[] times)
    {
        _type = ScheduleType.SetTimes;
        _times = times.ToList();
        return this;
    }

    /// <summary>Sets an interval schedule inside a time window, parsing the bounds from strings.</summary>
    public ScheduleDefinitionBuilder SetInterval(string startTime, string endTime, int intervalMinutes)
    {
        _type = ScheduleType.Interval;
        _startTime = TimeOnly.Parse(startTime);
        _endTime = TimeOnly.Parse(endTime);
        _intervalMinutes = intervalMinutes;
        return this;
    }

    /// <summary>Sets an interval schedule inside a time window.</summary>
    public ScheduleDefinitionBuilder SetInterval(TimeOnly startTime, TimeOnly endTime, int intervalMinutes)
    {
        _type = ScheduleType.Interval;
        _startTime = startTime;
        _endTime = endTime;
        _intervalMinutes = intervalMinutes;
        return this;
    }

    /// <summary>Sets a one-shot schedule that fires at a specific date/time.</summary>
    public ScheduleDefinitionBuilder SetExecuteAt(DateTime executeAt)
    {
        _type = ScheduleType.OneShot;
        _executeAt = executeAt.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(executeAt, DateTimeKind.Utc) : executeAt.ToUniversalTime();
        return this;
    }

    /// <summary>
    /// Sets a cron-expression schedule. Accepts standard 5-field (minute) and 6-field (second) cron forms. Examples: <c>"0 8 * * MON-FRI"</c> (weekdays at
    /// 08:00), <c>"*/15 9-17 * * MON-FRI"</c> (every 15 min during business hours).
    /// </summary>
    public ScheduleDefinitionBuilder SetCron(string cronExpression)
    {
        _type = ScheduleType.Cron;
        _cronExpression = cronExpression;
        return this;
    }

    /// <summary>Builds the finished <see cref="ScheduleDefinition" />.</summary>
    public ScheduleDefinition Build()
        => new(_type, _dayFlags, _monthFlags, _times, _startTime, _endTime, _intervalMinutes, _executeAt, _timeZone, _enabled, _description, _cronExpression);

    public override string ToString() => $"ScheduleDefinitionBuilder: type={_type}, enabled={_enabled}, cron={_cronExpression ?? "(none)"}";
}