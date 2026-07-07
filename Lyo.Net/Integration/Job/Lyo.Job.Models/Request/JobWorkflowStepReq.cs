using System.Diagnostics;
using Lyo.Job.Models.Enums;

namespace Lyo.Job.Models.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class JobWorkflowStepReq
{
    public Guid JobWorkflowId { get; set; }

    public Guid JobDefinitionId { get; set; }

    public string StepName { get; set; } = null!;

    public int StepOrder { get; set; }

    /// <summary>Comma-separated step ids that must finish before this step can run.</summary>
    public string? DependsOnStepIds { get; set; }

    public JobWorkflowFailurePolicy FailurePolicy { get; set; } = JobWorkflowFailurePolicy.Stop;

    public string? ParametersJson { get; set; }

    public bool Enabled { get; set; } = true;

    public override string ToString() => $"{StepOrder}: {StepName} -> {JobDefinitionId}";
}
