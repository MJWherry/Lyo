using Lyo.Query.Models.Common;

namespace Lyo.Story.Models.Request;

/// <summary>Choice inside a <see cref="StoryGraphNodeReq" />.</summary>
public sealed class StoryGraphChoiceReq
{
    /// <summary>Stable key on the node.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Player-facing prompt.</summary>
    public string? Prompt { get; set; }

    /// <summary>Sort among choices.</summary>
    public int SortOrder { get; set; }

    /// <summary>Node key to move to.</summary>
    public string? TargetNodeKey { get; set; }

    /// <summary>Visibility condition. Null means always visible.</summary>
    public WhereClause? VisibleWhen { get; set; }

    /// <summary>Enablement condition. Null means always enabled when visible.</summary>
    public WhereClause? EnabledWhen { get; set; }

    /// <summary>Response steps.</summary>
    public List<StoryGraphEffectReq> Effects { get; set; } = [];
}