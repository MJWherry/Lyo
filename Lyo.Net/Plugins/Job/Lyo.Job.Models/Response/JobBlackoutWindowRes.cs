using System.Diagnostics;
using Lyo.Common.Core.Enums;
using Lyo.Job.Models.Enums;
#if NET6_0_OR_GREATER
using TimeOnly = System.TimeOnly;
#else
using TimeOnly = Lyo.DateAndTime.TimeOnlyModel;
#endif

namespace Lyo.Job.Models.Response;

/// <summary>One blackout window inside a <see cref="JobBlackoutCalendarRes" />.</summary>
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
    DateTime? EndDateUtc = null,
    string? HolidaySlug = null,
    bool IncludeObservedDate = false,
    MonthFlags? MonthFlags = null,
    IReadOnlyList<int>? DaysOfMonth = null)
{
    public override string ToString()
    {
        if (!string.IsNullOrWhiteSpace(HolidaySlug))
            return $"{Name} holiday:{HolidaySlug}{(IncludeObservedDate ? "+observed" : "")} {StartTime}-{EndTime} ({Policy})";

        if (DaysOfMonth is { Count: > 0 })
            return $"{Name} {MonthFlags} days {string.Join(",", DaysOfMonth)} {StartTime}-{EndTime} ({Policy})";

        return StartDateUtc.HasValue
            ? $"{Name} {StartDateUtc:yyyy-MM-dd}{(EndDateUtc.HasValue && EndDateUtc != StartDateUtc ? $"..{EndDateUtc:yyyy-MM-dd}" : "")} {StartTime}-{EndTime} ({Policy})"
            : $"{Name} {DayFlags} {StartTime}-{EndTime} ({Policy})";
    }
}
