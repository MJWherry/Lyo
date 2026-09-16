namespace Lyo.Recipe.Web.Components;

/// <summary>Tabbed household-recipe workbench. Host passes <see cref="ApiClient"/> and <see cref="RecipeClient"/>.</summary>
public partial class RecipeManagement
{
    /// <summary>HTTP client for QueryProject and entity CRUD.</summary>
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>Typed client for lines, steps, notes, tags, and upload.</summary>
    [Parameter]
    [EditorRequired]
    public IRecipeClient RecipeClient { get; set; } = null!;

    /// <summary>Route prefix for Recipe endpoints (for example <c>Recipe</c> or <c>http://localhost:5251/Recipe</c>).</summary>
    [Parameter]
    public string BaseRoute { get; set; } = "Recipe";

    /// <summary>FileStorage MapGroup prefix. Gateway uses <c>Workbench/FileStorage</c>; Example hosts use <c>FileStorage</c>.</summary>
    [Parameter]
    public string FileStorageApiRoutePrefix { get; set; } = "FileStorage";

    private int _tabIndex;
}
