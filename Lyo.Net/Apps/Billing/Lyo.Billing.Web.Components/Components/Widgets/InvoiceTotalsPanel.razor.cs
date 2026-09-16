using Microsoft.AspNetCore.Components;

namespace Lyo.Billing.Web.Components.Components.Widgets;

public partial class InvoiceTotalsPanel
{
    [Parameter]
    [EditorRequired]
    public InvoiceRecord Invoice { get; set; } = null!;
}
