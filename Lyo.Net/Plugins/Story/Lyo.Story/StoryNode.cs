using System.Diagnostics;
using Lyo.Story.Enums;

namespace Lyo.Story;

/// <summary>A vertex in the play graph. Presentation fields are optional; a silent node is valid.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class StoryNode
{
    public Guid Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string? Title { get; set; }

    public string? SceneKey { get; set; }

    public bool IsEnding { get; set; }

    public StoryPresentationKind PresentationKind { get; set; }

    public string? Body { get; set; }

    public string? ContentType { get; set; }

    public string? MediaRef { get; set; }

    public IReadOnlyList<StoryChoice> Choices { get; set; } = [];

    /// <inheritdoc />
    public override string ToString() => $"StoryNode(Key=\"{Key}\", Ending={IsEnding}, Presentation={PresentationKind})";
}