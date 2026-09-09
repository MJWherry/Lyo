using System.Diagnostics;

namespace Lyo.Job.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record JobWorkflowRes(Guid Id, string Name, string? Description, bool Enabled, IReadOnlyList<JobWorkflowStepRes>? Steps)
{
    public override string ToString() => $"{Name}, {Description} (Enabled={Enabled})";
}