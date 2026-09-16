using Lyo.Story.Enums;

namespace Lyo.Story.Models.Response;

/// <summary>Full play graph returned by the graph routes.</summary>
public sealed class StoryGraphRes
{
    /// <summary>Story id.</summary>
    public Guid Id { get; set; }

    /// <summary>Display title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>URL slug.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Tenant that owns the story, or null.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Game the story belongs to, or null.</summary>
    public string? GameKey { get; set; }

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Optional BCP 47 language tag.</summary>
    public string? Language { get; set; }

    /// <summary>Authoring status.</summary>
    public StoryStatus Status { get; set; }

    /// <summary>Key of the node play starts on.</summary>
    public string? StartNodeKey { get; set; }

    /// <summary>Optional cover image reference.</summary>
    public string? CoverImageRef { get; set; }

    /// <summary>Created (UTC).</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Last write (UTC).</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <summary>Scene groups.</summary>
    public IReadOnlyList<StoryGraphSceneRes> Scenes { get; set; } = [];

    /// <summary>Nodes, including choices.</summary>
    public IReadOnlyList<StoryGraphNodeRes> Nodes { get; set; } = [];

    /// <summary>Declared variables.</summary>
    public IReadOnlyList<StoryGraphVariableRes> Variables { get; set; } = [];

    /// <summary>Named actions.</summary>
    public IReadOnlyList<StoryGraphActionRes> Actions { get; set; } = [];

    /// <summary>Ambient reactions.</summary>
    public IReadOnlyList<StoryGraphReactionRes> Reactions { get; set; } = [];
}
