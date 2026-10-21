using System.Text.Json.Nodes;
using Lyo.Web.Automation.Models;
using Microsoft.AspNetCore.Components;

namespace Lyo.Web.Automation.Web.Components;

/// <summary>Edits an automation plan. It does not start a browser. Steps without a form stay in the plan and can be changed in the JSON pane.</summary>
public partial class AutomationPlanWorkbench : ComponentBase
{
    private string _addType = "navigate";
    private string? _jsonError;
    private JsonNode? _jsonNode;
    private AutomationPlan _plan = new("Untitled", []);
    private int _selected = -1;

    /// <summary>Plan to edit. When null, the workbench starts from an empty untitled plan.</summary>
    [Parameter]
    public AutomationPlan? Plan { get; set; }

    /// <summary>Raised after an edit so the host can keep the selection.</summary>
    [Parameter]
    public EventCallback<AutomationPlan> PlanChanged { get; set; }

    private AutomationStepDefinition? Selected => _selected >= 0 && _selected < _plan.Steps.Count ? _plan.Steps[_selected] : null;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Plan is not null && !ReferenceEquals(Plan, _plan)) {
            _plan = Plan;
            _selected = _plan.Steps.Count == 0 ? -1 : Math.Clamp(_selected, 0, _plan.Steps.Count - 1);
            SyncJson();
        }
    }

    private void AddStep()
    {
        var steps = _plan.Steps.ToList();
        steps.Add(AutomationPlanDraft.CreateStep(_addType));
        _plan = _plan with { Steps = steps };
        _selected = steps.Count - 1;
        SyncFromPlan();
    }

    private void RemoveSelected()
    {
        if (Selected is null)
            return;

        var steps = _plan.Steps.ToList();
        steps.RemoveAt(_selected);
        _plan = _plan with { Steps = steps };
        _selected = steps.Count == 0 ? -1 : Math.Min(_selected, steps.Count - 1);
        SyncFromPlan();
    }

    private void Move(int toIndex)
    {
        if (Selected is null)
            return;

        _plan = AutomationPlanDraft.MoveStep(_plan, _selected, toIndex);
        _selected = toIndex;
        SyncFromPlan();
    }

    private void Select(int index) => _selected = index;

    private void OnName(string name)
    {
        _plan = _plan with { Name = name };
        SyncFromPlan();
    }

    private void OnStepChanged(AutomationStepDefinition step)
    {
        if (Selected is null)
            return;

        var steps = _plan.Steps.ToList();
        steps[_selected] = step;
        _plan = _plan with { Steps = steps };
        SyncJson();
        _ = PlanChanged.InvokeAsync(_plan);
    }

    private void OnJsonNode(JsonNode? node)
    {
        _jsonNode = node;
        _jsonError = null;
    }

    private void OnJsonError(string? error) => _jsonError = error;

    private void ApplyJson()
    {
        if (_jsonError != null)
            return;

        try {
            var json = _jsonNode?.ToJsonString(AutomationPlanDraft.JsonOptions) ?? throw new InvalidOperationException("Invalid plan JSON.");
            _plan = AutomationPlanDraft.Parse(json);
            _jsonError = null;
            _selected = _plan.Steps.Count == 0 ? -1 : Math.Clamp(_selected, 0, _plan.Steps.Count - 1);
            _ = PlanChanged.InvokeAsync(_plan);
        }
        catch (Exception ex) {
            _jsonError = ex.Message;
        }
    }

    private void SyncFromPlan()
    {
        SyncJson();
        _ = PlanChanged.InvokeAsync(_plan);
    }

    private void SyncJson()
    {
        _jsonNode = JsonNode.Parse(AutomationPlanDraft.ToJson(_plan));
        _jsonError = null;
    }
}