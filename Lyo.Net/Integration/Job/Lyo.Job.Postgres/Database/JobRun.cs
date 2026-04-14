namespace Lyo.Job.Postgres.Database;

public class JobRun
{
    public Guid Id { get; set; }

    public Guid JobDefinitionId { get; set; }

    public Guid? JobScheduleId { get; set; }

    public Guid? JobTriggerId { get; set; }

    public Guid? TriggeredByJobRunId { get; set; }

    public Guid? ReRanFromJobRunId { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string State { get; set; } = null!;

    public bool AllowTriggers { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? StartedTimestamp { get; set; }

    public DateTime? FinishedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public string? Result { get; set; }

    public virtual ICollection<JobRun> InverseReRanFromJobRun { get; set; } = new List<JobRun>();

    public virtual ICollection<JobRun> InverseTriggeredByJobRun { get; set; } = new List<JobRun>();

    public virtual JobDefinition JobDefinition { get; set; } = null!;

    public virtual ICollection<JobRunLog> JobRunLogs { get; set; } = new List<JobRunLog>();

    public virtual ICollection<JobRunParameter> JobRunParameters { get; set; } = new List<JobRunParameter>();

    public virtual ICollection<JobRunResult> JobRunResults { get; set; } = new List<JobRunResult>();

    public virtual JobSchedule? JobSchedule { get; set; }

    public virtual JobTrigger? JobTrigger { get; set; }

    public virtual JobRun? ReRanFromJobRun { get; set; }

    public virtual JobRun? TriggeredByJobRun { get; set; }
}