using Microsoft.AspNetCore.Components;

namespace Lyo.TestGateway.Components.TestGateway;

public partial class JobWorkbench
{
    private string _baseRoute = "Job";
    private string? _statsRoute;

    protected override void OnInitialized()
    {
        var baseUrl = (Configuration["JobDashboard:BaseApiUrl"] ?? Configuration["ApiClient:BaseUrl"] ?? "http://localhost:5251").TrimEnd('/');
        _baseRoute = $"{baseUrl}/Job";
        _statsRoute = Configuration["JobDashboard:StatisticsApiUrl"];
    }
}
