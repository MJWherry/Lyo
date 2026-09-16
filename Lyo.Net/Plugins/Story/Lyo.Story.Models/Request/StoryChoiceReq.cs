namespace Lyo.Story.Models.Request;

/// <summary>Create or update a choice row. Conditions are persisted <c>WhereClause</c> JSON.</summary>
public sealed class StoryChoiceReq
{
    /// <summary>Owning node.</summary>
    public Guid NodeId { get; set; }

    /// <summary>Stable key on the node.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Player-facing prompt.</summary>
    public string? Prompt { get; set; }

    /// <summary>Sort among choices on the node.</summary>
    public int SortOrder { get; set; }

    /// <summary>Node the choice moves to. Effects may also move the cursor.</summary>
    public Guid? TargetNodeId { get; set; }

    /// <summary>Serialized <c>WhereClause</c> for visibility. Null means always visible.</summary>
    public string? VisibleWhenJson { get; set; }

    /// <summary>Serialized <c>WhereClause</c> for whether the choice can be taken.</summary>
    public string? EnabledWhenJson { get; set; }
}
