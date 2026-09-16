using Lyo.Api.Client;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lyo.Billing.Web.Components.Views;

public partial class InvoiceComposerView
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    public Guid? ClientId { get; set; }

    [Parameter]
    public EventCallback OnCancel { get; set; }

    [Parameter]
    public EventCallback<Guid> OnIssued { get; set; }

    private Guid? _clientId;
    private decimal _taxRate = 0.08m;
    private InvoiceRecord? _invoice;
    private List<ChargeRecord> _unbilled = [];
    private bool _busy;
    private bool _seeded;

    protected override void OnParametersSet()
    {
        if (_seeded)
            return;
        _clientId = ClientId;
        _seeded = true;
    }

    private async Task OnClientChanged(Guid? id)
    {
        _clientId = id;
        if (id is { } clientId)
            await LoadUnbilledAsync(clientId);
    }

    private async Task StartDraftAsync()
    {
        if (_clientId is not { } clientId)
            return;
        _busy = true;
        try {
            _invoice = await ApiClient.PostAsAsync<object, InvoiceRecord>("Billing/Invoices/draft", new { clientId, taxRate = _taxRate });
            await LoadUnbilledAsync(clientId);
            Snackbar.Add("Draft created.", Severity.Success);
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task AddLineAsync(InvoiceLineRecord line)
    {
        if (_invoice == null)
            return;
        _busy = true;
        try {
            await ApiClient.PostAsAsync<InvoiceLineRecord, InvoiceLineRecord>($"Billing/Invoices/{_invoice.Id}/lines", line);
            await ReloadInvoiceAsync();
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private Task AddChargeAsync(ChargeRecord charge)
        => AddLineAsync(new InvoiceLineRecord { ChargeId = charge.Id });

    private async Task RemoveLineAsync(InvoiceLineRecord line)
    {
        if (_invoice == null)
            return;
        _busy = true;
        try {
            await ApiClient.DeleteAsAsync<object>($"Billing/Invoices/{_invoice.Id}/lines/{line.Id}");
            await ReloadInvoiceAsync();
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task IssueAsync()
    {
        if (_invoice == null)
            return;
        _busy = true;
        try {
            _invoice = await ApiClient.PostAsAsync<InvoiceRecord>($"Billing/Invoices/{_invoice.Id}/issue");
            Snackbar.Add($"Issued {_invoice.Number}.", Severity.Success);
            await OnIssued.InvokeAsync(_invoice.Id);
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
            await ApiClient.DeleteAsAsync<object>($"Billing/Invoices/{_invoice.Id}/draft");
            Snackbar.Add("Draft deleted.", Severity.Info);
            await OnCancel.InvokeAsync();
            MudDialog.Cancel();
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }

    private async Task ReloadInvoiceAsync()
    {
        if (_invoice == null)
            return;
        _invoice = await ApiClient.GetAsAsync<InvoiceRecord>($"Billing/Invoices/{_invoice.Id}/record");
        if (_clientId is { } clientId)
            await LoadUnbilledAsync(clientId);
    }

    private async Task LoadUnbilledAsync(Guid clientId)
        => _unbilled = await ApiClient.GetAsAsync<List<ChargeRecord>>($"Billing/Clients/{clientId:D}/unbilled-charges") ?? [];
}
