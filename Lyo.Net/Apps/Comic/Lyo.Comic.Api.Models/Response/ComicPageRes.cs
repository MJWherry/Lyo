namespace Lyo.Comic.Api.Models.Response;

/// <summary>Response model for a single comic page.</summary>
public sealed record ComicPageRes
{
    public Guid Id { get; init; }
    public Guid ChapterId { get; init; }
    public int PageNumber { get; init; }
    public string? ImageRef { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
    public DateTime CreatedTimestamp { get; init; }
    public DateTime? UpdatedTimestamp { get; init; }

    /// <summary>Resolved URL for the page image. Populated when <see cref="ImageRef"/> is a valid file storage GUID.</summary>
    public string? ImageUrl => ImageRef != null && Guid.TryParse(ImageRef, out var id) ? $"/files/{id}" : null;
}
