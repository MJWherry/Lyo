using System.Diagnostics;
using Lyo.Query.Models.Common;

namespace Lyo.Story;

/// <summary>A player-facing option on a node. Prompt and responses are both optional.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class StoryChoice
{
    public Guid Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string? Prompt { get; set; }

    public int SortOrder { get; set; }

    public string? TargetNodeKey { get; set; }

    public WhereClause? VisibleWhen { get; set; }

    public WhereClause? EnabledWhen { get; set; }

    public IReadOnlyList<StoryEffect> Effects { get; set; } = [];

    /// <inheritdoc />
    public override string ToString() => $"StoryChoice(Key=\"{Key}\")";
}
