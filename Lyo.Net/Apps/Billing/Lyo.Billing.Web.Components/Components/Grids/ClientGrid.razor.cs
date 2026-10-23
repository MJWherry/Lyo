using Microsoft.AspNetCore.Components;

namespace Lyo.Billing.Web.Components.Components.Grids;

public partial class ClientGrid
{
    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [
        new("DisplayName", "Name"), FilterPropertyDefinition.FromEnum("Status", BillingColorHelper.EnumFilterValues<BillingClientStatus>()), new("DefaultCurrency", "Currency")
    ];

    private LyoDataGridProjected? _dataGrid;

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    public EventCallback<Guid> OnOpen { get; set; }

    private async Task OpenAsync(object? item)
    {
        if (!ProjectedValueHelper.TryGetGuid(ProjectedValueHelper.GetValue(item, "Id"), out var id))
            return;

        await OnOpen.InvokeAsync(id);
    }

    private async Task OpenCreateAsync()
    {
        var parameters = new DialogParameters<ClientEditor> { { d => d.ApiClient, ApiClient } };
        var dialog = await DialogService.ShowAsync<ClientEditor>("New client", parameters, LyoDialogPresets.Large);
        await dialog.Result;
        if (_dataGrid != null)
            await _dataGrid.RefreshData();
    }

    private async Task EditAsync(object? item)
    {
        if (!ProjectedValueHelper.TryGetGuid(ProjectedValueHelper.GetValue(item, "Id"), out var id))
            return;

        var parameters = new DialogParameters<ClientEditor> { { d => d.ApiClient, ApiClient }, { d => d.ClientId, id } };
        var dialog = await DialogService.ShowAsync<ClientEditor>("Edit client", parameters, LyoDialogPresets.Large);
        var result = await dialog.Result;
        if (result is { Canceled: false } && _dataGrid != null)
            await _dataGrid.RefreshData();
    }

    /// <summary>Reloads the grid.</summary>
    public Task RefreshAsync() => _dataGrid?.RefreshData() ?? Task.CompletedTask;
}