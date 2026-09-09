namespace Lyo.Drift.Web.Components;

public partial class DriftChangeGrid
{
    /// <summary>HTTP client used for Query.</summary>
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>Drift route prefix.</summary>
    [Parameter]
    [EditorRequired]
    public string BaseRoute { get; set; } = "Drift";

    /// <summary>When set, only changes for this instance are shown.</summary>
    [Parameter]
    public Guid? InstanceId { get; set; }

    private string _route => $"{BaseRoute.TrimEnd('/')}/Change";

    private LyoDataGridProjected? _dataGrid;

    private Guid? _appliedInstanceId;

    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("WatchRoot", "Watch Root")];

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

    private async Task ViewChange(object? item)
    {
        var id = DriftColorHelper.GetGuidId(item);
        if (id is null)
            return;

        var res = await ApiClient.PostAsAsync<QueryConcreteReq, QueryRes<DriftChangeRes>>(
            $"{_route}/QueryConcrete", new() { Keys = [[id.Value]], Amount = 1 });
        var change = res?.Items?.FirstOrDefault();
        if (change is null)
            return;

        var parameters = new DialogParameters<JsonViewDialog<FileSystemChangeDto>> { { d => d.Data, change.Change } };
        await DialogService.ShowAsync<JsonViewDialog<FileSystemChangeDto>>("Change", parameters, LyoDialogPresets.Medium);
    }
}
