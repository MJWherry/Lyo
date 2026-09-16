namespace Lyo.Story.Web.Components;

/// <summary>Ordered response list: set, unset, increment, goto, end, and invoke.</summary>
public partial class StoryEffectListEditor
{
    /// <summary>Steps to edit. The list is mutated in place.</summary>
    [Parameter]
    [EditorRequired]
    public List<StoryEffectDraft> Effects { get; set; } = [];

    /// <summary>Declared variables offered to set, unset, and increment.</summary>
    [Parameter]
    public IReadOnlyList<StoryVariableDraft> Variables { get; set; } = [];

    /// <summary>Nodes offered to a goto step.</summary>
    [Parameter]
    public IReadOnlyList<StoryNodeDraft> Nodes { get; set; } = [];

    /// <summary>Actions offered to an invoke step.</summary>
    [Parameter]
    public IReadOnlyList<StoryActionDraft> Actions { get; set; } = [];

    /// <summary>Raised after a step is added, removed, reordered, or edited.</summary>
    [Parameter]
    public EventCallback OnChanged { get; set; }

    private async Task Add()
    {
        Effects.Add(new() { Kind = StoryEffectKind.Set, SortOrder = Effects.Count });
        await OnChanged.InvokeAsync();
    }

    private async Task Remove(int index)
    {
        Effects.RemoveAt(index);
        await OnChanged.InvokeAsync();
    }

    private async Task Move(int index, int delta)
    {
        var swap = index + delta;
        (Effects[index], Effects[swap]) = (Effects[swap], Effects[index]);
        await OnChanged.InvokeAsync();
    }

    private async Task SetKind(int index, StoryEffectKind kind)
    {
        Effects[index].Kind = kind;
        await OnChanged.InvokeAsync();
    }

    private async Task Set(int index, Action<StoryEffectDraft> apply)
    {
        apply(Effects[index]);
        await OnChanged.InvokeAsync();
    }
}
