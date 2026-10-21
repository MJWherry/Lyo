namespace Lyo.Story.Models.Response;

/// <summary>Scene row as returned by the API.</summary>
public sealed class StorySceneRes
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Owning story.</summary>
    public Guid StoryId { get; set; }

    /// <summary>Stable key within the story.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Optional title.</summary>
    public string? Title { get; set; }

    /// <summary>Sort among scenes.</summary>
    public int SortOrder { get; set; }
}