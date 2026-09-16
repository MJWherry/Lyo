using System.Diagnostics;
using Lyo.Story.Enums;

namespace Lyo.Story;

/// <summary>A choice or Advance the player may take at the current node.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class StoryMove
{
    public StoryMoveKind Kind { get; set; }

    public string? ChoiceKey { get; set; }

    public string? Prompt { get; set; }

    public bool Enabled { get; set; } = true;

    /// <inheritdoc />
    public override string ToString() => Kind == StoryMoveKind.Advance ? "StoryMove(Advance)" : $"StoryMove(Choice={ChoiceKey}, Enabled={Enabled})";
}
