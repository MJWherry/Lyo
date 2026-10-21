using Microsoft.AspNetCore.Components;

namespace Lyo.TestGateway.Components.Pages;

public partial class WorkflowBuilderExample
{
    private string _baseRoute = "Job";

    [Inject]
    private IConfiguration Configuration { get; set; } = null!;

    protected override string PageName { get; set; } = "Workflow Builder Example";

    protected override void OnParametersSet()
    {
        var baseUrl = (Configuration["JobDashboard:BaseApiUrl"] ?? Configuration["ApiClient:BaseUrl"] ?? "http://localhost:5251").TrimEnd('/');
        _baseRoute = $"{baseUrl}/Job";
    }
}