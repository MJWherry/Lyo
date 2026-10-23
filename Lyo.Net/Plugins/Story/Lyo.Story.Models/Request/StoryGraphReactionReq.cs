using Lyo.Query.Models.Common;
using Lyo.Story.Enums;

namespace Lyo.Story.Models.Request;

/// <summary>Reaction inside a <see cref="StoryGraphReq" />.</summary>
public sealed class StoryGraphReactionReq
{
    /// <summary>Stable key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>When the reaction runs.</summary>
    public StoryTrigger Trigger { get; set; }

    /// <summary>Node key scope. Null means the whole story.</summary>
    public string? ScopeNodeKey { get; set; }

    /// <summary>Variable the trigger watches.</summary>
    public string? ScopeVariableKey { get; set; }

    /// <summary>When true, the reaction fires at most once per playthrough.</summary>
    public bool Once { get; set; }

    /// <summary>Condition. Null means always.</summary>
    public WhereClause? When { get; set; }

    /// <summary>Response steps.</summary>
    public List<StoryGraphEffectReq> Effects { get; set; } = [];
}