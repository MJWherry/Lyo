using Lyo.Story.Enums;

namespace Lyo.Story.Models.Request;

/// <summary>One response step inside a graph choice, action, or reaction. Targets are keys.</summary>
public sealed class StoryGraphEffectReq
{
    /// <summary>What the step does.</summary>
    public StoryEffectKind Kind { get; set; }

    /// <summary>Order within the parent.</summary>
    public int SortOrder { get; set; }

    /// <summary>Variable to set.</summary>
    public string? VariableKey { get; set; }

    /// <summary>Value written.</summary>
    public string? Value { get; set; }

    /// <summary>Node key to move to.</summary>
    public string? TargetNodeKey { get; set; }

    /// <summary>Action key to invoke.</summary>
    public string? InvokeActionKey { get; set; }
}