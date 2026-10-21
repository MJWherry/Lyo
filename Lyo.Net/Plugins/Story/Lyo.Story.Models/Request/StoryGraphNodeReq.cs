using Lyo.Story.Enums;

namespace Lyo.Story.Models.Request;

/// <summary>Node inside a <see cref="StoryGraphReq" />.</summary>
public sealed class StoryGraphNodeReq
{
    /// <summary>Stable key within the story.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Optional title.</summary>
    public string? Title { get; set; }

    /// <summary>Scene key. Null when the node is ungrouped.</summary>
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
    public List<StoryGraphChoiceReq> Choices { get; set; } = [];
}