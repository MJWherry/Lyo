using System.ComponentModel.DataAnnotations;
using Lyo.Job.Models.Enums;

namespace Lyo.Job.Postgres.Database;

/// <summary>
/// One recurrence rule. It fires exactly one owner: a job definition or a workflow, never both and never neither. A database CHECK enforces that, and
/// <see cref="JobScheduleWriteValidator" /> reports it as a validation error before the write reaches Postgres.
/// </summary>
public class JobSchedule
{
    public Guid Id { get; set; }

    /// <summary>Definition this schedule fires. Null when the schedule is workflow-owned.</summary>
    public Guid? JobDefinitionId { get; set; }

    /// <summary>Workflow this schedule starts. Null when the schedule is definition-owned.</summary>
    public Guid? JobWorkflowId { get; set; }

    [MaxLength(3000)]
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

    /// <summary>Standard cron expression (5- or 6-field). Set only when Type is Cron.</summary>
    [MaxLength(120)]
    public string? CronExpression { get; set; }

    /// <summary>How slots missed while no scheduler was running are treated: Skip or RunOnce. Stored as string.</summary>
    [Required]
    [MaxLength(12)]
    public string MisfirePolicy { get; set; } = nameof(JobMisfirePolicy.Skip);

    /// <summary>UTC date before which this schedule never fires. Null means no lower bound.</summary>
    public DateTime? StartDateUtc { get; set; }

    /// <summary>UTC date after which this schedule never fires. Null means no upper bound.</summary>
    public DateTime? EndDateUtc { get; set; }

    /// <summary>IANA/Windows time zone id used when this schedule's times are evaluated. Null uses the scheduler-level time zone (or UTC).</summary>
    [MaxLength(64)]
    public string? TimeZoneId { get; set; }

    /// <summary>Optional blackout calendar whose do-not-run windows apply on this schedule.</summary>
    public Guid? JobBlackoutCalendarId { get; set; }

    public bool Enabled { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public virtual JobDefinition? JobDefinition { get; set; }

    public virtual JobWorkflow? JobWorkflow { get; set; }

    public virtual JobBlackoutCalendar? JobBlackoutCalendar { get; set; }

    public virtual ICollection<JobRun> JobRuns { get; set; } = new List<JobRun>();

    public virtual ICollection<JobScheduleParameter> JobScheduleParameters { get; set; } = new List<JobScheduleParameter>();
}