namespace Lyo.Story.Models.Request;

/// <summary>Create or update a scene row.</summary>
public sealed class StorySceneReq
{
    /// <summary>Owning story.</summary>
    public Guid StoryId { get; set; }

    /// <summary>Stable key within the story.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Optional title.</summary>
    public string? Title { get; set; }

    /// <summary>Sort among scenes.</summary>
    public int SortOrder { get; set; }
}