using Lyo.Api.Models.Common.Response;
using Lyo.Web.Components.DataGrid;
using Lyo.Web.Components.Dialog;
using Lyo.Web.Components.Models;

namespace Lyo.Recipe.Web.Components;

/// <summary>Projected recipe grid with create/edit dialogs.</summary>
public partial class RecipeGrid
{
    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [
        new("Title"), new("Status"), new("Difficulty"), new("Servings"), new("PrepMinutes"), new("CookMinutes")
    ];

    private Dictionary<string, string> _categoryNames = new(StringComparer.OrdinalIgnoreCase);

    private LyoDataGridProjected? _dataGrid;

    /// <summary>HTTP client used for QueryProject and CRUD.</summary>
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>Typed client for extra recipe routes.</summary>
    [Parameter]
    [EditorRequired]
    public IRecipeClient RecipeClient { get; set; } = null!;

    /// <summary>Recipe route prefix.</summary>
    [Parameter]
    [EditorRequired]
    public string BaseRoute { get; set; } = "Recipe";

    /// <summary>FileStorage MapGroup prefix for attachment downloads.</summary>
    [Parameter]
    public string FileStorageApiRoutePrefix { get; set; } = "FileStorage";

    private string _recipesRoute => $"{BaseRoute.TrimEnd('/')}/Recipes";

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        var categories = await ApiClient.QueryConcreteAsync<QueryRes<RecipeCategoryRes>>($"{BaseRoute.TrimEnd('/')}/Categories", new() { Amount = 500 });
        _categoryNames = (categories?.Items ?? []).ToDictionary(c => c.Id.ToString(), c => c.Name, StringComparer.OrdinalIgnoreCase);
    }

    private string CategoryName(string? categoryId) => categoryId != null && _categoryNames.TryGetValue(categoryId, out var name) ? name : "";

    private async Task OpenCreate()
    {
        var parameters = new DialogParameters<RecipeView> {
            { d => d.ApiClient, ApiClient },
            { d => d.RecipeClient, RecipeClient },
            { d => d.BaseRoute, BaseRoute },
            { d => d.FileStorageApiRoutePrefix, FileStorageApiRoutePrefix },
            { d => d.Recipe, null }
        };

        var dialog = await DialogService.ShowAsync<RecipeView>("New recipe", parameters, LyoDialogPresets.Large);
        var result = await dialog.Result;
        if (result is { Canceled: false } && _dataGrid != null)
            await _dataGrid.RefreshData();
    }

    private async Task OpenRecipe(object? item)
    {
        if (!ProjectedValueHelper.TryGetGuid(ProjectedValueHelper.GetValue(item, "Id"), out var id))
            return;

        var res = await ApiClient.QueryConcreteAsync<QueryRes<RecipeRes>>(_recipesRoute, new() { Keys = [[id]], Amount = 1 });
        var loaded = res?.Items?.FirstOrDefault();
        if (loaded == null)
            return;

        var parameters = new DialogParameters<RecipeView> {
            { d => d.ApiClient, ApiClient },
            { d => d.RecipeClient, RecipeClient },
            { d => d.BaseRoute, BaseRoute },
            { d => d.FileStorageApiRoutePrefix, FileStorageApiRoutePrefix },
            { d => d.Recipe, loaded }
        };

        var dialog = await DialogService.ShowAsync<RecipeView>("Recipe", parameters, LyoDialogPresets.Large);
        var result = await dialog.Result;
        if (result is { Canceled: false } && _dataGrid != null)
            await _dataGrid.RefreshData();
    }

    private async Task OpenCook(object? item)
    {
        var loaded = await LoadRecipe(item);
        if (loaded == null)
            return;

        var parameters = new DialogParameters<RecipeCookView> {
            { d => d.ApiClient, ApiClient },
            { d => d.RecipeClient, RecipeClient },
            { d => d.BaseRoute, BaseRoute },
            { d => d.FileStorageApiRoutePrefix, FileStorageApiRoutePrefix },
            { d => d.Recipe, loaded }
        };

        await DialogService.ShowAsync<RecipeCookView>(loaded.Title, parameters, LyoDialogPresets.Large);
    }

    private async Task<RecipeRes?> LoadRecipe(object? item)
    {
        if (!ProjectedValueHelper.TryGetGuid(ProjectedValueHelper.GetValue(item, "Id"), out var id))
            return null;

        var res = await ApiClient.QueryConcreteAsync<QueryRes<RecipeRes>>(_recipesRoute, new() { Keys = [[id]], Amount = 1 });
        return res?.Items?.FirstOrDefault();
    }
}