namespace Lyo.Story.Web.Components;

/// <summary>Edits the story's declared variables.</summary>
public partial class StoryVariableEditor
{
    /// <summary>Variables. Mutated in place.</summary>
    [Parameter]
    [EditorRequired]
    public List<StoryVariableDraft> Variables { get; set; } = [];

    /// <summary>Raised after an edit.</summary>
    [Parameter]
    public EventCallback OnChanged { get; set; }

    private async Task Add()
    {
        Variables.Add(new() { Key = "flag_" + (Variables.Count + 1), Type = StoryVariableType.Boolean, DefaultValue = "false" });
        await OnChanged.InvokeAsync();
    }

    private async Task Remove(int index)
    {
        Variables.RemoveAt(index);
        await OnChanged.InvokeAsync();
    }

    private async Task Set(int index, Action<StoryVariableDraft> apply)
    {
        apply(Variables[index]);
        await OnChanged.InvokeAsync();
    }
}