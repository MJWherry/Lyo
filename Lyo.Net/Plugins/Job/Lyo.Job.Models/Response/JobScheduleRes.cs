using System.Diagnostics;
using Lyo.Common.Core.Enums;
using Lyo.Common.Core.Extensions;
using Lyo.Job.Models.Enums;
using Lyo.Schedule.Models;
#if NET6_0_OR_GREATER
using TimeOnly = System.TimeOnly;
#else
using TimeOnly = Lyo.DateAndTime.TimeOnlyModel;
#endif

namespace Lyo.Job.Models.Response;

/// <summary>
/// One recurrence rule. Exactly one of <see cref="JobDefinitionId" /> and <see cref="JobWorkflowId" /> is set: the first is walked by the job scheduler, the second by the
/// workflow scheduler.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record JobScheduleRes(
    Guid Id,
    Guid? JobDefinitionId,
    MonthFlags MonthFlags,
    DayFlags DayFlags,
    ScheduleType Type,
    IReadOnlyList<TimeOnly>? Times,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    int? IntervalMinutes,
    string? Description,
    bool Enabled,
    IReadOnlyList<JobScheduleParameterRes>? Parameters,
    string? CronExpression = null,
    JobMisfirePolicy MisfirePolicy = JobMisfirePolicy.Skip,
    DateTime? StartDateUtc = null,
    DateTime? EndDateUtc = null,
    string? TimeZoneId = null,
    Guid? JobBlackoutCalendarId = null,
    JobBlackoutCalendarRes? JobBlackoutCalendar = null,
    Guid? JobWorkflowId = null)
{
    public override string ToString()
        => $"{Id.Truncated()} {Description ?? (Type == ScheduleType.SetTimes && Times?.Count > 0 ? $"Times: {string.Join(",", Times)}" : $"{StartTime} - {EndTime}, {IntervalMinutes}m Intervals")} Parameters={Parameters?.Count}";
}