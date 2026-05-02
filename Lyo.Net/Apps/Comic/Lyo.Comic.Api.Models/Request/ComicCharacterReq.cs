namespace Lyo.Comic.Api.Models.Request;

/// <summary>Request model for creating or updating a comic character.</summary>
public sealed class ComicCharacterReq
{
    /// <summary>Gets or sets the series this character primarily belongs to.</summary>
    public Guid SeriesId { get; set; }

    /// <summary>Gets or sets the character's name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets a description or biography of the character.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets a reference to the character's image in file storage.</summary>
    public string? ImageRef { get; set; }

    /// <summary>Gets or sets the character's role (e.g. "Protagonist", "Antagonist", "Supporting", "Minor").</summary>
    public string? Role { get; set; }
}
