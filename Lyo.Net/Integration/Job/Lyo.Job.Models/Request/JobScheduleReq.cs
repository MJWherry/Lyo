#if NET6_0_OR_GREATER
using TimeOnly = System.TimeOnly;
#else
using TimeOnly = Lyo.DateAndTime.TimeOnlyModel;
#endif
using System.Diagnostics;
using Lyo.Common.Enums;
using Lyo.Schedule.Models;

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

    public string? Description { get; set; }

    public bool Enabled { get; set; } = true;

    public List<JobScheduleParameterReq> CreateScheduleParameters { get; set; } = [];

    public override string ToString()
        => Description ?? $"Days: {DayFlags} - {(Type == ScheduleType.SetTimes && Times?.Count > 0
            ? $"Times: {string.Join(",", Times)}"
            : $"{StartTime} - {EndTime}, {IntervalMinutes}m Intervals")}, {(Enabled ? "Enabled" : "Disabled")}";
}