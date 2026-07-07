using System.Diagnostics;
using Lyo.Job.Models.Enums;

namespace Lyo.Job.Models.Response;

[DebuggerDisplay("{ToString(),nq}")]
public sealed record JobWorkflowStepRes(
    Guid Id,
    Guid JobWorkflowId,
    Guid JobDefinitionId,
    string StepName,
    int StepOrder,
    string? DependsOnStepIds,
    JobWorkflowFailurePolicy FailurePolicy,
    string? ParametersJson,
    bool Enabled,
    JobDefinitionRes? JobDefinition = null)
{
    public override string ToString() => $"{StepOrder}: {StepName}";
}
