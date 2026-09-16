using Lyo.Story.Enums;

namespace Lyo.Story.Models.Response;

/// <summary>Reaction row as returned by the API.</summary>
public sealed class StoryReactionRes
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Owning story.</summary>
    public Guid StoryId { get; set; }

    /// <summary>Stable key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>When the reaction runs.</summary>
    public StoryTrigger Trigger { get; set; }

    /// <summary>Node scope.</summary>
    public Guid? ScopeNodeId { get; set; }

    /// <summary>Variable the trigger watches.</summary>
    public string? ScopeVariableKey { get; set; }

    /// <summary>When true, the reaction fires at most once per playthrough.</summary>
    public bool Once { get; set; }

    /// <summary>Serialized condition <c>WhereClause</c>.</summary>
    public string? WhenPredicateJson { get; set; }
}
