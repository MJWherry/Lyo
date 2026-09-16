using Lyo.Note;

namespace Lyo.Recipe.Web.Components;

/// <summary>Nested notes for one recipe subject.</summary>
public partial class RecipeNotesPanel
{
    /// <summary>Typed client.</summary>
    [Parameter]
    [EditorRequired]
    public IRecipeClient RecipeClient { get; set; } = null!;

    /// <summary>Parent collection route, e.g. <c>Recipe/Recipes</c>.</summary>
    [Parameter]
    [EditorRequired]
    public string ParentRoute { get; set; } = "";

    /// <summary>Subject id.</summary>
    [Parameter]
    public Guid EntityId { get; set; }

    private List<NoteRecord> _notes = [];
    private string _draft = "";

    protected override Task OnParametersSetAsync() => Reload();

    private async Task Reload()
    {
        var rows = await RecipeClient.GetNotesAsync(ParentRoute, EntityId);
        _notes = rows.ToList();
    }

    private async Task Add()
    {
        await RecipeClient.AddNoteAsync(ParentRoute, EntityId, new() { Content = _draft });
        _draft = "";
        await Reload();
    }

    private async Task Delete(Guid noteId)
    {
        await RecipeClient.DeleteNoteAsync(ParentRoute, EntityId, noteId);
        await Reload();
    }
}
