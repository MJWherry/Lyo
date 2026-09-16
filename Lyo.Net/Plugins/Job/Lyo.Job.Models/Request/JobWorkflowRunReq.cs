using System.Diagnostics;
using Lyo.Job.Models.Enums;

namespace Lyo.Job.Models.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class JobWorkflowRunReq
{
    public Guid JobWorkflowId { get; set; }

    public JobWorkflowRunState State { get; set; } = JobWorkflowRunState.Pending;

    /// <summary>De-duplication key, unique per workflow. Set by <c>JobWorkflowService.StartWorkflowAsync</c> from the caller's start request.</summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>Schedule that started this run, when it came from the workflow scheduler.</summary>
    public Guid? JobScheduleId { get; set; }

    /// <summary>The cron instant this run covers. Unique per schedule.</summary>
    public DateTime? ScheduledSlotUtc { get; set; }

    /// <summary>Serialized flat string map of the values this run branches on, resolved by <c>JobWorkflowService.StartWorkflowAsync</c>.</summary>
    public string? InputsJson { get; set; }

    public DateTime? StartedTimestamp { get; set; }

    public DateTime? FinishedTimestamp { get; set; }

    public List<JobWorkflowRunStepReq> CreateRunSteps { get; set; } = [];

    public override string ToString() => $"Workflow={JobWorkflowId} State={State}";
}