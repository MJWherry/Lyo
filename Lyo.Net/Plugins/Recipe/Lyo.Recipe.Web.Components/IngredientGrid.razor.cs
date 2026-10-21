using Lyo.Api.Models.Common.Response;
using Lyo.Web.Components.DataGrid;
using Lyo.Web.Components.Dialog;
using Lyo.Web.Components.Models;

namespace Lyo.Recipe.Web.Components;

/// <summary>Projected ingredient catalog grid.</summary>
public partial class IngredientGrid
{
    private readonly List<FilterPropertyDefinition> _propertyDefinitions = [new("Name"), new("Slug")];

    private LyoDataGridProjected? _dataGrid;

    /// <summary>HTTP client.</summary>
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>Typed client for notes, tags, and upload.</summary>
    [Parameter]
    [EditorRequired]
    public IRecipeClient RecipeClient { get; set; } = null!;

    /// <summary>Recipe route prefix.</summary>
    [Parameter]
    [EditorRequired]
    public string BaseRoute { get; set; } = "Recipe";

    /// <summary>FileStorage MapGroup prefix.</summary>
    [Parameter]
    public string FileStorageApiRoutePrefix { get; set; } = "FileStorage";

    private string _route => $"{BaseRoute.TrimEnd('/')}/Ingredients";

    private async Task OpenCreate()
    {
        var parameters = new DialogParameters<IngredientView> {
            { d => d.ApiClient, ApiClient },
            { d => d.RecipeClient, RecipeClient },
            { d => d.BaseRoute, BaseRoute },
            { d => d.FileStorageApiRoutePrefix, FileStorageApiRoutePrefix },
            { d => d.Ingredient, null }
        };

        var dialog = await DialogService.ShowAsync<IngredientView>("New ingredient", parameters, LyoDialogPresets.Medium);
        var result = await dialog.Result;
        if (result is { Canceled: false } && _dataGrid != null)
            await _dataGrid.RefreshData();
    }

    private async Task OpenIngredient(object? item)
    {
        if (!ProjectedValueHelper.TryGetGuid(ProjectedValueHelper.GetValue(item, "Id"), out var id))
            return;

        var res = await ApiClient.QueryConcreteAsync<QueryRes<IngredientRes>>(_route, new() { Keys = [[id]], Amount = 1 });
        var loaded = res?.Items?.FirstOrDefault();
        if (loaded == null)
            return;

        var parameters = new DialogParameters<IngredientView> {
            { d => d.ApiClient, ApiClient },
            { d => d.RecipeClient, RecipeClient },
            { d => d.BaseRoute, BaseRoute },
            { d => d.FileStorageApiRoutePrefix, FileStorageApiRoutePrefix },
            { d => d.Ingredient, loaded }
        };

        var dialog = await DialogService.ShowAsync<IngredientView>("Ingredient", parameters, LyoDialogPresets.Medium);
        var result = await dialog.Result;
        if (result is { Canceled: false } && _dataGrid != null)
            await _dataGrid.RefreshData();
    }
}