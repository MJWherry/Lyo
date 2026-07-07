using System.ComponentModel.DataAnnotations;

namespace Lyo.Job.Postgres.Database;

public class JobSchedule
{
    public Guid Id { get; set; }

    public Guid JobDefinitionId { get; set; }

    [MaxLength(100)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(8)]
    public string Type { get; set; } = null!;

    [Required]
    [MaxLength(108)]
    public string MonthFlags { get; set; } = null!;

    [Required]
    [MaxLength(51)]
    public string DayFlags { get; set; } = null!;

    public List<string>? Times { get; set; }

    [MaxLength(8)]
    public string? StartTime { get; set; }

    [MaxLength(8)]
    public string? EndTime { get; set; }

    public int? IntervalMinutes { get; set; }

    /// <summary>Standard cron expression (5- or 6-field). Only set when Type is Cron.</summary>
    [MaxLength(120)]
    public string? CronExpression { get; set; }

    /// <summary>How slots missed while no scheduler was running are handled: Skip or RunOnce. Stored as string.</summary>
    [Required]
    [MaxLength(12)]
    public string MisfirePolicy { get; set; } = nameof(Models.Enums.JobMisfirePolicy.Skip);

    /// <summary>UTC date before which this schedule never fires. Null = no lower bound.</summary>
    public DateTime? StartDateUtc { get; set; }

    /// <summary>UTC date after which this schedule never fires. Null = no upper bound.</summary>
    public DateTime? EndDateUtc { get; set; }

    /// <summary>IANA/Windows time zone id used when evaluating this schedule's times. Null = use the scheduler-level time zone (or UTC).</summary>
    [MaxLength(64)]
    public string? TimeZoneId { get; set; }

    /// <summary>Optional calendar whose blackout windows apply to this schedule.</summary>
    public Guid? JobCalendarId { get; set; }

    public bool Enabled { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public virtual JobDefinition JobDefinition { get; set; } = null!;

    public virtual JobCalendar? JobCalendar { get; set; }

    public virtual ICollection<JobRun> JobRuns { get; set; } = new List<JobRun>();

    public virtual ICollection<JobScheduleParameter> JobScheduleParameters { get; set; } = new List<JobScheduleParameter>();
}