using Lyo.Story.Enums;

namespace Lyo.Story.Models.Response;

/// <summary>Node inside a <see cref="StoryGraphRes" />.</summary>
public sealed class StoryGraphNodeRes
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Stable key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Optional title.</summary>
    public string? Title { get; set; }

    /// <summary>Scene key.</summary>
    public string? SceneKey { get; set; }

    /// <summary>True when this node ends the playthrough.</summary>
    public bool IsEnding { get; set; }

    /// <summary>How the node is shown.</summary>
    public StoryPresentationKind PresentationKind { get; set; }

    /// <summary>Text body.</summary>
    public string? Body { get; set; }

    /// <summary>MIME token for <see cref="Body" />.</summary>
    public string? ContentType { get; set; }

    /// <summary>Cutscene media reference.</summary>
    public string? MediaRef { get; set; }

    /// <summary>Choices on this node.</summary>
    public IReadOnlyList<StoryGraphChoiceRes> Choices { get; set; } = [];
}