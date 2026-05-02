namespace Lyo.Comic.Api.Models.Response;

/// <summary>Response model for a comic character.</summary>
public sealed record ComicCharacterRes
{
    public Guid Id { get; init; }
    public Guid SeriesId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ImageRef { get; init; }
    public string? Role { get; init; }
    public DateTime CreatedTimestamp { get; init; }
    public DateTime? UpdatedTimestamp { get; init; }
}
