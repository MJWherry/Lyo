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

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public JobBlackoutPolicy Policy { get; set; } = JobBlackoutPolicy.Skip;

    public bool Enabled { get; set; } = true;

    public override string ToString() => $"{Name} {DayFlags} {StartTime}-{EndTime} ({Policy})";
}
