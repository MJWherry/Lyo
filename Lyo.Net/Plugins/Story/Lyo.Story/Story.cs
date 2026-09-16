using System.Diagnostics;
using Lyo.Story.Enums;

namespace Lyo.Story;

/// <summary>A playable story or quest graph. Presentation is optional on nodes; the graph is the system of record.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class Story
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    /// <summary>Tenant that owns the story, or null for a single-tenant host. <see cref="IStoryStore.ListStoriesAsync" /> filters on it.</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Game the story belongs to, for example a Lyo Game key, or null when the story stands alone.</summary>
    public string? GameKey { get; set; }

    public string? Description { get; set; }

    public string? Language { get; set; }

    public StoryStatus Status { get; set; }

    public string? StartNodeKey { get; set; }

    public string? CoverImageRef { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public IReadOnlyList<StoryScene> Scenes { get; set; } = [];

    public IReadOnlyList<StoryNode> Nodes { get; set; } = [];

    public IReadOnlyList<StoryVariable> Variables { get; set; } = [];

    public IReadOnlyList<StoryAction> Actions { get; set; } = [];

    public IReadOnlyList<StoryReaction> Reactions { get; set; } = [];

    /// <inheritdoc />
    public override string ToString() => $"Story(Id={Id}, Title=\"{Title}\", Slug=\"{Slug}\", Status={Status})";
}
