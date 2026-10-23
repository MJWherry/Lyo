using Microsoft.AspNetCore.Components;

namespace Lyo.Billing.Web.Components.Views;

public partial class InvoiceDetailView
{
    private bool _busy;

    private InvoiceRecord? _invoice;

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>Invoice to load and display.</summary>
    [Parameter]
    public Guid InvoiceId { get; set; }

    protected override Task OnParametersSetAsync() => ReloadAsync();

    private async Task ReloadAsync() => _invoice = await Billing.GetInvoiceAsync(InvoiceId);

    private async Task PayAsync()
    {
        if (_invoice == null)
            return;

        var remaining = _invoice.Total - _invoice.AmountPaid;
        var parameters = new DialogParameters<PaymentEditor> { { d => d.Amount, remaining }, { d => d.Method, PaymentMethod.Internal } };
        var dialog = await DialogService.ShowAsync<PaymentEditor>("Record payment", parameters, LyoDialogPresets.Small);
        var result = await dialog.Result;
        if (result is { Canceled: false, Data: PaymentEditor.PaymentEditModel pay }) {
            _busy = true;
            try {
                if (pay.Method == PaymentMethod.Internal) {
                    var collected = await Collector.CollectAsync(new() { InvoiceId = _invoice.Id, Amount = pay.Amount, Currency = _invoice.Currency });
                    if (!collected.Succeeded)
                        throw new InvalidOperationException(collected.FailureReason ?? "Collection failed.");

                    pay.Reference = collected.Reference;
                }

                await Billing.PayAsync(
                    _invoice.Id, new() {
                        Amount = pay.Amount,
                        Currency = _invoice.Currency,
                        Method = pay.Method,
                        Reference = pay.Reference
                    });

                Snackbar.Add("Payment recorded.", Severity.Success);
                await ReloadAsync();
            }
            catch (Exception ex) {
                Snackbar.Add(ex.Message, Severity.Error);
            }
            finally {
                _busy = false;
            }
        }
    }

    private async Task VoidAsync()
    {
        if (_invoice == null)
            return;

        if (!await DialogService.ConfirmAsync("Void invoice", $"Void {_invoice.Number ?? "this invoice"}? This posts a reversing journal.", "Void"))
            return;

        _busy = true;
        try {
            await Billing.VoidAsync(_invoice.Id);
            Snackbar.Add("Invoice voided.", Severity.Info);
            MudDialog.Close(DialogResult.Ok(_invoice.Id));
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task DeleteDraftAsync()
    {
        if (_invoice == null)
            return;

        if (!await DialogService.ConfirmDeleteAsync("this draft invoice", "Reserved charges are unbilled again."))
            return;

        _busy = true;
        try {
            await Billing.DeleteDraftAsync(_invoice.Id);
            Snackbar.Add("Draft deleted.", Severity.Info);
            MudDialog.Close(DialogResult.Ok(_invoice.Id));
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }
}