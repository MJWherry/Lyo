using Lyo.Query.Models.Common;

namespace Lyo.Story.Models.Response;

/// <summary>Choice inside a <see cref="StoryGraphNodeRes" />.</summary>
public sealed class StoryGraphChoiceRes
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Stable key on the node.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Player-facing prompt.</summary>
    public string? Prompt { get; set; }

    /// <summary>Sort among choices.</summary>
    public int SortOrder { get; set; }

    /// <summary>Node key to move to.</summary>
    public string? TargetNodeKey { get; set; }

    /// <summary>Visibility condition.</summary>
    public WhereClause? VisibleWhen { get; set; }

    /// <summary>Enablement condition.</summary>
    public WhereClause? EnabledWhen { get; set; }

    /// <summary>Response steps.</summary>
    public IReadOnlyList<StoryGraphEffectRes> Effects { get; set; } = [];
}
