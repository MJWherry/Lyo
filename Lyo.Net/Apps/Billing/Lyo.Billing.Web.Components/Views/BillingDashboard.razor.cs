using Microsoft.AspNetCore.Components;

namespace Lyo.Billing.Web.Components.Views;

public partial class BillingDashboard
{
    private Guid? _clientId;
    private InvoiceListView? _invoiceList;
    private KpiStrip? _kpis;
    private InvoiceGrid? _overviewInvoices;

    private int _tabIndex;

    [Inject]
    private IApiClient ApiClient { get; set; } = null!;

    [Inject]
    private IDialogService DialogService { get; set; } = null!;

    private Task ComposeAsync() => ShowComposerAsync(null);

    private Task ComposeForClient(Guid clientId) => ShowComposerAsync(clientId);

    private async Task ShowComposerAsync(Guid? clientId)
    {
        var parameters = new DialogParameters<InvoiceComposerView> { { d => d.ApiClient, ApiClient }, { d => d.ClientId, clientId } };
        var dialog = await DialogService.ShowAsync<InvoiceComposerView>("Compose invoice", parameters, LyoDialogPresets.Large);
        var result = await dialog.Result;
        if (result is { Canceled: false, Data: Guid invoiceId })
            await ShowInvoiceAsync(invoiceId);

        await RefreshInvoiceSurfacesAsync();
    }

    private async Task ShowInvoiceAsync(Guid invoiceId)
    {
        var parameters = new DialogParameters<InvoiceDetailView> { { d => d.ApiClient, ApiClient }, { d => d.InvoiceId, invoiceId } };
        var dialog = await DialogService.ShowAsync<InvoiceDetailView>("Invoice", parameters, LyoDialogPresets.Large);
        await dialog.Result;
        await RefreshInvoiceSurfacesAsync();
    }

    private async Task RefreshInvoiceSurfacesAsync()
    {
        if (_overviewInvoices != null)
            await _overviewInvoices.RefreshAsync();

        if (_invoiceList != null)
            await _invoiceList.RefreshAsync();

        if (_kpis != null)
            await _kpis.LoadAsync();
    }
}