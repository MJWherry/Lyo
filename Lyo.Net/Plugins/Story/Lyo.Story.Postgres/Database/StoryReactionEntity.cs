using System.Diagnostics;
using Lyo.Story.Enums;

namespace Lyo.Story.Postgres.Database;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class StoryReactionEntity
{
    public Guid Id { get; set; }

    public Guid StoryId { get; set; }

    public string Key { get; set; } = string.Empty;

    public StoryTrigger Trigger { get; set; }

    public Guid? ScopeNodeId { get; set; }

    public string? ScopeVariableKey { get; set; }

    public bool Once { get; set; }

    public string? WhenPredicateJson { get; set; }

    public StoryEntity Story { get; set; } = null!;

    public StoryNodeEntity? ScopeNode { get; set; }

    public ICollection<StoryEffectEntity> Effects { get; set; } = [];

    /// <summary>Type name and id.</summary>
    public override string ToString() => $"{GetType().Name} {Id}";
}