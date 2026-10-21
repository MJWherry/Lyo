using System.Diagnostics;
using Lyo.Story.Enums;

namespace Lyo.Story;

/// <summary>One run of a story graph.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class StoryPlaythrough
{
    public Guid Id { get; set; }

    public Guid StoryId { get; set; }

    public string? PlayerRef { get; set; }

    public StoryPlaythroughStatus Status { get; set; }

    public string? CurrentNodeKey { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public IReadOnlyList<StoryPlaythroughVariable> Variables { get; set; } = [];

    /// <inheritdoc />
    public override string ToString() => $"StoryPlaythrough(Id={Id}, Status={Status}, Node={CurrentNodeKey})";
}