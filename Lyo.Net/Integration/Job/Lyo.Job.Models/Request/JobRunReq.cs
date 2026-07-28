using System.Diagnostics;
using Lyo.Common.Extensions;
using Lyo.Job.Models.Enums;

namespace Lyo.Job.Models.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class JobRunReq
{
    public Guid JobDefinitionId { get; set; }

    public Guid? JobScheduleId { get; set; }

    public Guid? JobTriggerId { get; set; }

    public Guid? TriggeredByJobRunId { get; set; }

    public Guid? ReRanFromJobRunId { get; set; }

    public string CreatedBy { get; set; } = null!;

    public bool AllowTriggers { get; set; }

    public JobRunResult? Result { get; set; }

    /// <summary>
    /// The scheduled slot that triggered this run. When set, a unique constraint on (JobScheduleId, ScheduledSlotUtc) prevents duplicate runs across multiple scheduler
    /// instances.
    /// </summary>
    public DateTime? ScheduledSlotUtc { get; set; }

    /// <summary>Number of retry attempts (0 = first attempt).</summary>
    public int RetryAttempt { get; set; }

    /// <summary>Message priority (0-9) for dispatch. Null = inherit from the definition.</summary>
    public int? Priority { get; set; }

    /// <summary>Caller-supplied key for idempotent run creation within a definition.</summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// When true, the run is persisted as <c>Queued</c> but the immediate worker-queue publish is skipped. The caller becomes responsible for dispatch (e.g. the scheduler's
    /// delayed-MQ retry envelope, or the workflow engine publishing after linking the run to its step). The maintenance service's stuck-queued-run recovery acts as the safety net.
    /// Dispatch is also suppressed automatically when <see cref="ScheduledSlotUtc" /> is in the future (delayed retries).
    /// </summary>
    public bool SuppressDispatch { get; set; }

    /// <summary>When true, the worker executes validation only and does not commit side effects.</summary>
    public bool DryRun { get; set; }

    /// <summary>Distributed trace id propagated through the run lifecycle.</summary>
    public string? TraceId { get; set; }

    /// <summary>Parent run when this run is part of a batch or fan-out.</summary>
    public Guid? ParentJobRunId { get; set; }

    /// <summary>Zero-based index within a parent batch. Null when not part of a batch.</summary>
    public int? BatchIndex { get; set; }

    /// <summary>Total items in a parent batch. Null when not part of a batch.</summary>
    public int? BatchTotal { get; set; }

    public List<JobRunParameterReq> JobRunParameters { get; init; } = [];

    //no need for update or delete, jobs shouldn't be modified after the job run is created from definition
    public JobRunReq() { }

    public JobRunReq(Guid definitionId, string createdBy, bool allowTriggers, Guid? triggerId = null, Guid? scheduleId = null)
    {
        JobDefinitionId = definitionId;
        CreatedBy = createdBy;
        AllowTriggers = allowTriggers;
        JobTriggerId = triggerId;
        JobScheduleId = scheduleId;
    }

    public override string ToString()
        => $"Definition={JobDefinitionId.Truncated()}{(JobScheduleId.HasValue ? $" Schedule={JobScheduleId.Truncated()}" : "")}{(JobTriggerId.HasValue ? $" Triggers={JobTriggerId.Truncated()}" : "")} Created By={CreatedBy}, Triggering={AllowTriggers} Parameters: {JobRunParameters.Count}";
}