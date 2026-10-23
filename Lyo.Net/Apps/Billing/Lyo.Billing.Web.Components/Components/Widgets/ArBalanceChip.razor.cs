using Microsoft.AspNetCore.Components;

namespace Lyo.Billing.Web.Components.Components.Widgets;

public partial class ArBalanceChip
{
    private decimal _balance;
    private bool _loading = true;

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    [Parameter]
    public Guid ClientId { get; set; }

    [Parameter]
    public string Currency { get; set; } = "USD";

    protected override async Task OnParametersSetAsync()
    {
        if (ClientId == Guid.Empty) {
            _balance = 0;
            _loading = false;
            return;
        }

        _loading = true;
        try {
            _balance = await ApiClient.GetAsAsync<decimal>($"Ledger/Clients/{ClientId:D}/ar-balance");
        }
        catch {
            _balance = 0;
        }
        finally {
            _loading = false;
        }
    }
}