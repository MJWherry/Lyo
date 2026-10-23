using System.Diagnostics;

namespace Lyo.Story.Sqlite.Database;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class StorySceneEntity
{
    public Guid Id { get; set; }

    public Guid StoryId { get; set; }

    public string Key { get; set; } = string.Empty;

    public string? Title { get; set; }

    public int SortOrder { get; set; }

    public StoryEntity Story { get; set; } = null!;

    public ICollection<StoryNodeEntity> Nodes { get; set; } = [];

    /// <summary>Type name, id, and title.</summary>
    public override string ToString() => $"{GetType().Name} {Id} {Title}";
}