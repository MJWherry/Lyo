using System.ComponentModel.DataAnnotations;
using Lyo.Job.Models.Enums;

namespace Lyo.Job.Postgres.Database;

/// <summary>One do-not-run (or defer) window inside a <see cref="JobBlackoutCalendar" />.</summary>
public class JobBlackoutWindow
{
    public Guid Id { get; set; }

    public Guid JobBlackoutCalendarId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = null!;

    [Required]
    [MaxLength(51)]
    public string DayFlags { get; set; } = null!;

    /// <summary>UTC calendar date when this window starts applying. When set without <see cref="DaysOfMonth" />, this is a dated-range window.</summary>
    public DateTime? StartDateUtc { get; set; }

    /// <summary>UTC calendar date after which this window no longer applies.</summary>
    public DateTime? EndDateUtc { get; set; }

    [Required]
    [MaxLength(8)]
    public string StartTime { get; set; } = null!;

    [Required]
    [MaxLength(8)]
    public string EndTime { get; set; } = null!;

    /// <summary>How runs that land inside this window are treated. Stored as string.</summary>
    [Required]
    [MaxLength(10)]
    public string Policy { get; set; } = nameof(JobBlackoutPolicy.Skip);

    public bool Enabled { get; set; }

    /// <summary><c>HolidayInfo.Slug</c> when this window is a calculated holiday. Null for other kinds.</summary>
    [MaxLength(64)]
    public string? HolidaySlug { get; set; }

    /// <summary>When <see langword="true" />, a holiday window also matches the observed weekday if the holiday falls on a weekend.</summary>
    public bool IncludeObservedDate { get; set; }

    /// <summary>Months a calendar-day window applies to. Stored as the enum name. Null unless calendar-day kind.</summary>
    [MaxLength(108)]
    public string? MonthFlags { get; set; }

    /// <summary>Days of the month (1–31) a calendar-day window applies to. Null unless calendar-day kind.</summary>
    public List<int>? DaysOfMonth { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public virtual JobBlackoutCalendar JobBlackoutCalendar { get; set; } = null!;
}
