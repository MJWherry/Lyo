using Microsoft.AspNetCore.Components;

namespace Lyo.Billing.Web.Components.Components.Grids;

public partial class ClientContactGrid
{
    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("SubjectEntityType", "Type"), new("Title")];

    private bool _busy;

    private LyoDataGridProjected? _dataGrid;
    private string _displayName = string.Empty;
    private bool _isBilling = true;
    private bool _isPrimary = true;
    private BillingContactRole _role = BillingContactRole.Billing;
    private string? _title;

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    public Guid? ClientId { get; set; }

    [Parameter]
    public string GridKey { get; set; } = "BillingClientContactGrid";

    private void ApplyQuery(ProjectionQueryReqBuilder q)
    {
        if (ClientId is { } clientId)
            q.AddWhere(new ConditionClause("ClientId", ComparisonOperatorEnum.Equals, clientId));
    }

    private async Task AddLiteAsync()
    {
        if (ClientId is not { } clientId || string.IsNullOrWhiteSpace(_displayName))
            return;

        _busy = true;
        try {
            var person = await ApiClient.PostAsAsync<object, BillingIdBody>("Billing/People", new { displayName = _displayName.Trim() });
            await ApiClient.PostAsAsync<object, object>(
                $"Billing/Clients/{clientId:D}/contacts",
                new {
                    contact = new { entityType = BillingEntityTypes.BillingPerson, entityId = person.Id.ToString("D") },
                    role = (int)_role,
                    title = _title,
                    isPrimary = _isPrimary,
                    isBillingContact = _isBilling
                });

            _displayName = string.Empty;
            _title = null;
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

        if (!await DialogService.ConfirmDeleteAsync("this contact", "The link is removed. The person payload is kept."))
            return;

        try {
            await ApiClient.DeleteAsAsync<object>($"Billing/Contacts/{id:D}");
            if (_dataGrid != null)
                await _dataGrid.RefreshData();
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
    }
}