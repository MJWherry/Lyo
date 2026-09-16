namespace Lyo.Story.Models.Response;

/// <summary>Scene inside a <see cref="StoryGraphRes" />.</summary>
public sealed class StoryGraphSceneRes
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Stable key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Optional title.</summary>
    public string? Title { get; set; }

    /// <summary>Sort among scenes.</summary>
    public int SortOrder { get; set; }
}
