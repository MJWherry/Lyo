using System.Diagnostics;
using Lyo.Common.Enums;
using Lyo.Job.Models.Enums;
#if NET6_0_OR_GREATER
using TimeOnly = System.TimeOnly;

#else
using TimeOnly = Lyo.DateAndTime.TimeOnlyModel;
#endif

namespace Lyo.Job.Models.Response;

/// <summary>A single blackout window within a <see cref="JobBlackoutCalendarRes" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed record JobBlackoutWindowRes(
    Guid Id,
    Guid JobBlackoutCalendarId,
    string Name,
    DayFlags DayFlags,
    TimeOnly StartTime,
    TimeOnly EndTime,
    JobBlackoutPolicy Policy,
    bool Enabled,
    DateTime? StartDateUtc = null,
    DateTime? EndDateUtc = null)
{
    public override string ToString()
        => StartDateUtc.HasValue
            ? $"{Name} {StartDateUtc:yyyy-MM-dd}{(EndDateUtc.HasValue && EndDateUtc != StartDateUtc ? $"..{EndDateUtc:yyyy-MM-dd}" : "")} {StartTime}-{EndTime} ({Policy})"
            : $"{Name} {DayFlags} {StartTime}-{EndTime} ({Policy})";
}
