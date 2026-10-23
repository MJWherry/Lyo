using System.Diagnostics;

namespace Lyo.Story;

/// <summary>A named reusable response bundle, invoked from a choice, reaction, or another action.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class StoryAction
{
    public Guid Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string? Name { get; set; }

    public IReadOnlyList<StoryEffect> Effects { get; set; } = [];

    /// <inheritdoc />
    public override string ToString() => $"StoryAction(Key=\"{Key}\")";
}