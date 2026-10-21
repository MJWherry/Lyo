using System.Diagnostics;
using Lyo.Job.Models.Enums;
using Lyo.Query.Models.Common;

namespace Lyo.Job.Models.Request;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class JobWorkflowStepReq
{
    /// <summary>
    /// Client-minted step id. Leave unset to have the API mint one. Setting it is what lets a caller express <see cref="DependsOnStepIds" /> in the same POST that creates the
    /// steps, instead of creating them first and patching the dependencies afterwards.
    /// </summary>
    public Guid Id { get; set; }

    public Guid JobWorkflowId { get; set; }

    public Guid JobDefinitionId { get; set; }

    public string StepName { get; set; } = null!;

    public int StepOrder { get; set; }

    /// <summary>Comma-separated step ids that must finish before this step may run.</summary>
    public string? DependsOnStepIds { get; set; }

    public JobWorkflowFailurePolicy FailurePolicy { get; set; } = JobWorkflowFailurePolicy.Stop;

    public string? ParametersJson { get; set; }

    /// <summary>
    /// Serialized <see cref="WhereClause" /> gating this step, evaluated against the step's context. Each <c>ConditionClause.Field</c> is a context path such as
    /// <c>Step.Extract.Result.RowCount</c>, <c>Input.Env</c>, or <c>Workflow.Name</c>, and a <c>GroupClause</c> combines predicates with AND/OR. Null runs the step unconditionally.
    /// </summary>
    public string? ConditionJson { get; set; }

    public bool Enabled { get; set; } = true;

    public override string ToString() => $"{StepOrder}: {StepName} -> {JobDefinitionId}";
}