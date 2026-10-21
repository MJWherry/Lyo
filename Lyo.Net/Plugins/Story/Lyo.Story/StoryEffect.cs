using System.Diagnostics;
using Lyo.Story.Enums;

namespace Lyo.Story;

/// <summary>One ordered response step. Authoring uses keys; the store resolves node and action ids.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class StoryEffect
{
    public Guid Id { get; set; }

    public StoryEffectKind Kind { get; set; }

    public int SortOrder { get; set; }

    public string? VariableKey { get; set; }

    public string? Value { get; set; }

    public string? TargetNodeKey { get; set; }

    public string? InvokeActionKey { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"StoryEffect(Kind={Kind}, Variable={VariableKey}, Target={TargetNodeKey})";
}