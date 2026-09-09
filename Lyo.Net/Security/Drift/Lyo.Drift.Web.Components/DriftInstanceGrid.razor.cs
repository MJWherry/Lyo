namespace Lyo.Drift.Web.Components;

public partial class DriftInstanceGrid
{
    /// <summary>HTTP client used for Query and instance actions.</summary>
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>Drift route prefix (for example <c>Drift</c>).</summary>
    [Parameter]
    [EditorRequired]
    public string BaseRoute { get; set; } = "Drift";

    /// <summary>Raised when the user asks to see snapshots for an instance.</summary>
    [Parameter]
    public EventCallback<Guid> OnViewSnapshots { get; set; }

    private string _route => $"{BaseRoute.TrimEnd('/')}/Instance";

    private LyoDataGridProjected? _dataGrid;

    private static readonly string[] _quickSearchProperties = ["InstanceKey", "MachineName"];

    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("InstanceKey", "Key"), new("MachineName", "Machine"), new("State")];

    private static bool CanStop(object? item)
        => Enum.TryParse<DriftInstanceState>(ProjectedValueHelper.GetDisplayValue(item, "State"), ignoreCase: true, out var state)
            && state == DriftInstanceState.Running;

    private async Task ViewInstance(object? item)
    {
        var instance = await LoadInstanceAsync(item);
        if (instance is null)
            return;

        var parameters = new DialogParameters<DriftInstanceView> { { d => d.Instance, instance } };
        await DialogService.ShowAsync<DriftInstanceView>("Drift instance", parameters, LyoDialogPresets.Medium);
    }

    private async Task ViewSnapshots(object? item)
    {
        var id = DriftColorHelper.GetGuidId(item);
        if (id is null || !OnViewSnapshots.HasDelegate)
            return;

        await OnViewSnapshots.InvokeAsync(id.Value);
    }

    private async Task StopInstance(object? item)
    {
        var id = DriftColorHelper.GetGuidId(item);
        if (id is null)
            return;

        if (!await DialogService.ConfirmAsync("Stop instance", "Mark this drift agent instance as Stopped? Heartbeats will still be accepted if the process is alive.", "Stop"))
            return;

        try {
            await ApiClient.PostAsAsync<DriftInstanceRes>($"{_route}/{id}/Stop");
            Snackbar.Add("Instance marked stopped", Severity.Info);
            if (_dataGrid != null)
                await _dataGrid.RefreshData();
        }
        catch (Exception ex) {
            Snackbar.Add($"Stop failed: {ex.Message}", Severity.Error);
        }
    }

    private async Task DeleteInstance(object? item)
    {
        var id = DriftColorHelper.GetGuidId(item);
        if (id is null)
            return;

        if (!await DialogService.ConfirmDeleteAsync($"drift instance {id.Value.ToString()[..8]}…", "Snapshots, diffs, and changes for this instance are removed.", "Confirm Delete"))
            return;

        try {
            await ApiClient.DeleteAsAsync<object>($"{_route}/{id}");
            Snackbar.Add("Instance deleted", Severity.Success);
            if (_dataGrid != null)
                await _dataGrid.RefreshData();
        }
        catch (Exception ex) {
            Snackbar.Add($"Delete failed: {ex.Message}", Severity.Error);
        }
    }

    private async Task<DriftInstanceRes?> LoadInstanceAsync(object? item)
    {
        var id = DriftColorHelper.GetGuidId(item);
        if (id is null)
            return null;

        var res = await ApiClient.PostAsAsync<QueryConcreteReq, QueryRes<DriftInstanceRes>>(
            $"{_route}/QueryConcrete", new() { Keys = [[id.Value]], Amount = 1 });
        return res?.Items?.FirstOrDefault();
    }
}
