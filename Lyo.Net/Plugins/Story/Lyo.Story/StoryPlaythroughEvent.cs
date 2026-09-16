using System.Diagnostics;
using Lyo.Story.Enums;

namespace Lyo.Story;

/// <summary>Append-only play log row. <see cref="CausedByEventId"/> is the only consequence pointer.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class StoryPlaythroughEvent
{
    public Guid Id { get; set; }

    public int Sequence { get; set; }

    public StoryPlaythroughEventKind Kind { get; set; }

    public string? NodeKey { get; set; }

    public string? ChoiceKey { get; set; }

    public string? ReactionKey { get; set; }

    public string? ActionKey { get; set; }

    public string? VariableKey { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public Guid? CausedByEventId { get; set; }

    public DateTime OccurredAt { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"StoryPlaythroughEvent(Seq={Sequence}, Kind={Kind})";
}
