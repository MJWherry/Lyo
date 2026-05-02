using Lyo.Comic.Enums;

namespace Lyo.Comic.Api.Models.Response;

/// <summary>Enriched series response combining comic domain data with cross-domain metadata (tags, ratings, comments, favorites).</summary>
public sealed record ComicSeriesRes
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public ComicType ComicType { get; init; }
    public ComicStatus Status { get; init; }
    public string? Description { get; init; }
    public string? Language { get; init; }
    public int? PublishedYear { get; init; }
    public string? Author { get; init; }
    public string? Artist { get; init; }
    public string? Publisher { get; init; }
    public string? Source { get; init; }
    public string? CoverImageRef { get; init; }
    public string? Demographic { get; init; }
    public DateTime CreatedTimestamp { get; init; }
    public DateTime? UpdatedTimestamp { get; init; }
    public IReadOnlyList<ComicAlternateTitleRes> AlternateTitles { get; init; } = [];

    /// <summary>Tags applied to this series.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>Average rating value across all ratings for this series, or null if no ratings exist.</summary>
    public decimal? AverageRating { get; init; }

    /// <summary>Total number of ratings for this series.</summary>
    public int RatingCount { get; init; }

    /// <summary>Total number of comments for this series.</summary>
    public int CommentCount { get; init; }

    /// <summary>Total number of users who have favorited this series.</summary>
    public int FavoriteCount { get; init; }

    /// <summary>Whether the requesting user has favorited this series. Null when the caller is anonymous.</summary>
    public bool? IsFavorited { get; init; }

    /// <summary>Resolved URL for the series cover image. Populated when <see cref="CoverImageRef"/> is a valid file storage GUID.</summary>
    public string? CoverImageUrl => CoverImageRef != null && Guid.TryParse(CoverImageRef, out var id) ? $"/files/{id}" : null;
}

/// <summary>An alternate or translated title within a series response.</summary>
public sealed record ComicAlternateTitleRes
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Language { get; init; }
}
