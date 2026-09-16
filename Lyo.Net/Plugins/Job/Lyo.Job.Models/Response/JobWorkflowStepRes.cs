using System.Diagnostics;
using Lyo.Job.Models.Enums;
using Lyo.Query.Models.Common;

namespace Lyo.Job.Models.Response;

/// <summary>
/// One step of a workflow. <see cref="ConditionJson" /> is the step's gate: a serialized <see cref="WhereClause" /> tested against the step's context, where each
/// <c>ConditionClause.Field</c> is a context path (<c>Step.Extract.Result.RowCount</c>, <c>Input.Env</c>, <c>Workflow.Name</c>) and a <c>GroupClause</c> combines predicates
/// with AND/OR. A step whose gate does not open is marked <see cref="JobWorkflowStepState.ConditionNotMet" />.
/// </summary>
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
    JobDefinitionRes? JobDefinition = null,
    string? ConditionJson = null)
{
    public override string ToString() => $"{StepOrder}: {StepName}";
}
