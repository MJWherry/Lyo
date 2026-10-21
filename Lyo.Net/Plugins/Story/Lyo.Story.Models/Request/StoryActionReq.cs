namespace Lyo.Story.Models.Request;

/// <summary>Create or update a named action row.</summary>
public sealed class StoryActionReq
{
    /// <summary>Owning story.</summary>
    public Guid StoryId { get; set; }

    /// <summary>Stable key within the story.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Optional display name.</summary>
    public string? Name { get; set; }
}