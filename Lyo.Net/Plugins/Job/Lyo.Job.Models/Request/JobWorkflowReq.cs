using System.Diagnostics;

namespace Lyo.Job.Models.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class JobWorkflowReq
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool Enabled { get; set; } = true;

    public List<JobWorkflowStepReq> CreateSteps { get; set; } = [];

    /// <summary>Workflow-owned schedules to create alongside the workflow. Their <c>JobWorkflowId</c> is assigned server-side from the workflow being created.</summary>
    public List<JobScheduleReq> CreateSchedules { get; set; } = [];

    public override string ToString() => $"{Name}, {Description} (Enabled={Enabled}) Steps(C={CreateSteps.Count})";
}