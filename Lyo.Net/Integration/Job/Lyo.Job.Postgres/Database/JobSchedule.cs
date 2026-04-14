namespace Lyo.Job.Postgres.Database;

public class JobSchedule
{
    public Guid Id { get; set; }

    public Guid JobDefinitionId { get; set; }

    public string? Description { get; set; }

    public string Type { get; set; } = null!;

    public string MonthFlags { get; set; } = null!;

    public string DayFlags { get; set; } = null!;

    public List<string>? Times { get; set; }

    public string? StartTime { get; set; }

    public string? EndTime { get; set; }

    public int? IntervalMinutes { get; set; }

    public bool Enabled { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public virtual JobDefinition JobDefinition { get; set; } = null!;

    public virtual ICollection<JobRun> JobRuns { get; set; } = new List<JobRun>();

    public virtual ICollection<JobScheduleParameter> JobScheduleParameters { get; set; } = new List<JobScheduleParameter>();
}