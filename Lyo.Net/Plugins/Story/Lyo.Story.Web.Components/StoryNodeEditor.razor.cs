namespace Lyo.Story.Web.Components;

/// <summary>Edits one node: key, title, scene, presentation, and ending.</summary>
public partial class StoryNodeEditor
{
    /// <summary>Node being edited.</summary>
    [Parameter]
    [EditorRequired]
    public StoryNodeDraft Node { get; set; } = new();

    /// <summary>Scenes the node can join.</summary>
    [Parameter]
    public IReadOnlyList<StorySceneDraft> Scenes { get; set; } = [];

    /// <summary>Raised after an edit.</summary>
    [Parameter]
    public EventCallback OnChanged { get; set; }

    private Task OnKey(string value)
    {
        Node.Key = value;
        return OnChanged.InvokeAsync();
    }

    private Task OnTitle(string? value)
    {
        Node.Title = value;
        return OnChanged.InvokeAsync();
    }

    private Task OnScene(string? value)
    {
        Node.SceneKey = string.IsNullOrWhiteSpace(value) ? null : value;
        return OnChanged.InvokeAsync();
    }

    private Task OnPresentation(StoryPresentationKind kind)
    {
        Node.PresentationKind = kind;
        return OnChanged.InvokeAsync();
    }

    private Task OnBody(string? value)
    {
        Node.Body = value;
        return OnChanged.InvokeAsync();
    }

    private Task OnContentType(string? value)
    {
        Node.ContentType = value;
        return OnChanged.InvokeAsync();
    }

    private Task OnMedia(string? value)
    {
        Node.MediaRef = value;
        return OnChanged.InvokeAsync();
    }

    private Task OnEnding(bool ending)
    {
        Node.IsEnding = ending;
        return OnChanged.InvokeAsync();
    }
}