using System.Diagnostics;
using Lyo.Job.Models.Enums;

namespace Lyo.Job.Models.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class JobWorkflowRunStepReq
{
    public Guid JobWorkflowRunId { get; set; }

    public Guid JobWorkflowStepId { get; set; }

    public Guid? JobRunId { get; set; }

    public JobWorkflowStepState State { get; set; } = JobWorkflowStepState.Pending;

    public override string ToString() => $"Step={JobWorkflowStepId} State={State}";
}
