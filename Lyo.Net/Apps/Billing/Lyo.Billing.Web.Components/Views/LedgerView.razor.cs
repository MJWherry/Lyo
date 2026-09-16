using Lyo.Api.Client;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lyo.Billing.Web.Components.Views;

public partial class LedgerView
{
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    private bool _busy;

    private async Task EnsureChartAsync()
    {
        _busy = true;
        try {
            var result = await ApiClient.Post("Ledger/ChartOfAccounts/ensure").RunAsync();
            if (!result.Success)
                throw new InvalidOperationException(result.Error ?? "Could not ensure the chart of accounts.");
            Snackbar.Add("Chart of accounts is ready.", Severity.Success);
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
        }
        finally {
            _busy = false;
        }
    }
}
