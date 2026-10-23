using Microsoft.AspNetCore.Components;

namespace Lyo.Billing.Web.Components.Components.Widgets;

public partial class KpiStrip
{
    private string? _error;
    private bool _loading = true;

    private BillingSummaryRecord? _summary;

    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

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