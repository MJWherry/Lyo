using Lyo.Tag;

namespace Lyo.Recipe.Web.Components;

/// <summary>Nested tags for one recipe subject.</summary>
public partial class RecipeTagsPanel
{
    private List<string> _tagNames = [];

    private List<TagRecord> _tags = [];

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

    protected override Task OnParametersSetAsync() => Reload();

    private async Task Reload()
    {
        var rows = await RecipeClient.GetTagsAsync(ParentRoute, EntityId);
        _tags = rows.ToList();
        _tagNames = _tags.Select(t => t.Name).ToList();
    }

    private async Task OnTagsChanged(IEnumerable<string> values)
    {
        var next = values.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var previous = _tagNames;
        var added = next.Except(previous, StringComparer.OrdinalIgnoreCase).ToList();
        var removed = previous.Except(next, StringComparer.OrdinalIgnoreCase).ToList();
        _tagNames = next;
        foreach (var name in added)
            await RecipeClient.AddTagAsync(ParentRoute, EntityId, new() { Name = name });

        foreach (var name in removed)
            await RecipeClient.RemoveTagAsync(ParentRoute, EntityId, name);

        await Reload();
    }
}