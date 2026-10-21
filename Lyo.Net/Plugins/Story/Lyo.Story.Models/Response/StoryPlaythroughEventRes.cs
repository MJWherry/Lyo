using Lyo.Story.Enums;

namespace Lyo.Story.Models.Response;

/// <summary>One append-only play log row.</summary>
public sealed class StoryPlaythroughEventRes
{
    /// <summary>Event id.</summary>
    public Guid Id { get; set; }

    /// <summary>Order within the playthrough.</summary>
    public int Sequence { get; set; }

    /// <summary>What happened.</summary>
    public StoryPlaythroughEventKind Kind { get; set; }

    /// <summary>Node key, when the event is tied to a node.</summary>
    public string? NodeKey { get; set; }

    /// <summary>Choice key, when the player took a choice.</summary>
    public string? ChoiceKey { get; set; }

    /// <summary>Reaction key, when a reaction fired.</summary>
    public string? ReactionKey { get; set; }

    /// <summary>Action key, when an action ran.</summary>
    public string? ActionKey { get; set; }

    /// <summary>Variable key, when a variable changed.</summary>
    public string? VariableKey { get; set; }

    /// <summary>Previous value.</summary>
    public string? OldValue { get; set; }

    /// <summary>New value.</summary>
    public string? NewValue { get; set; }

    /// <summary>Event that caused this one.</summary>
    public Guid? CausedByEventId { get; set; }

    /// <summary>When the event was recorded (UTC).</summary>
    public DateTime OccurredAt { get; set; }
}