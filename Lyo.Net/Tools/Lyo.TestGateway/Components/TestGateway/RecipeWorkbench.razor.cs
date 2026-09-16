using Lyo.Api.Client;
using Lyo.Recipe.Client;
using Microsoft.AspNetCore.Components;

namespace Lyo.TestGateway.Components.TestGateway;

public partial class RecipeWorkbench
{
    private string _baseRoute = "Recipe";
    private string _fileStoragePrefix = Models.Constants.FileStorageWorkbench.ApiRoutePrefix;
    private IRecipeClient _recipeClient = null!;

    protected override void OnInitialized()
    {
        var baseUrl = (Configuration["ApiClient:BaseUrl"] ?? "http://localhost:5251").TrimEnd('/');
        _baseRoute = $"{baseUrl}/Recipe";
        _fileStoragePrefix = Models.Constants.FileStorageWorkbench.ApiRoutePrefix;
        _recipeClient = new RecipeClient(ApiClient);
    }
}
