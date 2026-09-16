using Lyo.Api.Client;
using Microsoft.AspNetCore.Components;

namespace Lyo.Billing.Web.Components.Views;

public partial class InvoiceListView
{
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    public EventCallback OnCompose { get; set; }

    private InvoiceGrid? _grid;

    /// <summary>Reloads the invoice grid.</summary>
    public Task RefreshAsync() => _grid?.RefreshAsync() ?? Task.CompletedTask;
}
