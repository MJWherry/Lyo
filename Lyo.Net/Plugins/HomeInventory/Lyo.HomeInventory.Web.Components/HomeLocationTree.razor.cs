using Lyo.Api.Models.Common.Response;
using Lyo.Query.Models.Common.Request;
using Lyo.Web.Components.Dialog;
using MudBlazor;

namespace Lyo.HomeInventory.Web.Components;

/// <summary>Selectable room tree with an inspector for child rooms and items in the selected room.</summary>
public partial class HomeLocationTree
{
    /// <summary>HTTP client.</summary>
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>HomeInventory route prefix.</summary>
    [Parameter]
    [EditorRequired]
    public string BaseRoute { get; set; } = "HomeInventory";

    /// <summary>FileStorage MapGroup prefix for the item editor.</summary>
    [Parameter]
    public string FileStorageApiRoutePrefix { get; set; } = "FileStorage";

    private List<TreeItemData<HomeLocationRes>> _nodes = [];
    private HomeLocationRes? _selected;
    private string _draftName = "";
    private List<HomeItemRes> _items = [];

    private string _locationsRoute => $"{BaseRoute.TrimEnd('/')}/Locations";

    private string _itemsRoute => $"{BaseRoute.TrimEnd('/')}/Items";

    protected override Task OnInitializedAsync() => ReloadTree();

    private async Task ReloadTree()
    {
        var selectedId = _selected?.Id;
        var res = await ApiClient.QueryConcreteAsync<QueryRes<HomeLocationRes>>(_locationsRoute, new QueryConcreteReq { Amount = 500 });
        var list = res?.Items ?? [];
        var byParent = list.ToLookup(l => l.ParentLocationId);
        _nodes = Build(null, byParent);
        _selected = selectedId is Guid id ? list.FirstOrDefault(l => l.Id == id) : null;
        await LoadItems();
    }

    private static List<TreeItemData<HomeLocationRes>> Build(Guid? parentId, ILookup<Guid?, HomeLocationRes> byParent)
        => byParent[parentId]
            .OrderBy(l => l.Name)
            .Select(l => new TreeItemData<HomeLocationRes> { Value = l, Children = Build(l.Id, byParent) })
            .ToList();

    private async Task OnSelectedAsync(HomeLocationRes? value)
    {
        _selected = value;
        await LoadItems();
    }

    private async Task LoadItems()
    {
        if (_selected is null) {
            _items = [];
            return;
        }

        var res = await ApiClient.QueryConcreteAsync<QueryRes<HomeItemRes>>(_itemsRoute, new QueryConcreteReq { Amount = 500 });
        _items = (res?.Items ?? []).Where(i => i.LocationId == _selected.Id).OrderBy(i => i.Name).ToList();
    }

    private async Task AddRoom()
    {
        var name = _draftName.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return;

        await ApiClient.PostAsAsync<HomeLocationReq, CreateResult<HomeLocationRes>>(
            _locationsRoute, new HomeLocationReq { Name = name, ParentLocationId = _selected?.Id });
        _draftName = "";
        await ReloadTree();
    }

    private async Task OpenItem(HomeItemRes item)
    {
        var parameters = new DialogParameters<HomeItemView> {
            { d => d.ApiClient, ApiClient },
            { d => d.BaseRoute, BaseRoute },
            { d => d.FileStorageApiRoutePrefix, FileStorageApiRoutePrefix },
            { d => d.Item, item }
        };
        var dialog = await DialogService.ShowAsync<HomeItemView>("Item", parameters, LyoDialogPresets.Medium);
        var result = await dialog.Result;
        if (result is { Canceled: false })
            await LoadItems();
    }
}
