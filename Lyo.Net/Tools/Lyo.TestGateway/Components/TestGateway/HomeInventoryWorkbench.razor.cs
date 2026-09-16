using Microsoft.AspNetCore.Components;

namespace Lyo.TestGateway.Components.TestGateway;

public partial class HomeInventoryWorkbench
{
    private string _baseRoute = "HomeInventory";
    private string _fileStoragePrefix = Models.Constants.FileStorageWorkbench.ApiRoutePrefix;

    protected override void OnInitialized()
    {
        var baseUrl = (Configuration["ApiClient:BaseUrl"] ?? "http://localhost:5251").TrimEnd('/');
        _baseRoute = $"{baseUrl}/HomeInventory";
        _fileStoragePrefix = Models.Constants.FileStorageWorkbench.ApiRoutePrefix;
    }
}
