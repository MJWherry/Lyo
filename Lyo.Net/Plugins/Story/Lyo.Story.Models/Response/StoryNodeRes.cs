using Lyo.Story.Enums;

namespace Lyo.Story.Models.Response;

/// <summary>Node row as returned by the API.</summary>
public sealed class StoryNodeRes
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Owning story.</summary>
    public Guid StoryId { get; set; }

    /// <summary>Optional scene grouping.</summary>
    public Guid? SceneId { get; set; }

    /// <summary>Stable key within the story.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Optional title.</summary>
    public string? Title { get; set; }

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
}
