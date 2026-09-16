using Lyo.Api.Client;
using Lyo.Billing;
using Lyo.Billing.Client;
using Lyo.Payments;
using Lyo.Web.Components.Dialog;
using MudBlazor;

namespace Lyo.Billing.Web.Components;

/// <summary>Gateway workbench: demo invoice on TestApi, then internal collection + RecordPayment.</summary>
public partial class BillingWorkbench
{
    private string _displayName = "Acme";
    private decimal _hours = 10m;
    private decimal _rate = 150m;
    private decimal _taxRate = 0.08m;
    private bool _busy;
    private Guid? _invoiceId;
    private string? _status;

    private async Task RunDemoAsync()
    {
        _busy = true;
        try {
            var client = await ApiClient.PostAsAsync<object, IdBody>(
                "Billing/Clients",
                new {
                    displayName = _displayName,
                    invoicePrefix = "INV",
                    paymentTermsDays = 30,
                    defaultCurrency = "USD",
                    status = (int)BillingClientStatus.Active
                });
            if (client.Id == Guid.Empty)
                throw new InvalidOperationException("Client create did not return an id.");

            var charge = await ApiClient.PostAsAsync<object, IdBody>(
                "Billing/Charges",
                new { clientId = client.Id, description = "Professional services", quantity = _hours, unitAmount = _rate, currency = "USD", kind = (int)ChargeKind.Time });
            if (charge.Id == Guid.Empty)
                throw new InvalidOperationException("Charge create did not return an id.");

            var invoice = await Billing.CreateFromChargesAsync(
                new CreateInvoiceFromChargesRequest { ClientId = client.Id, ChargeIds = [charge.Id], TaxRate = _taxRate });
            invoice = await Billing.IssueAsync(invoice.Id);
            _invoiceId = invoice.Id;

            var collected = await Collector.CollectAsync(new PaymentCollectionRequest { InvoiceId = invoice.Id, Amount = invoice.Total, Currency = invoice.Currency });
            if (!collected.Succeeded)
                throw new InvalidOperationException(collected.FailureReason ?? "Collection failed.");

            await Billing.PayAsync(
                invoice.Id,
                new RecordInvoicePaymentRequest { Amount = invoice.Total, Currency = invoice.Currency, Method = PaymentMethod.Internal, Reference = collected.Reference });
            _status = $"Issued {invoice.Number} and collected {invoice.Total} {invoice.Currency} ({collected.Reference}).";
            Snackbar.Add(_status, Severity.Success);
        }
        catch (Exception ex) {
            _status = ex.Message;
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task ViewInvoiceAsync()
    {
        if (_invoiceId is not { } id)
            return;
        var parameters = new DialogParameters<InvoiceDetailView> {
            { d => d.ApiClient, ApiClient },
            { d => d.InvoiceId, id }
        };
        await DialogService.ShowAsync<InvoiceDetailView>("Invoice", parameters, LyoDialogPresets.Large);
    }

    private sealed class IdBody
    {
        public Guid Id { get; set; }
    }
}
