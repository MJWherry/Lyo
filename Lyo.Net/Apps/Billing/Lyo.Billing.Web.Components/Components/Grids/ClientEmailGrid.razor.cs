using Microsoft.AspNetCore.Components;

namespace Lyo.Billing.Web.Components.Components.Grids;

public partial class ClientEmailGrid
{
    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("SubjectEntityType", "Type")];
    private bool _busy;

    private LyoDataGridProjected? _dataGrid;
    private string _email = string.Empty;
    private bool _isPrimary = true;
    private BillingEmailUsage _usage = BillingEmailUsage.Billing;

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    public Guid? ClientId { get; set; }

    [Parameter]
    public string GridKey { get; set; } = "BillingClientEmailGrid";

    private void ApplyQuery(ProjectionQueryReqBuilder q)
    {
        if (ClientId is { } clientId)
            q.AddWhere(new ConditionClause("ClientId", ComparisonOperatorEnum.Equals, clientId));
    }

    private async Task AddLiteAsync()
    {
        if (ClientId is not { } clientId || string.IsNullOrWhiteSpace(_email))
            return;

        _busy = true;
        try {
            var payload = await ApiClient.PostAsAsync<object, BillingIdBody>("Billing/EmailAddresses", new { email = _email.Trim() });
            await ApiClient.PostAsAsync<object, object>(
                $"Billing/Clients/{clientId:D}/emails",
                new { email = new { entityType = BillingEntityTypes.BillingEmailAddress, entityId = payload.Id.ToString("D") }, usage = (int)_usage, isPrimary = _isPrimary });

            _email = string.Empty;
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

        if (!await DialogService.ConfirmDeleteAsync("this email", "The link is removed. The email payload is kept."))
            return;

        try {
            await ApiClient.DeleteAsAsync<object>($"Billing/Emails/{id:D}");
            if (_dataGrid != null)
                await _dataGrid.RefreshData();
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }
}