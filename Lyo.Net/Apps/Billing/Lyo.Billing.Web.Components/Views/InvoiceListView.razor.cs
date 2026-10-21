using Microsoft.AspNetCore.Components;

namespace Lyo.Billing.Web.Components.Views;

public partial class InvoiceListView
{
    private InvoiceGrid? _grid;

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    public EventCallback OnCompose { get; set; }

    /// <summary>Reloads the invoice grid.</summary>
    public Task RefreshAsync() => _grid?.RefreshAsync() ?? Task.CompletedTask;
}