using Lyo.Query.Models.Common;
using Lyo.Story.Enums;

namespace Lyo.Story.Models.Response;

/// <summary>Reaction inside a <see cref="StoryGraphRes" />.</summary>
public sealed class StoryGraphReactionRes
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Stable key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>When the reaction runs.</summary>
    public StoryTrigger Trigger { get; set; }

    /// <summary>Node key scope.</summary>
    public string? ScopeNodeKey { get; set; }

    /// <summary>Variable the trigger watches.</summary>
    public string? ScopeVariableKey { get; set; }

    /// <summary>When true, the reaction fires at most once per playthrough.</summary>
    public bool Once { get; set; }

    /// <summary>Condition.</summary>
    public WhereClause? When { get; set; }

    /// <summary>Response steps.</summary>
    public IReadOnlyList<StoryGraphEffectRes> Effects { get; set; } = [];
}