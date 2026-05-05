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

    public bool Enabled { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public virtual JobDefinition JobDefinition { get; set; } = null!;

    public virtual ICollection<JobRun> JobRuns { get; set; } = new List<JobRun>();

    public virtual ICollection<JobScheduleParameter> JobScheduleParameters { get; set; } = new List<JobScheduleParameter>();
}