using System.Diagnostics;
using Lyo.Common.Core.Enums;
using Lyo.Job.Models.Enums;
#if NET6_0_OR_GREATER
using TimeOnly = System.TimeOnly;
#else
using TimeOnly = Lyo.DateAndTime.TimeOnlyModel;
#endif

namespace Lyo.Job.Models.Request;

/// <summary>One blackout window inside a <see cref="JobBlackoutCalendarReq" />.</summary>
/// <remarks>
/// Four mutually exclusive kinds, inferred from which fields are set:
/// weekdays (<see cref="DayFlags" />), calendar days (<see cref="DaysOfMonth" /> plus <see cref="MonthFlags" />), a dated range
/// (<see cref="StartDateUtc" />), or a calculated holiday (<see cref="HolidaySlug" />).
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class JobBlackoutWindowReq
{
    public Guid JobBlackoutCalendarId { get; set; }

    public string Name { get; set; } = null!;

    public DayFlags DayFlags { get; set; }

    /// <summary>UTC calendar date when this window starts applying. Dated-range kind, or an optional year bound on calendar-day windows.</summary>
    public DateTime? StartDateUtc { get; set; }

    /// <summary>UTC calendar date when this window stops applying. Null means the same day as <see cref="StartDateUtc" />.</summary>
    public DateTime? EndDateUtc { get; set; }

#if NET6_0_OR_GREATER
    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }
#else
    public TimeOnly StartTime { get; set; } = null!;

    public TimeOnly EndTime { get; set; } = null!;
#endif

    public JobBlackoutPolicy Policy { get; set; } = JobBlackoutPolicy.Skip;

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// <see cref="Lyo.DateAndTime.HolidayInfo.Slug" /> for a calculated holiday window (for example <c>christmas-day</c>). When set, the scheduler matches
    /// <c>HolidayInfo.OccursOn</c> instead of weekdays or a dated range.
    /// </summary>
    public string? HolidaySlug { get; set; }

    /// <summary>When <see langword="true" />, a holiday window also matches the observed weekday if the holiday falls on a weekend. Default is the calendar date only.</summary>
    public bool IncludeObservedDate { get; set; }

    /// <summary>Months a calendar-day window applies to. Required when <see cref="DaysOfMonth" /> is set.</summary>
    public MonthFlags? MonthFlags { get; set; }

    /// <summary>Days of the month (1–31) a calendar-day window applies to. Invalid dates such as 31 February never match.</summary>
    public List<int>? DaysOfMonth { get; set; }

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
