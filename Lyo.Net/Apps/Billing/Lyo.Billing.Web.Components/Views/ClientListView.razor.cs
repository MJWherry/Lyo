using Lyo.Api.Client;
using Microsoft.AspNetCore.Components;

namespace Lyo.Billing.Web.Components.Views;

public partial class ClientListView
{
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    public EventCallback<Guid> OnOpen { get; set; }
}
