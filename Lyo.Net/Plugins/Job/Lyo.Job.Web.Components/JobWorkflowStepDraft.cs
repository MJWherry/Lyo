using System.Text.Json.Serialization;
using Lyo.Common.Core.Identifiers;

namespace Lyo.Job.Web.Components;

/// <summary>
/// One step as the editor holds it, before <see cref="Lyo.Job.Models.Builders.JobWorkflowBuilder" /> turns it into a request.
/// <para>
/// Dependencies are draft ids, minted here, so renaming a step does not break the steps that wait on it. The builder is still the place that turns those into the names the
/// request and the runtime paths use.
/// </para>
/// </summary>
public sealed class JobWorkflowStepDraft
{
    /// <summary>Stable identity for this draft. Links and the graph selection use it; the name is only a label.</summary>
    public Guid Id { get; set; } = LyoGuid.CreateV7();

    public string StepName { get; set; } = "";

    public Guid JobDefinitionId { get; set; }

    /// <summary>Draft ids of the steps that must finish first. A name is resolved to one of these on load, then never stored again.</summary>
    public List<Guid> DependsOn { get; set; } = [];

    /// <summary>Raw <c>DependsOn</c> entries from a persisted draft, resolved by <see cref="JobWorkflowDraftLinks.Repair" /> before the editor sees them.</summary>
    [JsonIgnore]
    public List<string> PendingDependsOn { get; set; } = [];

    public JobWorkflowFailurePolicy FailurePolicy { get; set; } = JobWorkflowFailurePolicy.Stop;

    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Predicate tree gating this step, or null to always run. Fields are context paths — <c>Step.Extract.Result.RowCount</c>, <c>Input.Env</c>, <c>Workflow.Name</c> — and a
    /// <see cref="GroupClause" /> nests them under AND/OR.
    /// </summary>
    public WhereClause? Condition { get; set; }

    /// <summary>Parameter overrides keyed by parameter name. Serialized by the builder into the flat object map the scheduler merges.</summary>
    public Dictionary<string, string> Parameters { get; set; } = [];

    public bool HasCondition => Condition is not null;

    /// <summary>The condition on one line, for a chip or a panel header. <see cref="WhereClause.Print" /> indents over several lines, which neither has room for.</summary>
    public string? ConditionText => Condition is null ? null : string.Join(" ", Condition.Print().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}