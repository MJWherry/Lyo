using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Lyo.Billing.Web.Components.Components.Grids;

public partial class ChargeGrid
{
    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [
        new("Description"), FilterPropertyDefinition.FromEnum("Kind", BillingColorHelper.EnumFilterValues<ChargeKind>())
    ];

    private LyoDataGridProjected? _dataGrid;

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    public Guid? ClientId { get; set; }

    private void ApplyQuery(ProjectionQueryReqBuilder q)
    {
        if (ClientId is { } clientId)
            q.AddWhere(new ConditionClause("ClientId", ComparisonOperatorEnum.Equals, clientId));
    }

    private async Task OpenCreateAsync()
    {
        var parameters = new DialogParameters<ChargeEditor> { { d => d.ApiClient, ApiClient }, { d => d.ClientId, ClientId } };
        var dialog = await DialogService.ShowAsync<ChargeEditor>("New charge", parameters, LyoDialogPresets.Medium);
        var result = await dialog.Result;
        if (result is { Canceled: false } && _dataGrid != null)
            await _dataGrid.RefreshData();
    }

    private async Task EditAsync(object? item)
    {
        if (!ProjectedValueHelper.TryGetGuid(ProjectedValueHelper.GetValue(item, "Id"), out var id))
            return;

        ProjectedValueHelper.TryGetGuid(ProjectedValueHelper.GetValue(item, "ClientId"), out var clientId);
        var parameters = new DialogParameters<ChargeEditor> {
            { d => d.ApiClient, ApiClient },
            { d => d.ChargeId, id },
            { d => d.ClientId, clientId },
            { d => d.Description, ProjectedValueHelper.GetDisplayValue(item, "Description") },
            { d => d.Quantity, ParseDecimal(ProjectedValueHelper.GetValue(item, "Quantity"), 1m) },
            { d => d.UnitAmount, ParseDecimal(ProjectedValueHelper.GetValue(item, "UnitAmount"), 0m) },
            { d => d.Locked, ProjectedValueHelper.GetValue(item, "InvoiceId") != null }
        };

        var dialog = await DialogService.ShowAsync<ChargeEditor>("Edit charge", parameters, LyoDialogPresets.Medium);
        var result = await dialog.Result;
        if (result is { Canceled: false } && _dataGrid != null)
            await _dataGrid.RefreshData();
    }

    private static decimal ParseDecimal(object? value, decimal fallback) => decimal.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var d) ? d : fallback;

    /// <summary>Reloads the grid.</summary>
    public Task RefreshAsync() => _dataGrid?.RefreshData() ?? Task.CompletedTask;
}