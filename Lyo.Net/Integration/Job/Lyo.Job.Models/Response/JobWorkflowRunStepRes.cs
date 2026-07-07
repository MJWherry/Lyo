using System.Diagnostics;
using Lyo.Job.Models.Enums;

namespace Lyo.Job.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record JobWorkflowRunStepRes(
    Guid Id,
    Guid JobWorkflowRunId,
    Guid JobWorkflowStepId,
    Guid? JobRunId,
    JobWorkflowStepState State,
    JobWorkflowStepRes? JobWorkflowStep = null,
    JobRunRes? JobRun = null)
{
    public override string ToString() => $"Step={JobWorkflowStepId} State={State}";
}
