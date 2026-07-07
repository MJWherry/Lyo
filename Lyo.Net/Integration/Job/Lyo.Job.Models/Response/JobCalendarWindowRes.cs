using System.Diagnostics;
using Lyo.Common.Enums;
using Lyo.Job.Models.Enums;
#if NET6_0_OR_GREATER
using TimeOnly = System.TimeOnly;

#else
using TimeOnly = Lyo.DateAndTime.TimeOnlyModel;
#endif

namespace Lyo.Job.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record JobCalendarWindowRes(
    Guid Id,
    Guid JobCalendarId,
    string Name,
    DayFlags DayFlags,
    TimeOnly StartTime,
    TimeOnly EndTime,
    JobBlackoutPolicy Policy,
    bool Enabled)
{
    public override string ToString() => $"{Name} {DayFlags} {StartTime}-{EndTime} ({Policy})";
}
