using Microsoft.AspNetCore.Components;

namespace Lyo.TestGateway.Components.TestGateway;

public partial class DriftWorkbench
{
    private string _baseRoute = "Drift";

    protected override void OnInitialized()
    {
        var baseUrl = (Configuration["DriftDashboard:BaseApiUrl"] ?? Configuration["ApiClient:BaseUrl"] ?? "http://localhost:5251").TrimEnd('/');
        _baseRoute = $"{baseUrl}/Drift";
    }
}
