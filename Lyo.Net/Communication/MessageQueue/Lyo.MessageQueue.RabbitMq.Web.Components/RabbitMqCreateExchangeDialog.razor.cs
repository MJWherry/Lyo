using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lyo.MessageQueue.RabbitMq.Web.Components;

public partial class RabbitMqCreateExchangeDialog
{
    private readonly RabbitMqCreateExchangeRequest _request = new();

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    private bool CanSave => !string.IsNullOrWhiteSpace(_request.Name);

    private void Save()
    {
        if (!CanSave) {
            Snackbar.Add("Enter an exchange name.", Severity.Warning);
            return;
        }

        MudDialog.Close(DialogResult.Ok(_request));
    }
}