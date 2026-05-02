namespace Lyo.Comic.Api.Models.Response;

/// <summary>Enriched volume response combining comic domain data with cross-domain metadata (tags, ratings, comments).</summary>
public sealed record ComicVolumeRes
{
    public Guid Id { get; init; }
    public Guid SeriesId { get; init; }
    public decimal? VolumeNumber { get; init; }
    public string? Title { get; init; }
    public string? CoverImageRef { get; init; }
    public DateTime? PublishedDate { get; init; }
    public DateTime CreatedTimestamp { get; init; }
    public DateTime? UpdatedTimestamp { get; init; }

    /// <summary>Tags applied to this volume.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>Average rating value across all ratings for this volume, or null if no ratings exist.</summary>
    public decimal? AverageRating { get; init; }

    /// <summary>Total number of ratings for this volume.</summary>
    public int RatingCount { get; init; }

    /// <summary>Total number of comments for this volume.</summary>
    public int CommentCount { get; init; }

    /// <summary>Total number of users who have favorited this volume.</summary>
    public int FavoriteCount { get; init; }

    /// <summary>Whether the requesting user has favorited this volume. Null when the caller is anonymous.</summary>
    public bool? IsFavorited { get; init; }

    /// <summary>Resolved URL for the volume cover image. Populated when <see cref="CoverImageRef"/> is a valid file storage GUID.</summary>
    public string? CoverImageUrl => CoverImageRef != null && Guid.TryParse(CoverImageRef, out var id) ? $"/files/{id}" : null;
}
