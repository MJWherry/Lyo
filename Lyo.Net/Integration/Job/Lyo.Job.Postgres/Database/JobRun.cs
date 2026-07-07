using System.ComponentModel.DataAnnotations;
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

    [Required]
    [MaxLength(50)]
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

    /// <summary>Message priority (0-9) used when dispatching this run. Copied from the definition at creation unless explicitly overridden.</summary>
    public int Priority { get; set; }

    /// <summary>Completion percentage (0-100) reported by the worker. Null until the worker reports progress.</summary>
    public int? ProgressPercent { get; set; }

    /// <summary>Short human-readable progress message reported by the worker.</summary>
    [MaxLength(500)]
    public string? ProgressMessage { get; set; }

    /// <summary>Caller-supplied key for idempotent run creation within a definition.</summary>
    [MaxLength(128)]
    public string? IdempotencyKey { get; set; }

    /// <summary>When true, the worker executes validation only and does not commit side effects.</summary>
    public bool DryRun { get; set; }

    /// <summary>Whether an SLA breach was detected for this run.</summary>
    public bool SlaBreached { get; set; }

    /// <summary>Distributed trace id propagated through the run lifecycle.</summary>
    [MaxLength(64)]
    public string? TraceId { get; set; }

    /// <summary>Parent run when this run is part of a batch or fan-out.</summary>
    public Guid? ParentJobRunId { get; set; }

    /// <summary>Zero-based index within a parent batch. Null when not part of a batch.</summary>
    public int? BatchIndex { get; set; }

    /// <summary>Total items in a parent batch. Null when not part of a batch.</summary>
    public int? BatchTotal { get; set; }

    /// <summary>Snapshot of <see cref="JobDefinition.DefinitionVersion" /> at run creation for audit correlation.</summary>
    public int? DefinitionAuditVersion { get; set; }

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

    public virtual JobRun? ParentJobRun { get; set; }

    public virtual ICollection<JobRun> InverseParentJobRun { get; set; } = new List<JobRun>();

    public virtual ICollection<JobWorkflowRunStep> JobWorkflowRunSteps { get; set; } = new List<JobWorkflowRunStep>();
}