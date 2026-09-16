using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Query.Models.Common.Request;
using Lyo.Recipe.Models;
using Lyo.Recipe.Models.Enums;
using Lyo.Web.Components.Dialog;

namespace Lyo.Recipe.Web.Components;

/// <summary>Create/edit dialog for a household recipe. Lines and steps are edited before the first save and written with it.</summary>
public partial class RecipeView
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    /// <summary>HTTP client for CRUD.</summary>
    [Parameter]
    [EditorRequired]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>Typed client for extra routes.</summary>
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

    /// <summary>Existing recipe when editing; null for create.</summary>
    [Parameter]
    public RecipeRes? Recipe { get; set; }

    private readonly RecipeReq _req = new();
    private Guid? _savedId;
    private IReadOnlyList<RecipeCategoryRes> _categories = [];
    private IReadOnlyList<IngredientRes> _ingredients = [];
    private IReadOnlyList<UnitRes> _units = [];
    private readonly List<RecipeIngredientLineReq> _lines = [];
    private readonly List<RecipeStepReq> _steps = [];
    private HashSet<Guid> _savedStepIds = [];

    private string _recipesRoute => $"{BaseRoute.TrimEnd('/')}/Recipes";

    private string _attachmentsRoute => $"{BaseRoute.TrimEnd('/')}/Attachments";

    private static string NotesParentRoute => Constants.Rest.Recipe.Recipes;

    private string ApiOrigin
    {
        get
        {
            if (Uri.TryCreate(BaseRoute, UriKind.Absolute, out var uri))
                return uri.GetLeftPart(UriPartial.Authority);
            return "";
        }
    }

    protected override async Task OnInitializedAsync()
    {
        if (Recipe != null) {
            _savedId = Recipe.Id;
            _req.Title = Recipe.Title;
            _req.Slug = Recipe.Slug;
            _req.Description = Recipe.Description;
            _req.Servings = Recipe.Servings;
            _req.PrepMinutes = Recipe.PrepMinutes;
            _req.CookMinutes = Recipe.CookMinutes;
            _req.Difficulty = Recipe.Difficulty;
            _req.Status = Recipe.Status;
            _req.SourceName = Recipe.SourceName;
            _req.SourceUrl = Recipe.SourceUrl;
            _req.YieldLabel = Recipe.YieldLabel;
            _req.CategoryId = Recipe.CategoryId;
            var lines = await RecipeClient.GetIngredientLinesAsync(Recipe.Id);
            _lines.AddRange(lines.Select(ToLineReq));
            var steps = await RecipeClient.GetStepsAsync(Recipe.Id);
            _steps.AddRange(steps.Select(ToStepReq));
            _savedStepIds = _steps.Select(s => s.Id).ToHashSet();
        }

        var categories = await ApiClient.QueryConcreteAsync<QueryRes<RecipeCategoryRes>>($"{BaseRoute.TrimEnd('/')}/Categories", new QueryConcreteReq { Amount = 500 });
        _categories = categories?.Items ?? [];
        var ingredients = await ApiClient.QueryConcreteAsync<QueryRes<IngredientRes>>($"{BaseRoute.TrimEnd('/')}/Ingredients", new QueryConcreteReq { Amount = 500 });
        _ingredients = ingredients?.Items ?? [];
        _units = await RecipeClient.ListUnitsAsync();
    }

    private void Refresh() => StateHasChanged();

    private void Cancel() => MudDialog.Close(DialogResult.Ok(true));

    private Task SaveClick() => Save();

    private async Task<bool> Save()
    {
        try {
            if (string.IsNullOrWhiteSpace(_req.Slug))
                _req.Slug = RecipeSlugs.FromName(_req.Title);

            if (_savedId is Guid id)
                await ApiClient.PostAsAsync<UpdateRequest<RecipeReq>, UpdateResult<RecipeRes>>($"{_recipesRoute}/Update", new(_req, id));
            else {
                var created = await ApiClient.PostAsAsync<RecipeReq, CreateResult<RecipeRes>>(_recipesRoute, _req);
                _savedId = created.Data?.Id;
            }

            if (_savedId is not Guid recipeId)
                return false;

            var steps = _steps.Where(s => !string.IsNullOrWhiteSpace(s.Instruction)).ToList();
            for (var i = 0; i < steps.Count; i++)
                steps[i].SortOrder = i;
            await RecipeClient.ReplaceStepsAsync(recipeId, steps);
            _steps.Clear();
            _steps.AddRange(steps);

            var lines = _lines.Where(l => l.IngredientId != null || !string.IsNullOrWhiteSpace(l.IngredientText)).ToList();
            var stepIds = steps.Select(s => s.Id).ToHashSet();
            for (var i = 0; i < lines.Count; i++) {
                lines[i].SortOrder = i;
                lines[i].StepIds = lines[i].StepIds.Where(stepIds.Contains).ToList();
            }

            var savedLines = await RecipeClient.ReplaceIngredientLinesAsync(recipeId, lines);
            _lines.Clear();
            _lines.AddRange(savedLines.Select(ToLineReq));
            _savedStepIds = steps.Select(s => s.Id).ToHashSet();
            Snackbar.Add("Saved", Severity.Success);
            return true;
        }
        catch (Exception ex) {
            Snackbar.Add(ex.Message, Severity.Error);
            return false;
        }
    }

    private async Task Cook()
    {
        if (!await Save())
            return;
        if (_savedId is not Guid saved)
            return;
        var recipe = new RecipeRes {
            Id = saved,
            Title = _req.Title,
            Slug = _req.Slug,
            Description = _req.Description,
            Servings = _req.Servings,
            PrepMinutes = _req.PrepMinutes,
            CookMinutes = _req.CookMinutes,
            Difficulty = _req.Difficulty,
            Status = _req.Status,
            SourceName = _req.SourceName,
            SourceUrl = _req.SourceUrl,
            YieldLabel = _req.YieldLabel,
            CategoryId = _req.CategoryId
        };
        var parameters = new DialogParameters<RecipeCookView> {
            { d => d.ApiClient, ApiClient },
            { d => d.RecipeClient, RecipeClient },
            { d => d.BaseRoute, BaseRoute },
            { d => d.FileStorageApiRoutePrefix, FileStorageApiRoutePrefix },
            { d => d.Recipe, recipe }
        };
        await DialogService.ShowAsync<RecipeCookView>(string.IsNullOrWhiteSpace(recipe.Title) ? "Cook" : recipe.Title, parameters, LyoDialogPresets.Large);
    }

    private static RecipeIngredientLineReq ToLineReq(RecipeIngredientLineRes row)
        => new() {
            Id = row.Id,
            IngredientId = row.IngredientId,
            IngredientText = row.IngredientText,
            SortOrder = row.SortOrder,
            Quantity = row.Quantity,
            Unit = row.Unit,
            Preparation = row.Preparation,
            IsOptional = row.IsOptional,
            GroupLabel = row.GroupLabel,
            StepIds = row.StepIds ?? []
        };

    private static RecipeStepReq ToStepReq(RecipeStepRes row)
        => new() {
            Id = row.Id,
            SortOrder = row.SortOrder,
            Instruction = row.Instruction,
            DurationMinutes = row.DurationMinutes,
            GroupLabel = row.GroupLabel
        };
}
