using System.Diagnostics;

namespace Lyo.Story.Postgres.Database;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class StoryActionEntity
{
    public Guid Id { get; set; }

    public Guid StoryId { get; set; }

    public string Key { get; set; } = string.Empty;

    public string? Name { get; set; }

    public StoryEntity Story { get; set; } = null!;

    public ICollection<StoryEffectEntity> Effects { get; set; } = [];

    /// <summary>Type name, id, and name.</summary>
    public override string ToString() => $"{GetType().Name} {Id} {Name}";
}