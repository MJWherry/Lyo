using Lyo.Api.Client;
using Lyo.Api.Models.Builders;
using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Query.Models.Common.Request;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lyo.Billing.Web.Components.Components.Forms;

public partial class ClientEditor
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    public Guid? ClientId { get; set; }

    private readonly ClientEditModel _edit = new();
    private bool _formHasChanges;
    private bool _saving;
    private bool _seeded;
    private Guid? _savedId;

    private bool _isNew => _savedId is not { } id || id == Guid.Empty;

    private bool _canSave => !_saving && !string.IsNullOrWhiteSpace(_edit.DisplayName) && (_isNew || _formHasChanges);

    protected override async Task OnParametersSetAsync()
    {
        if (_seeded)
            return;
        _seeded = true;
        _savedId = ClientId is { } id && id != Guid.Empty ? id : null;
        if (_savedId is not { } clientId)
            return;

        var res = await ApiClient.QueryProjectAsync<ProjectedQueryRes<ClientLookup>>(
            "Billing/Clients",
            new ProjectionQueryReq {
                Keys = [[clientId]],
                Amount = 1,
                Select = [
                    "Id", "DisplayName", "LegalName", "Status", "TaxExempt", "DefaultCurrency", "PaymentTermsDays", "InvoicePrefix", "DefaultInvoiceNotes",
                    "ContactName", "Line1", "Line2", "City", "Region", "PostalCode", "Country"
                ]
            });
        var row = res?.Items?.FirstOrDefault();
        if (row == null)
            return;
        _edit.DisplayName = row.DisplayName ?? string.Empty;
        _edit.LegalName = row.LegalName;
        _edit.Status = (BillingClientStatus)row.Status;
        _edit.TaxExempt = row.TaxExempt;
        _edit.DefaultCurrency = string.IsNullOrWhiteSpace(row.DefaultCurrency) ? "USD" : row.DefaultCurrency;
        _edit.PaymentTermsDays = row.PaymentTermsDays <= 0 ? 30 : row.PaymentTermsDays;
        _edit.InvoicePrefix = row.InvoicePrefix;
        _edit.DefaultInvoiceNotes = row.DefaultInvoiceNotes;
        _edit.ContactName = row.ContactName;
        _edit.Line1 = row.Line1;
        _edit.Line2 = row.Line2;
        _edit.City = row.City;
        _edit.Region = row.Region;
        _edit.PostalCode = row.PostalCode;
        _edit.Country = row.Country;
    }

    private void OnStatusChanged(BillingClientStatus status)
    {
        _edit.Status = status;
        _formHasChanges = true;
    }

    private async Task SaveAsync()
    {
        _saving = true;
        try {
            if (_isNew) {
                var created = await ApiClient.PostAsAsync<object, BillingIdBody>("Billing/Clients", CreateBody());
                _savedId = created.Id;
                _formHasChanges = false;
                Snackbar.Add("Client created. Add contacts on the other tabs, then close.", Severity.Success);
            }
            else {
                var patch = new PatchRequestBuilder()
                    .WithKey(_savedId!.Value)
                    .SetProperty("DisplayName", _edit.DisplayName)
                    .SetProperty("LegalName", _edit.LegalName)
                    .SetProperty("Status", (int)_edit.Status)
                    .SetProperty("DefaultCurrency", _edit.DefaultCurrency)
                    .SetProperty("PaymentTermsDays", _edit.PaymentTermsDays)
                    .SetProperty("TaxExempt", _edit.TaxExempt)
                    .SetProperty("InvoicePrefix", _edit.InvoicePrefix)
                    .SetProperty("DefaultInvoiceNotes", _edit.DefaultInvoiceNotes)
                    .SetProperty("ContactName", _edit.ContactName)
                    .SetProperty("Line1", _edit.Line1)
                    .SetProperty("Line2", _edit.Line2)
                    .SetProperty("City", _edit.City)
                    .SetProperty("Region", _edit.Region)
                    .SetProperty("PostalCode", _edit.PostalCode)
                    .SetProperty("Country", _edit.Country);
                if (!string.IsNullOrWhiteSpace(_edit.TaxId))
                    patch.SetProperty("TaxId", _edit.TaxId);
                await ApiClient.PatchAsAsync<PatchRequest, PatchResult<object>>("Billing/Clients", patch.Build());
                _formHasChanges = false;
                MudDialog.Close(DialogResult.Ok(true));
            }
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _saving = false;
        }
    }

    private object CreateBody()
        => new {
            displayName = _edit.DisplayName,
            legalName = _edit.LegalName,
            status = (int)_edit.Status,
            defaultCurrency = _edit.DefaultCurrency,
            paymentTermsDays = _edit.PaymentTermsDays,
            taxExempt = _edit.TaxExempt,
            invoicePrefix = _edit.InvoicePrefix,
            defaultInvoiceNotes = _edit.DefaultInvoiceNotes,
            taxId = _edit.TaxId,
            contactName = _edit.ContactName,
            line1 = _edit.Line1,
            line2 = _edit.Line2,
            city = _edit.City,
            region = _edit.Region,
            postalCode = _edit.PostalCode,
            country = _edit.Country
        };

    public sealed class ClientEditModel
    {
        public string DisplayName { get; set; } = string.Empty;
        public string? LegalName { get; set; }
        public BillingClientStatus Status { get; set; } = BillingClientStatus.Active;
        public string DefaultCurrency { get; set; } = "USD";
        public int PaymentTermsDays { get; set; } = 30;
        public bool TaxExempt { get; set; }
        public string? InvoicePrefix { get; set; }
        public string? DefaultInvoiceNotes { get; set; }
        public string? TaxId { get; set; }
        public string? ContactName { get; set; }
        public string? Line1 { get; set; }
        public string? Line2 { get; set; }
        public string? City { get; set; }
        public string? Region { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
    }
}
