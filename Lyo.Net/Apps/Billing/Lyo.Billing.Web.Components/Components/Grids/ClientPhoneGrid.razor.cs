using Microsoft.AspNetCore.Components;

namespace Lyo.Billing.Web.Components.Components.Grids;

public partial class ClientPhoneGrid
{
    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("SubjectEntityType", "Type")];
    private bool _busy;

    private LyoDataGridProjected? _dataGrid;
    private bool _isPrimary = true;
    private string _number = string.Empty;
    private BillingPhoneUsage _usage = BillingPhoneUsage.Work;

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    public Guid? ClientId { get; set; }

    [Parameter]
    public string GridKey { get; set; } = "BillingClientPhoneGrid";

    private void ApplyQuery(ProjectionQueryReqBuilder q)
    {
        if (ClientId is { } clientId)
            q.AddWhere(new ConditionClause("ClientId", ComparisonOperatorEnum.Equals, clientId));
    }

    private async Task AddLiteAsync()
    {
        if (ClientId is not { } clientId || string.IsNullOrWhiteSpace(_number))
            return;

        _busy = true;
        try {
            var payload = await ApiClient.PostAsAsync<object, BillingIdBody>("Billing/PhoneNumbers", new { number = _number.Trim() });
            await ApiClient.PostAsAsync<object, object>(
                $"Billing/Clients/{clientId:D}/phones",
                new { phone = new { entityType = BillingEntityTypes.BillingPhoneNumber, entityId = payload.Id.ToString("D") }, usage = (int)_usage, isPrimary = _isPrimary });

            _number = string.Empty;
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

        if (!await DialogService.ConfirmDeleteAsync("this phone", "The link is removed. The phone payload is kept."))
            return;

        try {
            await ApiClient.DeleteAsAsync<object>($"Billing/Phones/{id:D}");
            if (_dataGrid != null)
                await _dataGrid.RefreshData();
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }
}