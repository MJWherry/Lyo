using System.Diagnostics;
using Lyo.Story.Enums;

namespace Lyo.Story.Sqlite.Database;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class StoryPlaythroughEventEntity
{
    public Guid Id { get; set; }

    public Guid PlaythroughId { get; set; }

    public int Sequence { get; set; }

    public StoryPlaythroughEventKind Kind { get; set; }

    public Guid? NodeId { get; set; }

    public Guid? ChoiceId { get; set; }

    public Guid? ReactionId { get; set; }

    public Guid? ActionId { get; set; }

    public string? VariableKey { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public Guid? CausedByEventId { get; set; }

    public DateTime OccurredAt { get; set; }

    public StoryPlaythroughEntity Playthrough { get; set; } = null!;

    public StoryNodeEntity? Node { get; set; }

    public StoryChoiceEntity? Choice { get; set; }

    public StoryReactionEntity? Reaction { get; set; }

    public StoryActionEntity? Action { get; set; }

    public StoryPlaythroughEventEntity? CausedBy { get; set; }

    /// <summary>Type name and id.</summary>
    public override string ToString() => $"{GetType().Name} {Id}";
}