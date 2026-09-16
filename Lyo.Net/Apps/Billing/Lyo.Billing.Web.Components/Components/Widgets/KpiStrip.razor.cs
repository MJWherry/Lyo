using Lyo.Api.Client;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Lyo.Billing.Web.Components.Components.Widgets;

public partial class KpiStrip
{
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    private BillingSummaryRecord? _summary;
    private bool _loading = true;
    private string? _error;

    protected override Task OnInitializedAsync() => LoadAsync();

    /// <summary>Reloads <c>GET /Billing/Summary</c>.</summary>
    public async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try {
            _summary = await ApiClient.GetAsAsync<BillingSummaryRecord>("Billing/Summary");
        }
        catch (Exception ex) {
            _error = ex.Message;
        }
        finally {
            _loading = false;
        }
    }
}
