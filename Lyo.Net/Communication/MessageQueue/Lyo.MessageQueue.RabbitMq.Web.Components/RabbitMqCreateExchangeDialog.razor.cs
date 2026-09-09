using System.Net.Http.Json;
using Lyo.Web.Components;
using Lyo.Web.Primitives;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace Lyo.MessageQueue.RabbitMq.Web.Components;

public partial class RabbitMqCreateExchangeDialog
{
    [CascadingParameter]
    IMudDialogInstance MudDialog { get; set; } = null!;

    private readonly RabbitMqCreateExchangeRequest _request = new();

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
