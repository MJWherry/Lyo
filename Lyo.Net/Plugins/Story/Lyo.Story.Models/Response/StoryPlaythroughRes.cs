using Lyo.Story.Enums;

namespace Lyo.Story.Models.Response;

/// <summary>Playthrough row as returned by read-only CRUD. The live snapshot is <see cref="StoryPlayStateRes" />.</summary>
public sealed class StoryPlaythroughRes
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Story being played.</summary>
    public Guid StoryId { get; set; }

    /// <summary>Optional opaque player handle.</summary>
    public string? PlayerRef { get; set; }

    /// <summary>Play status.</summary>
    public StoryPlaythroughStatus Status { get; set; }

    /// <summary>Current node row.</summary>
    public Guid? CurrentNodeId { get; set; }

    /// <summary>When play started (UTC).</summary>
    public DateTime StartedAt { get; set; }

    /// <summary>Last move (UTC).</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>When play completed (UTC).</summary>
    public DateTime? CompletedAt { get; set; }
}
