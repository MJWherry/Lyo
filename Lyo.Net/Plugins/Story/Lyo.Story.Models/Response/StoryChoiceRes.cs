namespace Lyo.Story.Models.Response;

/// <summary>Choice row as returned by the API.</summary>
public sealed class StoryChoiceRes
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Owning node.</summary>
    public Guid NodeId { get; set; }

    /// <summary>Stable key on the node.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Player-facing prompt.</summary>
    public string? Prompt { get; set; }

    /// <summary>Sort among choices.</summary>
    public int SortOrder { get; set; }

    /// <summary>Node the choice moves to.</summary>
    public Guid? TargetNodeId { get; set; }

    /// <summary>Serialized visibility <c>WhereClause</c>.</summary>
    public string? VisibleWhenJson { get; set; }

    /// <summary>Serialized enablement <c>WhereClause</c>.</summary>
    public string? EnabledWhenJson { get; set; }
}
