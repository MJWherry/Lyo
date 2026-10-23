using Lyo.Story.Enums;

namespace Lyo.Story.Models.Response;

/// <summary>Effect row as returned by the API.</summary>
public sealed class StoryEffectRes
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Owning choice.</summary>
    public Guid? ChoiceId { get; set; }

    /// <summary>Owning action.</summary>
    public Guid? ActionId { get; set; }

    /// <summary>Owning reaction.</summary>
    public Guid? ReactionId { get; set; }

    /// <summary>Order within the parent.</summary>
    public int SortOrder { get; set; }

    /// <summary>What the step does.</summary>
    public StoryEffectKind Kind { get; set; }

    /// <summary>Variable to set.</summary>
    public string? VariableKey { get; set; }

    /// <summary>Value written.</summary>
    public string? Value { get; set; }

    /// <summary>Node to move to.</summary>
    public Guid? TargetNodeId { get; set; }

    /// <summary>Action to invoke.</summary>
    public Guid? InvokeActionId { get; set; }
}