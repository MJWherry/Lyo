using System.Diagnostics;

namespace Lyo.Story.Sqlite.Database;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class StoryPlaythroughVariableEntity
{
    public Guid PlaythroughId { get; set; }

    public string VariableKey { get; set; } = string.Empty;

    public string? Value { get; set; }

    public DateTime UpdatedAt { get; set; }

    public StoryPlaythroughEntity Playthrough { get; set; } = null!;

    /// <summary>Type name and primary key.</summary>
    public override string ToString() => $"{GetType().Name} {PlaythroughId} {VariableKey}";
}