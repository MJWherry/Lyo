using Microsoft.AspNetCore.Components;

namespace Lyo.Billing.Web.Components.Views;

public partial class ChargeListView
{
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;
}