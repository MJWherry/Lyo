using Lyo.Story.Enums;

namespace Lyo.Story.Models.Request;

/// <summary>Full play graph for <c>POST /Story/graph</c>. Distinct from the row <see cref="StoryReq" />.</summary>
public sealed class StoryGraphReq
{
    /// <summary>Existing id to update. Empty creates a story.</summary>
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

    /// <summary>Optional scene groups.</summary>
    public List<StoryGraphSceneReq> Scenes { get; set; } = [];

    /// <summary>Nodes, including their choices.</summary>
    public List<StoryGraphNodeReq> Nodes { get; set; } = [];

    /// <summary>Declared variables.</summary>
    public List<StoryGraphVariableReq> Variables { get; set; } = [];

    /// <summary>Named actions.</summary>
    public List<StoryGraphActionReq> Actions { get; set; } = [];

    /// <summary>Ambient reactions.</summary>
    public List<StoryGraphReactionReq> Reactions { get; set; } = [];
}
