using System.Diagnostics;
using Lyo.Common;
using Lyo.Common.Enums;
using Lyo.Schedule.Models;
#if NET6_0_OR_GREATER
using TimeOnly = System.TimeOnly;

#else
using TimeOnly = Lyo.DateAndTime.TimeOnlyModel;
#endif

namespace Lyo.Job.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record JobScheduleRes(
    Guid Id,
    Guid JobDefinitionId,
    MonthFlags MonthFlags,
    DayFlags DayFlags,
    ScheduleType Type,
    IReadOnlyList<TimeOnly>? Times,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    int? IntervalMinutes,
    string? Description,
    bool Enabled,
    IReadOnlyList<JobScheduleParameterRes>? Parameters)
{
    public override string ToString()
        => $"{Id.Truncated()} {Description ?? (Type == ScheduleType.SetTimes && Times?.Count > 0 ? $"Times: {string.Join(",", Times)}" : $"{StartTime} - {EndTime}, {IntervalMinutes}m Intervals")} Parameters={Parameters?.Count}";
}