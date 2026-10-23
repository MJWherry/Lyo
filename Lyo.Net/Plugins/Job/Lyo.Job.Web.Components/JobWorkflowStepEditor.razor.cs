using Lyo.Formatter.Web.Components;
using Lyo.Job.Models.Context;
using Lyo.Web.Components.ParamTable;

namespace Lyo.Job.Web.Components;

/// <summary>
/// Edits one <see cref="JobWorkflowStepDraft" />: which definition it runs, what it depends on, its failure policy, its parameter overrides, and the predicate tree that gates it.
/// <para>
/// Dependencies are picked by draft id, and the list of choices excludes this step, so the editor cannot express a self-reference. The remaining ways to build an invalid graph
/// (a longer cycle, a gate reading a step this one does not depend on) are caught by the builder when the workbench assembles the request.
/// </para>
/// <para>
/// Parameter templates and condition fields are both offered from one <see cref="JobWorkflowFormatterContext" /> bag, which is the same shape the engine seeds when it starts
/// the step. Neither list is written by hand, so the editor cannot advertise a path the runtime does not resolve.
/// </para>
/// </summary>
public partial class JobWorkflowStepEditor
{
    private List<FilterPropertyDefinition> _contextPaths = [];
    private IReadOnlyList<LyoParameterEditRow> _inheritRows = [];
    private List<LyoParameterEditRow> _paramRows = [];
    private JobContextBag _stepContext = new();
    private JobWorkflowStepDraft? _syncedStep;

    [Parameter]
    [EditorRequired]
    public JobWorkflowStepDraft Step { get; set; } = null!;

    /// <summary>Every other step in the draft. Supplies the depends-on choices, and the definitions behind them so upstream paths can be offered.</summary>
    [Parameter]
    public IReadOnlyList<JobWorkflowStepDraft> OtherSteps { get; set; } = [];

    /// <summary>Every step, including this one, so the step picker can change which draft is being edited.</summary>
    [Parameter]
    public IReadOnlyList<JobWorkflowStepDraft> Steps { get; set; } = [];

    /// <summary>Raised when the step picker chooses a different draft.</summary>
    [Parameter]
    public EventCallback<Guid> StepSelected { get; set; }

    [Parameter]
    public IReadOnlyList<JobDefinitionRes> Definitions { get; set; } = [];

    /// <summary>Run input keys the workbench declares, offered as <c>Input.{key}</c> paths.</summary>
    [Parameter]
    public IReadOnlyList<string> InputKeys { get; set; } = [];

    /// <summary>The workflow being authored, supplying the <c>Workflow.*</c> paths.</summary>
    [Parameter]
    public JobWorkflowRes? Workflow { get; set; }

    /// <summary>Needed by <see cref="LyoParameterEditor" /> to render query-backed value pickers declared on the definition's parameters.</summary>
    [Parameter]
    public IApiClient? ApiClient { get; set; }

    /// <summary>Raised after any edit so the workbench can re-derive the request and persist the draft.</summary>
    [Parameter]
    public EventCallback OnChanged { get; set; }

    [Parameter]
    public EventCallback<JobWorkflowStepDraft> OnRemove { get; set; }

    private JobDefinitionRes? SelectedDefinition => Definitions.FirstOrDefault(d => d.Id == Step.JobDefinitionId);

    private string ConditionSummary => Step.ConditionText is { } text ? $"Condition: {text}" : "Condition: always runs";

    private string ParametersSummary => Step.Parameters.Count == 0 ? "Parameters: definition defaults" : $"Parameters: {Step.Parameters.Count} override(s)";

    protected override void OnParametersSet()
    {
        _inheritRows = [
            .. (SelectedDefinition?.JobParameters ?? []).OrderBy(p => p.Order)
            .Select(p => new LyoParameterEditRow {
                Key = p.Key,
                Type = p.Type,
                Options = p.Options,
                AllowedValues = p.AllowedValues,
                Description = p.Description
            })
        ];

        BuildStepContext();

        // Reseeded only when the workbench hands over a different draft. Rebuilding every render would drop the row the user is mid-edit on.
        if (ReferenceEquals(_syncedStep, Step))
            return;

        _syncedStep = Step;
        _paramRows = [.. Step.Parameters.Select((p, index) => new LyoParameterEditRow { Key = p.Key, Value = p.Value, Order = index })];
        foreach (var row in _paramRows)
            row.Type = _inheritRows.FirstOrDefault(i => string.Equals(i.Key, row.Key, StringComparison.OrdinalIgnoreCase))?.Type ?? row.Type;
    }

    /// <summary>
    /// The bag both the template editor and the condition field picker read. Walking it into dotted paths is what removes the hand-written path list: whatever the engine seeds
    /// is what the editor offers, down to <see cref="LyoFormatterContextCatalog.MaxDepth" /> levels.
    /// </summary>
    private void BuildStepContext()
    {
        var dependencies = Step.DependsOn.Select(id => OtherSteps.FirstOrDefault(s => s.Id == id))
            .Where(s => s is not null)
            .Select(s => (s!.StepName, Definitions.FirstOrDefault(d => d.Id == s.JobDefinitionId)));

        _stepContext = JobWorkflowFormatterContext.Build(ToStepRes(Step), SelectedDefinition, Workflow, InputKeys, dependencies);
        _contextPaths = [.. JobWorkflowFormatterContext.Paths(_stepContext).Select(p => new FilterPropertyDefinition(p))];
    }

    /// <summary>The draft as the response shape the context seeders take. Ids stay empty: nothing in the bag resolves through them while authoring.</summary>
    private static JobWorkflowStepRes ToStepRes(JobWorkflowStepDraft step)
        => new(Guid.Empty, Guid.Empty, step.JobDefinitionId, step.StepName, 0, null, step.FailurePolicy, null, step.Enabled);

    private Task OnStepNameChanged(string value) => Apply(() => JobWorkflowDraftLinks.Rename([Step, .. OtherSteps], Step, value));

    private Task OnDefinitionChanged(Guid value) => Apply(() => Step.JobDefinitionId = value);

    private Task OnEnabledChanged(bool value) => Apply(() => Step.Enabled = value);

    private Task OnFailurePolicyChanged(JobWorkflowFailurePolicy value) => Apply(() => Step.FailurePolicy = value);

    private Task OnDependsOnChanged(IReadOnlyCollection<Guid>? values) => Apply(() => Step.DependsOn = [.. values ?? []]);

    private static string DependsOnSummary(IReadOnlyList<string?>? selected) => selected is null || selected.Count == 0 ? "No dependencies" : string.Join(", ", selected);

    private Task OnConditionChanged(WhereClause? value) => Apply(() => Step.Condition = value);

    /// <summary>The editor owns <see cref="_paramRows" /> in place, so a change means projecting them back onto the draft. Blank keys are rows the user has not named yet.</summary>
    private Task OnParametersChanged()
        => Apply(() => Step.Parameters = _paramRows.Where(r => !string.IsNullOrWhiteSpace(r.Key))
            .GroupBy(r => r.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().ValueForSave ?? "", StringComparer.OrdinalIgnoreCase));

    private async Task Apply(Action edit)
    {
        edit();
        await OnChanged.InvokeAsync();
    }
}