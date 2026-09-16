using Lyo.Story.Enums;

namespace Lyo.Story.Models.Response;

/// <summary>Story row as returned by the API. Graph children are not included.</summary>
public sealed class StoryRes
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Display title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>URL slug.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Optional BCP 47 language tag.</summary>
    public string? Language { get; set; }

    /// <summary>Authoring status.</summary>
    public StoryStatus Status { get; set; }

    /// <summary>Start node row.</summary>
    public Guid? StartNodeId { get; set; }

    /// <summary>Optional cover image reference.</summary>
    public string? CoverImageRef { get; set; }

    /// <summary>Created (UTC).</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Last write (UTC).</summary>
    public DateTime? UpdatedTimestamp { get; set; }
}
