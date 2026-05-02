using Lyo.Job.Models.Enums;

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

    public JobState State { get; set; }

    public bool AllowTriggers { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? StartedTimestamp { get; set; }

    public DateTime? FinishedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public Models.Enums.JobRunResult? Result { get; set; }

    /// <summary>
    /// The scheduled slot that caused this run to be created. Combined with <see cref="JobScheduleId" />, this forms a unique constraint that prevents duplicate runs when
    /// multiple scheduler instances fire concurrently.
    /// </summary>
    public DateTime? ScheduledSlotUtc { get; set; }

    /// <summary>Number of retry attempts (0 = first attempt).</summary>
    public int RetryAttempt { get; set; }

    /// <summary>
    /// UTC timestamp of the last heartbeat received from the worker. Updated every ~30 s by the worker SDK. When this falls more than <c>JobDefinition.TimeoutMinutes</c> behind
    /// <c>UtcNow</c>, the <c>JobMaintenanceService</c> marks the run as failed (dead worker detection).
    /// </summary>
    public DateTime? LastHeartbeatUtc { get; set; }

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