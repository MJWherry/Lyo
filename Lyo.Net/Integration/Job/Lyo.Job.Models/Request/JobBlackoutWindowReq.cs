using System.Diagnostics;
using Lyo.Common.Enums;
using Lyo.Job.Models.Enums;
#if NET6_0_OR_GREATER
using TimeOnly = System.TimeOnly;
#else
using TimeOnly = Lyo.DateAndTime.TimeOnlyModel;
#endif

namespace Lyo.Job.Models.Request;

/// <summary>A single blackout window within a <see cref="JobBlackoutCalendarReq" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class JobBlackoutWindowReq
{
    public Guid JobBlackoutCalendarId { get; set; }

    public string Name { get; set; } = null!;

    public DayFlags DayFlags { get; set; }

    /// <summary>UTC calendar date when this window starts applying. When set with <see cref="EndDateUtc" />, overrides <see cref="DayFlags" />.</summary>
    public DateTime? StartDateUtc { get; set; }

    /// <summary>UTC calendar date when this window stops applying. Null = same day as <see cref="StartDateUtc" />.</summary>
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

    public override string ToString()
        => StartDateUtc.HasValue
            ? $"{Name} {StartDateUtc:yyyy-MM-dd}{(EndDateUtc.HasValue && EndDateUtc != StartDateUtc ? $"..{EndDateUtc:yyyy-MM-dd}" : "")} {StartTime}-{EndTime} ({Policy})"
            : $"{Name} {DayFlags} {StartTime}-{EndTime} ({Policy})";
}