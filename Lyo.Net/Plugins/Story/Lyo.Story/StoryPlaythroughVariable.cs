using System.Diagnostics;

namespace Lyo.Story;

/// <summary>Current value of one variable on a playthrough.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class StoryPlaythroughVariable
{
    public string VariableKey { get; set; } = string.Empty;

    public string? Value { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"StoryPlaythroughVariable({VariableKey}={Value})";
}