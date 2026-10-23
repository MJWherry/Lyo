namespace Lyo.Story.Web.Components;

/// <summary>Edits one choice: prompt, visibility, enablement, and response steps.</summary>
public partial class StoryChoiceEditor
{
    /// <summary>Choice being edited.</summary>
    [Parameter]
    [EditorRequired]
    public StoryChoiceDraft Choice { get; set; } = new();

    /// <summary>Condition field presets for this story.</summary>
    [Parameter]
    public IReadOnlyList<FilterPropertyDefinition> Fields { get; set; } = [];

    /// <summary>Declared variables.</summary>
    [Parameter]
    public IReadOnlyList<StoryVariableDraft> Variables { get; set; } = [];

    /// <summary>Nodes a goto can target.</summary>
    [Parameter]
    public IReadOnlyList<StoryNodeDraft> Nodes { get; set; } = [];

    /// <summary>Named actions an invoke can call.</summary>
    [Parameter]
    public IReadOnlyList<StoryActionDraft> Actions { get; set; } = [];

    /// <summary>Raised after an edit.</summary>
    [Parameter]
    public EventCallback OnChanged { get; set; }

    private Task OnKey(string value)
    {
        Choice.Key = value;
        return OnChanged.InvokeAsync();
    }

    private Task OnPrompt(string value)
    {
        Choice.Prompt = value;
        return OnChanged.InvokeAsync();
    }

    private Task OnEnabled(WhereClause? clause)
    {
        Choice.EnabledWhen = clause;
        return OnChanged.InvokeAsync();
    }

    private Task OnVisible(WhereClause? clause)
    {
        Choice.VisibleWhen = clause;
        return OnChanged.InvokeAsync();
    }
}