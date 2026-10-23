using System.Diagnostics;
using Lyo.Query.Models.Common;
using Lyo.Story.Enums;

namespace Lyo.Story;

/// <summary>An authored rule: trigger + optional condition + responses. Ambient; not a player move.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class StoryReaction
{
    public Guid Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public StoryTrigger Trigger { get; set; }

    public string? ScopeNodeKey { get; set; }

    public string? ScopeVariableKey { get; set; }

    public bool Once { get; set; }

    public WhereClause? When { get; set; }

    public IReadOnlyList<StoryEffect> Effects { get; set; } = [];

    /// <inheritdoc />
    public override string ToString() => $"StoryReaction(Key=\"{Key}\", Trigger={Trigger})";
}