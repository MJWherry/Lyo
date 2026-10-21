using Microsoft.AspNetCore.Components;

namespace Lyo.Billing.Web.Components.Views;

public partial class ClientDetailView
{
    private ClientLookup? _client;
    private int _linksVersion;

    private string _name = "Client";

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    public Guid ClientId { get; set; }

    [Parameter]
    public EventCallback OnBack { get; set; }

    [Parameter]
    public EventCallback<Guid> OnCompose { get; set; }

    protected override async Task OnParametersSetAsync()
    {
        var res = await ApiClient.QueryProjectAsync<ProjectedQueryRes<ClientLookup>>(
            "Billing/Clients",
            new() {
                Keys = [[ClientId]],
                Amount = 1,
                Select = [
                    "Id", "DisplayName", "LegalName", "Status", "TaxExempt", "DefaultCurrency", "PaymentTermsDays", "InvoicePrefix", "DefaultInvoiceNotes", "ContactName",
                    "Line1", "Line2", "City", "Region", "PostalCode", "Country"
                ]
            });

        _client = res?.Items?.FirstOrDefault();
        _name = _client?.DisplayName ?? "Client";
    }

    private async Task EditAsync()
    {
        var parameters = new DialogParameters<ClientEditor> { { d => d.ApiClient, ApiClient }, { d => d.ClientId, ClientId } };
        var dialog = await DialogService.ShowAsync<ClientEditor>("Edit client", parameters, LyoDialogPresets.Large);
        await dialog.Result;
        _linksVersion++;
        await OnParametersSetAsync();
    }
}