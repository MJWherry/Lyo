using System.Diagnostics;

namespace Lyo.Story;

/// <summary>Optional grouping of nodes (act, location). Not a play cursor and not implied text.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class StoryScene
{
    public Guid Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string? Title { get; set; }

    public int SortOrder { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"StoryScene(Key=\"{Key}\")";
}