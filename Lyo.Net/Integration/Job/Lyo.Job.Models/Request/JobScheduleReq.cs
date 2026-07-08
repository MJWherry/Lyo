using System.Diagnostics;
using Lyo.Common.Enums;
using Lyo.Schedule.Models;
#if NET6_0_OR_GREATER
using TimeOnly = System.TimeOnly;

#else
using TimeOnly = Lyo.DateAndTime.TimeOnlyModel;
#endif

namespace Lyo.Job.Models.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class JobScheduleReq
{
    public Guid JobDefinitionId { get; set; }

    public MonthFlags MonthFlags { get; set; }

    public DayFlags DayFlags { get; set; }

    public ScheduleType Type { get; set; }

    public List<TimeOnly>? Times { get; set; }

    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }

    public int? IntervalMinutes { get; set; }

    /// <summary>Standard cron expression (5- or 6-field). Only required when <see cref="Type" /> is <c>Cron</c>.</summary>
    public string? CronExpression { get; set; }

    /// <summary>How slots missed while no scheduler was running are handled.</summary>
    public Enums.JobMisfirePolicy MisfirePolicy { get; set; } = Enums.JobMisfirePolicy.Skip;

    /// <summary>UTC date before which this schedule never fires. Null = no lower bound.</summary>
    public DateTime? StartDateUtc { get; set; }

    /// <summary>UTC date after which this schedule never fires. Null = no upper bound.</summary>
    public DateTime? EndDateUtc { get; set; }

    /// <summary>IANA/Windows time zone id used when evaluating this schedule's times. Null = use the scheduler-level time zone (or UTC).</summary>
    public string? TimeZoneId { get; set; }

    /// <summary>Optional blackout calendar whose do-not-run windows apply to this schedule.</summary>
    public Guid? JobBlackoutCalendarId { get; set; }

    /// <summary>Inline blackout calendar to create and link when this schedule is persisted. Mutually exclusive with <see cref="JobBlackoutCalendarId" />.</summary>
    public JobBlackoutCalendarReq? CreateBlackoutCalendar { get; set; }

    public string? Description { get; set; }

    public bool Enabled { get; set; } = true;

    public List<JobScheduleParameterReq> CreateScheduleParameters { get; set; } = [];

    public override string ToString()
        => Description ?? $"Days: {DayFlags} - {(Type == ScheduleType.SetTimes && Times?.Count > 0
            ? $"Times: {string.Join(",", Times)}"
            : $"{StartTime} - {EndTime}, {IntervalMinutes}m Intervals")}, {(Enabled ? "Enabled" : "Disabled")}";
}