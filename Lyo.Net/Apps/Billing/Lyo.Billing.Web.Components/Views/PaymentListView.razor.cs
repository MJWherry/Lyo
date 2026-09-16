using Lyo.Api.Client;
using Microsoft.AspNetCore.Components;

namespace Lyo.Billing.Web.Components.Views;

public partial class PaymentListView
{
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;
}
