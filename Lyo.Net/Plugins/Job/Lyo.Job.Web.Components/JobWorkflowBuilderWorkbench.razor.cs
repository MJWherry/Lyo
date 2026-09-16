using System.Text.Json;
using Lyo.Exceptions.Models;
using Lyo.Job.Models.Builders;
using Lyo.Web.Components.ParamTable;
using Microsoft.Extensions.Logging;

namespace Lyo.Job.Web.Components;

/// <summary>
/// Authoring surface for a workflow: a visual DAG builder, the exact request JSON it produces, and a run panel, all over one draft persisted through <c>ClientStore</c>.
/// <para>
/// The request is assembled by <see cref="JobWorkflowBuilder" /> on every edit rather than hand-built here. That is what makes the editor's rules the real rules —
/// duplicate names, unknown dependencies, and cycles are rejected by the same code the API and the engine rely on, so nothing is enforced only in the UI.
/// </para>
/// </summary>
public partial class JobWorkflowBuilderWorkbench : IAsyncDisposable
{
    /// <summary>Under the API's include-query page cap. Matches `JobScheduleGrid`, which pulls the same definition list with a navigation included.</summary>
    private const int DefinitionQueryAmount = 200;

    private const int SaveDebounceMs = 450;

    private string? _buildError;
    private IReadOnlyList<JobDefinitionRes> _definitions = [];
    private List<LyoParameterEditRow> _inputRows = [];
    private JobWorkflowReq? _request;
    private CancellationTokenSource? _saveDebounceCts;
    private JobWorkflowStepDraft? _selected;
    private bool _saving;
    private bool _starting;
    private JobWorkflowWorkbenchState _state = new();

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public string BaseRoute { get; set; } = "Job";

    [Parameter]
    public string Title { get; set; } = "Workflow Builder";

    [Inject]
    private ClientStore ClientStore { get; set; } = null!;

    [Inject]
    private JsonSerializerOptions JsonOptions { get; set; } = null!;

    [Inject]
    private ILogger<JobWorkflowBuilderWorkbench> Logger { get; set; } = null!;

    private string WorkflowRoute => $"{BaseRoute.TrimEnd('/')}/Workflow";

    public async ValueTask DisposeAsync()
    {
        _saveDebounceCts?.Cancel();
        _saveDebounceCts?.Dispose();
        _saveDebounceCts = null;
        try {
            await PersistNowAsync().ConfigureAwait(false);
        }
        catch (Exception ex) {
            Logger.LogDebug(ex, "Workflow workbench: final save skipped.");
        }
    }

    protected override async Task OnInitializedAsync()
    {
        try {
            var json = await ClientStore.GetWorkflowWorkbenchStateAsync().ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(json))
                _state = JobWorkflowDraftLinks.Load(json, JsonOptions);
        }
        catch (Exception ex) {
            Logger.LogDebug(ex, "Workflow workbench: could not load persisted state.");
        }

        _inputRows = [.. _state.Inputs.Select(i => new LyoParameterEditRow { Key = i.Key, Value = i.Value })];
        await LoadDefinitionsAsync();
        RebuildRequest();
    }

    /// <summary>The editor owns <see cref="_inputRows" /> in place, so a change means projecting them back. Blank keys are rows the user has not named yet.</summary>
    private Task OnInputsChangedAsync()
    {
        _state.Inputs = _inputRows.Where(r => !string.IsNullOrWhiteSpace(r.Key))
            .GroupBy(r => r.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().ValueForSave ?? "", StringComparer.OrdinalIgnoreCase);

        return OnStepChangedAsync();
    }

    private async Task LoadDefinitionsAsync()
    {
        try {
            // Parameters come along so the step editor can offer the definition's declared keys, types, and value pickers instead of a free-form JSON blob.
            var res = await ApiClient.PostAsAsync<QueryConcreteReq, QueryRes<JobDefinitionRes>>(
                $"{BaseRoute.TrimEnd('/')}/Definition/QueryConcrete", new() { Amount = DefinitionQueryAmount, Include = ["JobParameters"] });

            _definitions = res?.Items ?? [];
        }
        catch (Exception ex) {
            Snackbar.Add($"Failed to load job definitions: {ex.Message}", Severity.Error);
        }
    }

    private IReadOnlyList<JobWorkflowStepDraft> OtherSteps(JobWorkflowStepDraft step)
        => [.. _state.Steps.Where(s => !ReferenceEquals(s, step) && !string.IsNullOrWhiteSpace(s.StepName))];

    private IReadOnlyList<string> InputKeys => [.. _state.Inputs.Keys];

    /// <summary>The draft as the response shape the step context reads <c>Workflow.*</c> from. The id stays empty until the workflow is saved.</summary>
    private JobWorkflowRes DraftWorkflow
        => new(_state.LastStartedWorkflowId ?? Guid.Empty, string.IsNullOrWhiteSpace(_state.Name) ? "Untitled workflow" : _state.Name, _state.Description, _state.Enabled, null);

    /// <summary>
    /// Held by reference rather than resolved from <see cref="JobWorkflowWorkbenchState.SelectedStepId" /> on every render. The persisted id is re-derived from this after
    /// each change.
    /// </summary>
    private JobWorkflowStepDraft? SelectedStep
        => _selected is not null && _state.Steps.Contains(_selected)
            ? _selected
            : _selected = _state.Steps.FirstOrDefault(s => s.Id == _state.SelectedStepId) ?? _state.Steps.FirstOrDefault();

    private Task SelectStepAsync(Guid stepId)
    {
        _selected = _state.Steps.FirstOrDefault(s => s.Id == stepId);
        return OnStepChangedAsync();
    }

    /// <summary>Appends a step that waits on the selected one, so repeated clicks build a chain rather than a pile of unrelated steps all starting at once.</summary>
    private Task AddStep() => AddStep(SelectedStep is { } previous ? [previous.Id] : []);

    /// <summary>
    /// Appends a sibling of the selected step: same dependencies, so the two run side by side. This is how a fan-out is authored — two reports off one extract, with a
    /// later step depending on both.
    /// </summary>
    private Task AddStep(IEnumerable<Guid> dependsOn)
    {
        var step = new JobWorkflowStepDraft {
            StepName = NextStepName(),
            JobDefinitionId = _definitions.Count > 0 ? _definitions[0].Id : Guid.Empty,
            DependsOn = [.. dependsOn]
        };

        _state.Steps.Add(step);

        // A new step is what the user is about to configure, and only the selected step has an editor.
        _selected = step;
        return OnStepChangedAsync();
    }

    private Task AddParallelStep() => AddStep(SelectedStep?.DependsOn ?? []);

    /// <summary>Names must be unique for the builder to accept the draft, so a new step gets the first free "Step N" rather than a placeholder the user must fix.</summary>
    private string NextStepName()
    {
        for (var index = _state.Steps.Count + 1; ; index++) {
            var candidate = $"Step {index}";
            if (!_state.Steps.Any(s => string.Equals(s.StepName, candidate, StringComparison.OrdinalIgnoreCase)))
                return candidate;
        }
    }

    private Task RemoveStepAsync(JobWorkflowStepDraft step)
    {
        _state.Steps.Remove(step);
        if (ReferenceEquals(_selected, step))
            _selected = null;

        // Leaving references to a removed step would make every later build fail with an unknown-dependency error the user cannot see the cause of.
        var prefix = $"{JobWorkflowStepCondition.StepPathPrefix}{step.StepName}.";
        foreach (var other in _state.Steps) {
            other.DependsOn.RemoveAll(id => id == step.Id);
            other.Condition = Prune(other.Condition, prefix);
        }

        return OnStepChangedAsync();
    }

    /// <summary>
    /// Drops every predicate reading the removed step out of a gate, keeping the rest. Clearing the whole clause instead would silently un-gate a step the user had
    /// deliberately conditioned on several things.
    /// </summary>
    /// <returns>The pruned clause, or null once nothing testable is left.</returns>
    private static WhereClause? Prune(WhereClause? clause, string prefix)
    {
        if (clause is null)
            return null;

        clause.SubClause = Prune(clause.SubClause, prefix);
        if (clause is GroupClause group) {
            group.Children = [.. group.Children.Select(child => Prune(child, prefix)).OfType<WhereClause>()];
            return group.Children.Count == 0 && group.SubClause is null ? null : group;
        }

        // A condition on the removed step evaluates against a path nothing seeds any more, which would strand the step rather than gate it.
        return clause is ConditionClause condition && condition.Field?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == true ? condition.SubClause : clause;
    }

    private Task OnNameChanged(string value)
    {
        _state.Name = value;
        return OnStepChangedAsync();
    }

    private Task OnDescriptionChanged(string? value)
    {
        _state.Description = value;
        return OnStepChangedAsync();
    }

    private Task OnEnabledChanged(bool value)
    {
        _state.Enabled = value;
        return OnStepChangedAsync();
    }

    private async Task OnStepChangedAsync()
    {
        _state.SelectedStepId = SelectedStep?.Id ?? Guid.Empty;
        _state.SelectedStepName = SelectedStep?.StepName;
        RebuildRequest();
        SchedulePersist();
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Re-derives the request from the draft. A build failure is shown rather than thrown: a half-finished draft (a blank name, a dependency not added yet) is the normal
    /// state while editing, and the message is the same one a caller would get from the builder.
    /// </summary>
    private void RebuildRequest()
    {
        try {
            var builder = JobWorkflowBuilder.New(string.IsNullOrWhiteSpace(_state.Name) ? "Untitled workflow" : _state.Name, _state.Description)
                .WithEnabled(_state.Enabled);

            foreach (var step in _state.Steps)
                builder.AddStep(
                    step.StepName, step.JobDefinitionId, JobWorkflowDraftLinks.DependencyNames(_state.Steps, step), step.Condition, step.Parameters, step.FailurePolicy,
                    enabled: step.Enabled);

            _request = builder.Build();
            _buildError = null;
        }
        catch (ValidationException ex) {
            _request = null;
            _buildError = ex.Message;
        }
    }

    private async Task SaveWorkflowAsync()
    {
        if (_request is null) {
            Snackbar.Add(_buildError ?? "The workflow is not valid yet.", Severity.Warning);
            return;
        }

        _saving = true;
        try {
            var created = await ApiClient.PostAsAsync<JobWorkflowReq, JobWorkflowRes>(WorkflowRoute, _request);
            if (created is null) {
                Snackbar.Add("The API did not return the created workflow.", Severity.Error);
                return;
            }

            _state.LastStartedWorkflowId = created.Id;
            SchedulePersist();
            Snackbar.Add($"Saved workflow '{created.Name}'.", Severity.Success);
        }
        catch (Exception ex) {
            Snackbar.Add($"Failed to save workflow: {ex.Message}", Severity.Error);
        }
        finally {
            _saving = false;
        }
    }

    private async Task StartRunAsync(Guid workflowId)
    {
        _starting = true;
        try {
            await ApiClient.PostAsAsync<JobWorkflowStartReq, JobWorkflowRunRes>($"{WorkflowRoute}/{workflowId}/Start", new() { Inputs = _state.Inputs });
            Snackbar.Add("Workflow run started.", Severity.Success);
        }
        catch (Exception ex) {
            Snackbar.Add($"Failed to start workflow: {ex.Message}", Severity.Error);
        }
        finally {
            _starting = false;
        }
    }

    private async Task ResetAsync()
    {
        _state = new();
        _selected = null;
        _inputRows = [];
        RebuildRequest();
        await ClientStore.RemoveWorkflowWorkbenchStateAsync();
        await InvokeAsync(StateHasChanged);
    }

    private void SchedulePersist()
    {
        _saveDebounceCts?.Cancel();
        _saveDebounceCts?.Dispose();
        _saveDebounceCts = new();
        _ = DebounceSaveAsync(_saveDebounceCts.Token);
    }

    private async Task DebounceSaveAsync(CancellationToken ct)
    {
        try {
            await Task.Delay(SaveDebounceMs, ct).ConfigureAwait(false);
            await PersistNowAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            // Replaced by a later edit, or disposing.
        }
        catch (Exception ex) {
            Logger.LogDebug(ex, "Workflow workbench: could not save state.");
        }
    }

    private async Task PersistNowAsync() => await ClientStore.SetWorkflowWorkbenchStateAsync(JsonSerializer.Serialize(_state, JsonOptions)).ConfigureAwait(false);
}
