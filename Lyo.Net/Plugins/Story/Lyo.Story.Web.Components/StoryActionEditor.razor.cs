namespace Lyo.Story.Web.Components;

/// <summary>Edits named actions and the response list each one runs.</summary>
public partial class StoryActionEditor
{
    private int _index;

    /// <summary>Actions. Mutated in place.</summary>
    [Parameter]
    [EditorRequired]
    public List<StoryActionDraft> Actions { get; set; } = [];

    /// <summary>Declared variables.</summary>
    [Parameter]
    public IReadOnlyList<StoryVariableDraft> Variables { get; set; } = [];

    /// <summary>Nodes a goto can target.</summary>
    [Parameter]
    public IReadOnlyList<StoryNodeDraft> Nodes { get; set; } = [];

    /// <summary>Raised after an edit.</summary>
    [Parameter]
    public EventCallback OnChanged { get; set; }

    private async Task Add()
    {
        Actions.Add(new() { Key = "action_" + (Actions.Count + 1) });
        _index = Actions.Count - 1;
        await OnChanged.InvokeAsync();
    }

    private async Task Remove()
    {
        if (_index < 0 || _index >= Actions.Count)
            return;

        Actions.RemoveAt(_index);
        _index = Math.Max(0, _index - 1);
        await OnChanged.InvokeAsync();
    }

    private Task Select(int index)
    {
        _index = index;
        return Task.CompletedTask;
    }

    private Task Set(Action<StoryActionDraft> apply)
    {
        apply(Actions[_index]);
        return OnChanged.InvokeAsync();
    }
}