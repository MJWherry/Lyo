using Lyo.Story.Enums;

namespace Lyo.Story.Models.Request;

/// <summary>Create or update one response step. Exactly one parent id is set.</summary>
public sealed class StoryEffectReq
{
    /// <summary>Owning choice, when this step hangs off a choice.</summary>
    public Guid? ChoiceId { get; set; }

    /// <summary>Owning action, when this step hangs off an action.</summary>
    public Guid? ActionId { get; set; }

    /// <summary>Owning reaction, when this step hangs off a reaction.</summary>
    public Guid? ReactionId { get; set; }

    /// <summary>Order within the parent.</summary>
    public int SortOrder { get; set; }

    /// <summary>What the step does.</summary>
    public StoryEffectKind Kind { get; set; }

    /// <summary>Variable to set, when <see cref="Kind" /> writes a variable.</summary>
    public string? VariableKey { get; set; }

    /// <summary>Value written, when <see cref="Kind" /> writes a variable.</summary>
    public string? Value { get; set; }

    /// <summary>Node to move to, when <see cref="Kind" /> changes the cursor.</summary>
    public Guid? TargetNodeId { get; set; }

    /// <summary>Action to invoke, when <see cref="Kind" /> calls an action.</summary>
    public Guid? InvokeActionId { get; set; }
}
