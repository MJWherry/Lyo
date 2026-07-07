using System.Diagnostics;
using Lyo.Job.Models.Enums;

namespace Lyo.Job.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record JobWorkflowRunRes(
    Guid Id,
    Guid JobWorkflowId,
    JobWorkflowRunState State,
    DateTime? StartedTimestamp,
    DateTime? FinishedTimestamp,
    DateTime CreatedTimestamp,
    IReadOnlyList<JobWorkflowRunStepRes>? RunSteps,
    JobWorkflowRes? JobWorkflow = null)
{
    public override string ToString() => $"Workflow={JobWorkflowId} State={State}";
}
