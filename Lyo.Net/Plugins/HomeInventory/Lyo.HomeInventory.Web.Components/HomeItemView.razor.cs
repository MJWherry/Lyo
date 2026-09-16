using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.HomeInventory.Models;
using Lyo.HomeInventory.Models.Enums;
using Lyo.Query.Models.Common.Request;

namespace Lyo.HomeInventory.Web.Components;

/// <summary>Create/edit dialog for a household item, including stock, notes, tags, and attachments.</summary>
public partial class HomeItemView
{
    private const string CustomKindChoice = "__custom__";

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    /// <summary>HTTP client.</summary>
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>HomeInventory route prefix.</summary>
    [Parameter]
    [EditorRequired]
    public string BaseRoute { get; set; } = "HomeInventory";

    /// <summary>FileStorage MapGroup prefix.</summary>
    [Parameter]
    public string FileStorageApiRoutePrefix { get; set; } = "FileStorage";

    /// <summary>Existing item when editing; null for create.</summary>
    [Parameter]
    public HomeItemRes? Item { get; set; }

    private readonly HomeItemReq _req = new();
    private Guid? _savedId;
    private IReadOnlyList<HomeLocationRes> _locations = [];
    private IReadOnlyList<HomeCategoryRes> _categories = [];
    private string _draftKind = HomeProductKeys.Sku;
    private string _customKind = "";
    private string _draftValue = "";
    private string _draftNote = "";

    private string _itemsRoute => $"{BaseRoute.TrimEnd('/')}/Items";

    private string _attachmentsRoute => $"{BaseRoute.TrimEnd('/')}/Attachments";

    private bool IsCustomKind => _draftKind == CustomKindChoice;

    private bool CanAddKey
        => !string.IsNullOrWhiteSpace(_draftValue)
           && (IsCustomKind ? !string.IsNullOrWhiteSpace(_customKind) : !string.IsNullOrWhiteSpace(_draftKind));

    private string ApiOrigin
    {
        get
        {
            if (Uri.TryCreate(BaseRoute, UriKind.Absolute, out var uri))
                return uri.GetLeftPart(UriPartial.Authority);
            return "";
        }
    }

    protected override async Task OnInitializedAsync()
    {
        if (Item != null) {
            _savedId = Item.Id;
            _req.Name = Item.Name;
            _req.Description = Item.Description;
            _req.Status = Item.Status;
            _req.Condition = Item.Condition;
            _req.LocationId = Item.LocationId;
            _req.Owner = Item.Owner;
            _req.CategoryId = Item.CategoryId;
            _req.ParentItemId = Item.ParentItemId;
            _req.ProductKeys = Item.ProductKeys.Select(k => new HomeProductKeyEntry { Kind = k.Kind, Value = k.Value, Note = k.Note }).ToList();
        }

        var locations = await ApiClient.QueryConcreteAsync<QueryRes<HomeLocationRes>>($"{BaseRoute.TrimEnd('/')}/Locations", new QueryConcreteReq { Amount = 500 });
        _locations = locations?.Items ?? [];
        var categories = await ApiClient.QueryConcreteAsync<QueryRes<HomeCategoryRes>>($"{BaseRoute.TrimEnd('/')}/Categories", new QueryConcreteReq { Amount = 500 });
        _categories = categories?.Items ?? [];
    }

    private void AddProductKey()
    {
        if (!CanAddKey)
            return;

        var kind = IsCustomKind ? _customKind.Trim() : _draftKind;
        _req.ProductKeys.Add(new HomeProductKeyEntry {
            Kind = kind,
            Value = _draftValue.Trim(),
            Note = string.IsNullOrWhiteSpace(_draftNote) ? null : _draftNote.Trim()
        });
        _draftValue = "";
        _draftNote = "";
        if (IsCustomKind)
            _customKind = "";
    }

    private void RemoveProductKey(HomeProductKeyEntry entry) => _req.ProductKeys.Remove(entry);

    private void Cancel() => MudDialog.Close(DialogResult.Ok(true));

    private async Task Save()
    {
        if (_savedId is Guid id) {
            await ApiClient.PostAsAsync<UpdateRequest<HomeItemReq>, UpdateResult<HomeItemRes>>($"{_itemsRoute}/Update", new(_req, id));
        }
        else {
            var created = await ApiClient.PostAsAsync<HomeItemReq, CreateResult<HomeItemRes>>(_itemsRoute, _req);
            _savedId = created.Data?.Id;
        }

        await InvokeAsync(StateHasChanged);
    }
}
