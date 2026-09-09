using System.Diagnostics;

namespace Lyo.Job.Models.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class JobWorkflowReq
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool Enabled { get; set; } = true;

    public List<JobWorkflowStepReq> CreateSteps { get; set; } = [];

    public override string ToString() => $"{Name}, {Description} (Enabled={Enabled}) Steps(C={CreateSteps.Count})";
}