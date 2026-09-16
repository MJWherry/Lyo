using Lyo.Story.Enums;

namespace Lyo.Story.Models.Response;

/// <summary>A choice or Advance the player may take.</summary>
public sealed class StoryMoveRes
{
    /// <summary>Choice or advance.</summary>
    public StoryMoveKind Kind { get; set; }

    /// <summary>Choice key when <see cref="Kind" /> is a choice.</summary>
    public string? ChoiceKey { get; set; }

    /// <summary>Player-facing prompt.</summary>
    public string? Prompt { get; set; }

    /// <summary>False when the choice is visible but its condition fails.</summary>
    public bool Enabled { get; set; } = true;
}
