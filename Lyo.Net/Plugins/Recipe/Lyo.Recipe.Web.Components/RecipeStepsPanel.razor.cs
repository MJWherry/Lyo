using Lyo.Common.Core.Identifiers;

namespace Lyo.Recipe.Web.Components;

/// <summary>Reorderable instruction cards. The parent owns the list and saves it with the recipe.</summary>
public partial class RecipeStepsPanel
{
    /// <summary>Steps in display order. New steps already have an id so lines can tag them before save.</summary>
    [Parameter]
    public List<RecipeStepReq> Steps { get; set; } = [];

    /// <summary>Ingredient lines, used to show which lines tag this step.</summary>
    [Parameter]
    public IReadOnlyList<RecipeIngredientLineReq> Lines { get; set; } = [];

    /// <summary>Catalog names for linked-line chips.</summary>
    [Parameter]
    public IReadOnlyList<IngredientRes> Ingredients { get; set; } = [];

    /// <summary>HTTP client for attachment list and update.</summary>
    [Parameter]
    public IApiClient ApiClient { get; set; } = null!;

    /// <summary>Typed client for attachment upload.</summary>
    [Parameter]
    public IRecipeClient RecipeClient { get; set; } = null!;

    /// <summary>Attachment CRUD route.</summary>
    [Parameter]
    public string AttachmentsRoute { get; set; } = "";

    /// <summary>FileStorage MapGroup prefix.</summary>
    [Parameter]
    public string FileStorageApiRoutePrefix { get; set; } = "FileStorage";

    /// <summary>Origin used when the attachment route is absolute.</summary>
    [Parameter]
    public string ApiOrigin { get; set; } = "";

    /// <summary>When false, step photos stay hidden. New recipes enable this after the first save.</summary>
    [Parameter]
    public bool AllowAttachments { get; set; }

    /// <summary>Step ids that already exist in the store. Photos can be attached to these.</summary>
    [Parameter]
    public IReadOnlySet<Guid> SavedStepIds { get; set; } = new HashSet<Guid>();

    /// <summary>Raised after a step is added or removed.</summary>
    [Parameter]
    public EventCallback Changed { get; set; }

    private string LineName(RecipeIngredientLineReq line)
    {
        if (line.IngredientId is Guid id) {
            var match = Ingredients.FirstOrDefault(i => i.Id == id);
            if (match != null)
                return match.Name;
        }

        return string.IsNullOrWhiteSpace(line.IngredientText) ? "Ingredient" : line.IngredientText;
    }

    private async Task Add()
    {
        Steps.Add(new() { Id = LyoGuid.CreateV7(), SortOrder = Steps.Count, Instruction = "" });
        if (Changed.HasDelegate)
            await Changed.InvokeAsync();
    }

    private async Task Remove(RecipeStepReq step)
    {
        Steps.Remove(step);
        foreach (var line in Lines)
            line.StepIds.Remove(step.Id);

        if (Changed.HasDelegate)
            await Changed.InvokeAsync();
    }
}