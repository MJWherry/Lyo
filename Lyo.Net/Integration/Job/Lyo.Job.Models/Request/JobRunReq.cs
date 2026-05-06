using System.Diagnostics;
using Lyo.Common;
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