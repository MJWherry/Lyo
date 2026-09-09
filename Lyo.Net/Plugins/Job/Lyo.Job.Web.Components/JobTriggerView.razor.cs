namespace Lyo.Job.Web.Components;

public partial class JobTriggerView
{
    [Parameter]
    public IReadOnlyList<JobTriggerRes>? JobTriggers { get; set; }

    [Parameter]
    public Guid JobDefinitionId { get; set; }

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public string TriggerRoute { get; set; } = "";

    /// <summary>Local copy of the trigger list. The enable chip is toggled here as well as through the API, so the rendered state matches what was just persisted.</summary>
    private List<JobTriggerRes> _triggers = [];

    /// <summary>The exact <see cref="JobTriggers" /> instance already cloned, compared by reference so a parent reload reseeds and a plain re-render does not.</summary>
    private IReadOnlyList<JobTriggerRes>? _seededTriggers;

    private JobTriggerRes? _selected;

    protected override void OnParametersSet()
    {
        if (!ReferenceEquals(_seededTriggers, JobTriggers)) {
            _seededTriggers = JobTriggers;
            _triggers = JobTriggers?.ToList() ?? [];
        }

        if (_selected == null || _triggers.All(t => t.Id != _selected.Id))
            _selected = _triggers.FirstOrDefault();
    }

    private async Task ToggleTrigger(JobTriggerRes trigger)
    {
        var enabled = !trigger.Enabled;
        var patch = new PatchRequestBuilder().WithKey(trigger.Id).SetProperty("Enabled", enabled).Build();
        try {
            await ApiClient.PatchAsAsync<PatchRequest, PatchResult<object>>(TriggerRoute, patch);
        }
        catch (Exception ex) {
            Snackbar.Add($"Trigger update failed: {ex.Message}", Severity.Error);
            return;
        }

        var index = _triggers.FindIndex(t => t.Id == trigger.Id);
        if (index >= 0) {
            _triggers[index] = _triggers[index] with { Enabled = enabled };
            if (_selected?.Id == trigger.Id)
                _selected = _triggers[index];
        }

        Snackbar.Add($"Trigger {(enabled ? "enabled" : "disabled")}", Severity.Success);
    }
}
