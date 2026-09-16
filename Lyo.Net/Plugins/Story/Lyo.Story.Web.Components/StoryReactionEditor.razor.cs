namespace Lyo.Story.Web.Components;

/// <summary>Edits ambient reactions: trigger, scope, once, condition, and response steps.</summary>
public partial class StoryReactionEditor
{
    private int _index;

    /// <summary>Reactions. Mutated in place.</summary>
    [Parameter]
    [EditorRequired]
    public List<StoryReactionDraft> Reactions { get; set; } = [];

    /// <summary>Condition field presets.</summary>
    [Parameter]
    public IReadOnlyList<FilterPropertyDefinition> Fields { get; set; } = [];

    /// <summary>Declared variables.</summary>
    [Parameter]
    public IReadOnlyList<StoryVariableDraft> Variables { get; set; } = [];

    /// <summary>Nodes a scope or goto can name.</summary>
    [Parameter]
    public IReadOnlyList<StoryNodeDraft> Nodes { get; set; } = [];

    /// <summary>Named actions an invoke can call.</summary>
    [Parameter]
    public IReadOnlyList<StoryActionDraft> Actions { get; set; } = [];

    /// <summary>Raised after an edit.</summary>
    [Parameter]
    public EventCallback OnChanged { get; set; }

    private async Task Add()
    {
        Reactions.Add(new() { Key = "react_" + (Reactions.Count + 1), Trigger = StoryTrigger.StoryStarted });
        _index = Reactions.Count - 1;
        await OnChanged.InvokeAsync();
    }

    private async Task Remove()
    {
        if (_index < 0 || _index >= Reactions.Count)
            return;
        Reactions.RemoveAt(_index);
        _index = Math.Max(0, _index - 1);
        await OnChanged.InvokeAsync();
    }

    private Task Select(int index)
    {
        _index = index;
        return Task.CompletedTask;
    }

    private Task Set(Action<StoryReactionDraft> apply)
    {
        apply(Reactions[_index]);
        return OnChanged.InvokeAsync();
    }
}
