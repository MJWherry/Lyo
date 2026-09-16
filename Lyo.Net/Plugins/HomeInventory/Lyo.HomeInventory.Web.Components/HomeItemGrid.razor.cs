using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Query.Models.Common.Request;
using Lyo.Web.Components.DataGrid;
using Lyo.Web.Components.Dialog;
using Lyo.Web.Components.Models;

namespace Lyo.HomeInventory.Web.Components;

/// <summary>Projected item grid with create/edit dialogs.</summary>
public partial class HomeItemGrid
{
    /// <summary>HTTP client used for QueryProject and CRUD.</summary>
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>HomeInventory route prefix.</summary>
    [Parameter]
    [EditorRequired]
    public string BaseRoute { get; set; } = "HomeInventory";

    /// <summary>FileStorage MapGroup prefix for attachment downloads.</summary>
    [Parameter]
    public string FileStorageApiRoutePrefix { get; set; } = "FileStorage";

    private string _itemsRoute => $"{BaseRoute.TrimEnd('/')}/Items";

    private LyoDataGridProjected? _dataGrid;

    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("Name"), new("Status"), new("Condition")];

    private async Task OpenCreate()
    {
        var parameters = new DialogParameters<HomeItemView> {
            { d => d.ApiClient, ApiClient },
            { d => d.BaseRoute, BaseRoute },
            { d => d.FileStorageApiRoutePrefix, FileStorageApiRoutePrefix },
            { d => d.Item, null }
        };
        var dialog = await DialogService.ShowAsync<HomeItemView>("New item", parameters, LyoDialogPresets.Medium);
        var result = await dialog.Result;
        if (result is { Canceled: false } && _dataGrid != null)
            await _dataGrid.RefreshData();
    }

    private async Task OpenItem(object? item)
    {
        if (!ProjectedValueHelper.TryGetGuid(ProjectedValueHelper.GetValue(item, "Id"), out var id))
            return;

        var res = await ApiClient.QueryConcreteAsync<QueryRes<HomeItemRes>>(_itemsRoute, new QueryConcreteReq { Keys = [[id]], Amount = 1 });
        var loaded = res?.Items?.FirstOrDefault();
        if (loaded == null)
            return;

        var parameters = new DialogParameters<HomeItemView> {
            { d => d.ApiClient, ApiClient },
            { d => d.BaseRoute, BaseRoute },
            { d => d.FileStorageApiRoutePrefix, FileStorageApiRoutePrefix },
            { d => d.Item, loaded }
        };
        var dialog = await DialogService.ShowAsync<HomeItemView>("Item", parameters, LyoDialogPresets.Medium);
        var result = await dialog.Result;
        if (result is { Canceled: false } && _dataGrid != null)
            await _dataGrid.RefreshData();
    }
}
