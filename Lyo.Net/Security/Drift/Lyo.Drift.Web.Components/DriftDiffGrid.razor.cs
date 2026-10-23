namespace Lyo.Drift.Web.Components;

public partial class DriftDiffGrid
{
    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("Source")];

    private LyoDataGridProjected? _dataGrid;

    /// <summary>HTTP client used for Query.</summary>
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>Drift route prefix.</summary>
    [Parameter]
    [EditorRequired]
    public string BaseRoute { get; set; } = "Drift";

    private string _route => $"{BaseRoute.TrimEnd('/')}/Diff";

    private async Task ViewDiff(object? item)
    {
        var id = DriftColorHelper.GetGuidId(item);
        if (id is null)
            return;

        var res = await ApiClient.PostAsAsync<QueryConcreteReq, QueryRes<DriftDiffRes>>($"{_route}/QueryConcrete", new() { Keys = [[id.Value]], Amount = 1 });
        var diff = res?.Items?.FirstOrDefault();
        if (diff is null)
            return;

        var parameters = new DialogParameters<DriftDiffView> { { d => d.Diff, diff } };
        await DialogService.ShowAsync<DriftDiffView>("Diff", parameters, LyoDialogPresets.Large);
    }
}