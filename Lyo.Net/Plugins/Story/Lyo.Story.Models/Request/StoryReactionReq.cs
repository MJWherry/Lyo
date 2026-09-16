using Lyo.Story.Enums;

namespace Lyo.Story.Models.Request;

/// <summary>Create or update a reaction row.</summary>
public sealed class StoryReactionReq
{
    /// <summary>Owning story.</summary>
    public Guid StoryId { get; set; }

    /// <summary>Stable key within the story.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>When the reaction runs.</summary>
    public StoryTrigger Trigger { get; set; }

    /// <summary>Node scope. Null means the whole story.</summary>
    public Guid? ScopeNodeId { get; set; }

    /// <summary>Variable the trigger watches, when the trigger is variable-shaped.</summary>
    public string? ScopeVariableKey { get; set; }

    /// <summary>When true, the reaction fires at most once per playthrough.</summary>
    public bool Once { get; set; }

    /// <summary>Serialized <c>WhereClause</c>. Null means always.</summary>
    public string? WhenPredicateJson { get; set; }
}
