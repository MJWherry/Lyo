using Lyo.Story.Enums;

namespace Lyo.Story.Models.Request;

/// <summary>Create or update a story row. The play aggregate is <see cref="StoryGraphReq" />.</summary>
public sealed class StoryReq
{
    /// <summary>Display title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>URL slug. Unique per catalog.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Optional longer description.</summary>
    public string? Description { get; set; }

    /// <summary>Optional BCP 47 language tag.</summary>
    public string? Language { get; set; }

    /// <summary>Authoring status.</summary>
    public StoryStatus Status { get; set; }

    /// <summary>Start node row. Null until the graph is saved.</summary>
    public Guid? StartNodeId { get; set; }

    /// <summary>Optional cover image reference.</summary>
    public string? CoverImageRef { get; set; }
}