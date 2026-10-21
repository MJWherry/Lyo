using Microsoft.AspNetCore.Components;

namespace Lyo.Billing.Web.Components.Components.Forms;

public partial class ChargeEditor
{
    private readonly ChargeEditModel _edit = new();
    private bool _formHasChanges;
    private bool _saving;
    private bool _seeded;

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    public Guid? ChargeId { get; set; }

    [Parameter]
    public Guid? ClientId { get; set; }

    [Parameter]
    public string? Description { get; set; }

    [Parameter]
    public decimal Quantity { get; set; } = 1m;

    [Parameter]
    public decimal UnitAmount { get; set; }

    [Parameter]
    public bool Locked { get; set; }

    private bool _isNew => ChargeId is not { } id || id == Guid.Empty;

    private bool _canSave => !Locked && !_saving && _edit.ClientId != Guid.Empty && !string.IsNullOrWhiteSpace(_edit.Description) && (_isNew || _formHasChanges);

    protected override void OnParametersSet()
    {
        if (_seeded)
            return;

        _edit.ClientId = ClientId ?? Guid.Empty;
        _edit.Description = Description ?? string.Empty;
        _edit.Quantity = Quantity <= 0 ? 1m : Quantity;
        _edit.UnitAmount = UnitAmount;
        _edit.Currency = "USD";
        _seeded = true;
    }

    private Task OnClientPicked(Guid? id)
    {
        _edit.ClientId = id ?? Guid.Empty;
        _formHasChanges = true;
        return Task.CompletedTask;
    }

    private async Task SaveAsync()
    {
        if (Locked)
            return;

        _saving = true;
        try {
            if (_isNew) {
                await ApiClient.PostAsAsync<object, BillingIdBody>(
                    "Billing/Charges", new {
                        clientId = _edit.ClientId,
                        description = _edit.Description,
                        quantity = _edit.Quantity,
                        unitAmount = _edit.UnitAmount,
                        currency = _edit.Currency,
                        kind = (int)ChargeKind.Time
                    });
            }
            else {
                var patch = new PatchRequestBuilder().WithKey(ChargeId!.Value)
                    .SetProperty("Description", _edit.Description)
                    .SetProperty("Quantity", _edit.Quantity)
                    .SetProperty("UnitAmount", _edit.UnitAmount)
                    .Build();

                await ApiClient.PatchAsAsync<PatchRequest, PatchResult<object>>("Billing/Charges", patch);
            }

            MudDialog.Close(DialogResult.Ok(true));
        }
        finally {
            _saving = false;
        }
    }

    public sealed class ChargeEditModel
    {
        public Guid ClientId { get; set; }

        public string Description { get; set; } = string.Empty;

        public decimal Quantity { get; set; } = 1m;

        public decimal UnitAmount { get; set; }

        public string Currency { get; set; } = "USD";
    }
}