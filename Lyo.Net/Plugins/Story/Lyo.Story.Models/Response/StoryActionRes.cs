namespace Lyo.Story.Models.Response;

/// <summary>Action row as returned by the API.</summary>
public sealed class StoryActionRes
{
    /// <summary>Row id.</summary>
    public Guid Id { get; set; }

    /// <summary>Owning story.</summary>
    public Guid StoryId { get; set; }

    /// <summary>Stable key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Optional display name.</summary>
    public string? Name { get; set; }
}
