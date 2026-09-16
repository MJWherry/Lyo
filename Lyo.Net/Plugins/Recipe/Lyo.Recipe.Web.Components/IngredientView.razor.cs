using Lyo.Api.Models.Common.Request;
using Lyo.Api.Models.Common.Response;
using Lyo.Recipe.Models;

namespace Lyo.Recipe.Web.Components;

/// <summary>Create/edit dialog for an ingredient catalog row.</summary>
public partial class IngredientView
{
    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    /// <summary>HTTP client for CRUD.</summary>
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

    /// <summary>Existing ingredient when editing; null for create.</summary>
    [Parameter]
    public IngredientRes? Ingredient { get; set; }

    private readonly IngredientReq _req = new();
    private Guid? _savedId;
    private IReadOnlyList<UnitRes> _units = [];
    private IReadOnlyList<IngredientUsageRes> _usage = [];

    private string _route => $"{BaseRoute.TrimEnd('/')}/Ingredients";

    private string _attachmentsRoute => $"{BaseRoute.TrimEnd('/')}/Attachments";

    private static string NotesParentRoute => Constants.Rest.Recipe.Ingredients;

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
        _units = await RecipeClient.ListUnitsAsync();
        if (Ingredient == null)
            return;
        _savedId = Ingredient.Id;
        _req.Name = Ingredient.Name;
        _req.Slug = Ingredient.Slug;
        _req.DefaultUnit = Ingredient.DefaultUnit;
        _req.Notes = Ingredient.Notes;
        _usage = await RecipeClient.ListIngredientUsageAsync(Ingredient.Id);
    }

    private void Cancel() => MudDialog.Close(DialogResult.Ok(true));

    private async Task Save()
    {
        if (string.IsNullOrWhiteSpace(_req.Slug))
            _req.Slug = RecipeSlugs.FromName(_req.Name);

        if (_savedId is Guid id) {
            await ApiClient.PostAsAsync<UpdateRequest<IngredientReq>, UpdateResult<IngredientRes>>($"{_route}/Update", new(_req, id));
        }
        else {
            var created = await ApiClient.PostAsAsync<IngredientReq, CreateResult<IngredientRes>>(_route, _req);
            _savedId = created.Data?.Id;
        }

        await InvokeAsync(StateHasChanged);
    }
}
