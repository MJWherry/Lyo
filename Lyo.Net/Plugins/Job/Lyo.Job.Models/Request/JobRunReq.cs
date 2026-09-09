using System.Diagnostics;
using Lyo.Common.Core.Extensions;
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
    /// Scheduled slot that triggered this run. When set with <see cref="JobScheduleId" />, a unique constraint on (JobScheduleId, ScheduledSlotUtc) makes create idempotent
    /// across scheduler instances: a duplicate returns the existing run instead of inserting a second row.
    /// </summary>
    public DateTime? ScheduledSlotUtc { get; set; }

    /// <summary>Retry attempt count (0 = first attempt).</summary>
    public int RetryAttempt { get; set; }

    /// <summary>Message priority (0-9) for dispatch. Null inherits from the definition.</summary>
    public int? Priority { get; set; }

    /// <summary>Caller-supplied key for idempotent run creation on a definition.</summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// When true, the run is persisted as <c>Queued</c> but the immediate worker-queue publish is skipped. The caller becomes responsible for dispatch (for example the scheduler's
    /// delayed-MQ retry envelope, or the workflow engine publishing after linking the run to its step). The maintenance service's stuck-queued-run recovery is the safety net.
    /// Dispatch is also suppressed automatically when <see cref="ScheduledSlotUtc" /> is in the future (delayed retries).
    /// </summary>
    public bool SuppressDispatch { get; set; }

    /// <summary>When true, the worker runs validation only and does not commit side effects.</summary>
    public bool DryRun { get; set; }

    /// <summary>Distributed trace id carried through the run lifecycle.</summary>
    public string? TraceId { get; set; }

    /// <summary>Parent run when this run belongs to a batch or fan-out.</summary>
    public Guid? ParentJobRunId { get; set; }

    /// <summary>Zero-based index inside a parent batch. Null when not part of a batch.</summary>
    public int? BatchIndex { get; set; }

    /// <summary>Total items in a parent batch. Null when this run is not part of a batch.</summary>
    public int? BatchTotal { get; set; }

    public List<JobRunParameterReq> JobRunParameters { get; init; } = [];

    // No update or delete: a run should not be changed after it is created from the definition
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