using System.Diagnostics;
using Lyo.Job.Models.Enums;

namespace Lyo.Job.Models.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class JobWorkflowRunReq
{
    public Guid JobWorkflowId { get; set; }

    public JobWorkflowRunState State { get; set; } = JobWorkflowRunState.Pending;

    public DateTime? StartedTimestamp { get; set; }

    public DateTime? FinishedTimestamp { get; set; }

    public List<JobWorkflowRunStepReq> CreateRunSteps { get; set; } = [];

    public override string ToString() => $"Workflow={JobWorkflowId} State={State}";
}
