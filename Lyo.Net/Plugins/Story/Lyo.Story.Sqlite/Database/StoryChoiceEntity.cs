using System.Diagnostics;

namespace Lyo.Story.Sqlite.Database;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class StoryChoiceEntity
{
    public Guid Id { get; set; }

    public Guid NodeId { get; set; }

    public string Key { get; set; } = string.Empty;

    public string? Prompt { get; set; }

    public int SortOrder { get; set; }

    public Guid? TargetNodeId { get; set; }

    public string? VisibleWhenJson { get; set; }

    public string? EnabledWhenJson { get; set; }

    public StoryNodeEntity Node { get; set; } = null!;

    public StoryNodeEntity? TargetNode { get; set; }

    public ICollection<StoryEffectEntity> Effects { get; set; } = [];

    /// <summary>Type name and id.</summary>
    public override string ToString() => $"{GetType().Name} {Id}";
}