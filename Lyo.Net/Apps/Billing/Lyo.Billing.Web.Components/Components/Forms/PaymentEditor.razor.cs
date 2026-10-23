using Microsoft.AspNetCore.Components;

namespace Lyo.Billing.Web.Components.Components.Forms;

public partial class PaymentEditor
{
    private readonly PaymentEditModel _edit = new();
    private bool _formHasChanges = true;
    private bool _saving;
    private bool _seeded;

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public decimal Amount { get; set; }

    [Parameter]
    public PaymentMethod Method { get; set; } = PaymentMethod.Internal;

    [Parameter]
    public string? Reference { get; set; }

    private bool _canSave => !_saving && _edit.Amount > 0;

    protected override void OnParametersSet()
    {
        if (_seeded)
            return;

        _edit.Amount = Amount;
        _edit.Method = Method;
        _edit.Reference = Reference;
        _seeded = true;
    }

    private Task SaveAsync()
    {
        _saving = true;
        MudDialog.Close(DialogResult.Ok(_edit));
        return Task.CompletedTask;
    }

    public sealed class PaymentEditModel
    {
        public decimal Amount { get; set; }

        public PaymentMethod Method { get; set; } = PaymentMethod.Internal;

        public string? Reference { get; set; }
    }
}