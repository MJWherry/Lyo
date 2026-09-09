using System.Text.Json.Nodes;

namespace Lyo.Drift.Web.Components;

public partial class DriftSnapshotGrid
{
    /// <summary>HTTP client used for Query and snapshot actions.</summary>
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>Drift route prefix.</summary>
    [Parameter]
    [EditorRequired]
    public string BaseRoute { get; set; } = "Drift";

    /// <summary>When set, the grid only shows snapshots of this instance.</summary>
    [Parameter]
    public Guid? InstanceId { get; set; }

    /// <summary>Two-way binding so the instance filter chip can clear host state.</summary>
    [Parameter]
    public EventCallback<Guid?> InstanceIdChanged { get; set; }

    private string _route => $"{BaseRoute.TrimEnd('/')}/Snapshot";

    private LyoDataGridProjected? _dataGrid;

    private Guid? _appliedInstanceId;

    private static readonly string[] _quickSearchProperties = ["WatchRoot", "ContentHash"];

    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("Kind"), new("WatchRoot", "Watch Root"), new("ContentHashAlgorithm", "Hash")];

    protected override async Task OnParametersSetAsync()
    {
        if (_appliedInstanceId == InstanceId)
            return;

        _appliedInstanceId = InstanceId;
        if (_dataGrid != null)
            await _dataGrid.RefreshData();
    }

    private void ApplyQuery(ProjectionQueryReqBuilder q)
    {
        if (InstanceId is { } instanceId)
            q.AddWhere(new ConditionClause("InstanceId", ComparisonOperatorEnum.Equals, instanceId));
    }

    private async Task ClearInstanceFilter()
    {
        InstanceId = null;
        _appliedInstanceId = null;
        if (InstanceIdChanged.HasDelegate)
            await InstanceIdChanged.InvokeAsync(null);

        if (_dataGrid != null)
            await _dataGrid.RefreshData();
    }

    private async Task ViewSnapshot(object? item)
    {
        var snapshot = await LoadSnapshotAsync(item);
        if (snapshot is null)
            return;

        var json = snapshot.Kind == DriftSnapshotKind.SystemInfo ? snapshot.SystemInfoJson : snapshot.TreeJson;
        JsonNode? node = null;
        if (!string.IsNullOrWhiteSpace(json)) {
            try {
                node = JsonNode.Parse(json);
            }
            catch (Exception ex) {
                Snackbar.Add($"Snapshot JSON is invalid: {ex.Message}", Severity.Error);
                return;
            }
        }

        var parameters = new DialogParameters<JsonViewDialog<JsonNode?>> { { d => d.Data, node } };
        await DialogService.ShowAsync<JsonViewDialog<JsonNode?>>("Snapshot", parameters, LyoDialogPresets.Large);
    }

    private async Task DiffAgainst(object? item)
    {
        var snapshot = await LoadSnapshotAsync(item);
        if (snapshot is null)
            return;

        var parameters = new DialogParameters<DriftDiffAgainstDialog> {
            { d => d.ApiClient, ApiClient },
            { d => d.BaseRoute, BaseRoute },
            { d => d.FromSnapshot, snapshot }
        };
        var dialog = await DialogService.ShowAsync<DriftDiffAgainstDialog>("Diff against", parameters, LyoDialogPresets.Medium);
        var result = await dialog.Result;
        if (result is { Canceled: false, Data: DriftDiffRes diff }) {
            var view = new DialogParameters<DriftDiffView> { { d => d.Diff, diff } };
            await DialogService.ShowAsync<DriftDiffView>("Diff", view, LyoDialogPresets.Large);
        }
    }

    private async Task<DriftSnapshotRes?> LoadSnapshotAsync(object? item)
    {
        var id = DriftColorHelper.GetGuidId(item);
        if (id is null)
            return null;

        var res = await ApiClient.PostAsAsync<QueryConcreteReq, QueryRes<DriftSnapshotRes>>(
            $"{_route}/QueryConcrete", new() { Keys = [[id.Value]], Amount = 1 });
        return res?.Items?.FirstOrDefault();
    }
}
