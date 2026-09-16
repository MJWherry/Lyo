using Lyo.Api.Client;
using Lyo.Query.Models.Builders;
using Lyo.Query.Models.Common;
using Lyo.Query.Models.Enums;
using Lyo.Web.Components.DataGrid;
using Lyo.Web.Components.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lyo.Billing.Web.Components.Components.Grids;

public partial class ClientAddressGrid
{
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    public Guid? ClientId { get; set; }

    [Parameter]
    public string GridKey { get; set; } = "BillingClientAddressGrid";

    private LyoDataGridProjected? _dataGrid;
    private string _line1 = string.Empty;
    private string? _city;
    private string? _region;
    private string? _postal;
    private BillingAddressUsage _usage = BillingAddressUsage.Billing;
    private bool _isPrimary = true;
    private bool _busy;

    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("SubjectEntityType", "Type")];

    private void ApplyQuery(ProjectionQueryReqBuilder q)
    {
        if (ClientId is { } clientId)
            q.AddWhere(new ConditionClause("ClientId", ComparisonOperatorEnum.Equals, clientId));
    }

    private async Task AddLiteAsync()
    {
        if (ClientId is not { } clientId || string.IsNullOrWhiteSpace(_line1))
            return;
        _busy = true;
        try {
            var payload = await ApiClient.PostAsAsync<object, BillingIdBody>(
                "Billing/AddressPayloads",
                new { line1 = _line1.Trim(), city = _city, region = _region, postalCode = _postal, country = "US" });
            await ApiClient.PostAsAsync<object, object>(
                $"Billing/Clients/{clientId:D}/addresses",
                new {
                    address = new { entityType = BillingEntityTypes.BillingAddress, entityId = payload.Id.ToString("D") },
                    usage = (int)_usage,
                    isPrimary = _isPrimary
                });
            _line1 = string.Empty;
            _city = _region = _postal = null;
            if (_dataGrid != null)
                await _dataGrid.RefreshData();
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task DetachAsync(object? item)
    {
        if (!ProjectedValueHelper.TryGetGuid(ProjectedValueHelper.GetValue(item, "Id"), out var id))
            return;
        if (!await DialogService.ConfirmDeleteAsync("this address", "The link is removed. The address payload is kept."))
            return;
        try {
            await ApiClient.DeleteAsAsync<object>($"Billing/Addresses/{id:D}");
            if (_dataGrid != null)
                await _dataGrid.RefreshData();
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }
}
