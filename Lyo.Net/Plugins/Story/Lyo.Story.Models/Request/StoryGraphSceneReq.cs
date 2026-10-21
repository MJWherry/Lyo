namespace Lyo.Story.Models.Request;

/// <summary>Scene inside a <see cref="StoryGraphReq" />. The store resolves <see cref="Key" /> to an id.</summary>
public sealed class StoryGraphSceneReq
{
    /// <summary>Stable key within the story.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Optional title.</summary>
    public string? Title { get; set; }

    /// <summary>Sort among scenes.</summary>
    public int SortOrder { get; set; }
}