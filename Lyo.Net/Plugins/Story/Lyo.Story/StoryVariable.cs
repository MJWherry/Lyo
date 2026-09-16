using System.Diagnostics;
using Lyo.Story.Enums;

namespace Lyo.Story;

/// <summary>A declared variable on a story. Defaults seed the playthrough bag at start.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class StoryVariable
{
    public Guid Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string? Name { get; set; }

    public StoryVariableType Type { get; set; }

    public string? DefaultValue { get; set; }

    public string? Description { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"StoryVariable(Key=\"{Key}\", Type={Type})";
}
