using Lyo.HomeInventory.Models.Enums;
using Lyo.HomeInventory.Models.Request;
using Lyo.HomeInventory.Models.Response;

namespace Lyo.HomeInventory.Web.Components;

/// <summary>On-hand adjust/transfer for one item.</summary>
public partial class HomeStockPanel
{
    /// <summary>HTTP client.</summary>
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>HomeInventory route prefix.</summary>
    [Parameter]
    [EditorRequired]
    public string BaseRoute { get; set; } = "HomeInventory";

    /// <summary>Item whose stock is shown.</summary>
    [Parameter]
    public Guid ItemId { get; set; }

    /// <summary>Locations for pickers.</summary>
    [Parameter]
    public IReadOnlyList<HomeLocationRes> Locations { get; set; } = [];

    private List<HomeItemStockRes> _stock = [];
    private Guid _locationId;
    private Guid _toLocationId;
    private decimal _delta = 1;

    protected override async Task OnParametersSetAsync()
    {
        if (ItemId == Guid.Empty)
            return;
        if (_locationId == Guid.Empty && Locations.Count > 0)
            _locationId = Locations[0].Id;
        if (_toLocationId == Guid.Empty && Locations.Count > 1)
            _toLocationId = Locations[1].Id;
        await Reload();
    }

    private string LocationName(Guid id) => Locations.FirstOrDefault(l => l.Id == id)?.Name ?? id.ToString("D")[..8];

    private async Task Reload()
    {
        var rows = await ApiClient.GetAsAsync<List<HomeItemStockRes>>($"{BaseRoute.TrimEnd('/')}/Items/{ItemId}/Stock");
        _stock = rows ?? [];
    }

    private async Task Adjust()
    {
        await ApiClient.PostAsAsync<HomeItemStockAdjustReq, HomeItemStockRes>(
            $"{BaseRoute.TrimEnd('/')}/Items/{ItemId}/Stock/Adjust",
            new() { LocationId = _locationId, QuantityDelta = _delta, MovementType = HomeItemMovementType.Adjustment });
        await Reload();
    }

    private async Task Transfer()
    {
        await ApiClient.PostAsAsync<HomeItemStockTransferReq, List<HomeItemStockRes>>(
            $"{BaseRoute.TrimEnd('/')}/Items/{ItemId}/Stock/Transfer",
            new() { FromLocationId = _locationId, ToLocationId = _toLocationId, Quantity = Math.Abs(_delta) });
        await Reload();
    }
}
