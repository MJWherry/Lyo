using Lyo.Story.Enums;

namespace Lyo.Story.Models.Request;

/// <summary>Create or update a node row.</summary>
public sealed class StoryNodeReq
{
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

    /// <summary>How the node is shown. None is a silent beat.</summary>
    public StoryPresentationKind PresentationKind { get; set; }

    /// <summary>Text body when <see cref="PresentationKind" /> is text.</summary>
    public string? Body { get; set; }

    /// <summary>MIME token for <see cref="Body" />. Use <c>MimeTypeInfo.Txt.Value</c> or <c>MimeTypeInfo.Html.Value</c>.</summary>
    public string? ContentType { get; set; }

    /// <summary>Media reference when <see cref="PresentationKind" /> is a cutscene.</summary>
    public string? MediaRef { get; set; }
}