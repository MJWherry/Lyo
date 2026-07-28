using System.ComponentModel.DataAnnotations;
using Lyo.Job.Models.Enums;

namespace Lyo.Job.Postgres.Database;

/// <summary>A single do-not-run (or defer) window within a <see cref="JobBlackoutCalendar" />.</summary>
public class JobBlackoutWindow
{
    public Guid Id { get; set; }

    public Guid JobBlackoutCalendarId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    [Required]
    [MaxLength(51)]
    public string DayFlags { get; set; } = null!;

    /// <summary>UTC calendar date when this window starts applying. When set, overrides <see cref="DayFlags" />.</summary>
    public DateTime? StartDateUtc { get; set; }

    /// <summary>UTC calendar date when this window stops applying.</summary>
    public DateTime? EndDateUtc { get; set; }

    [Required]
    [MaxLength(8)]
    public string StartTime { get; set; } = null!;

    [Required]
    [MaxLength(8)]
    public string EndTime { get; set; } = null!;

    /// <summary>How runs that fall inside this window are handled. Stored as string.</summary>
    [Required]
    [MaxLength(10)]
    public string Policy { get; set; } = nameof(JobBlackoutPolicy.Skip);

    public bool Enabled { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public virtual JobBlackoutCalendar JobBlackoutCalendar { get; set; } = null!;
}