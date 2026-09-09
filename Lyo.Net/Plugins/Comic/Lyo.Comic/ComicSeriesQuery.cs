using Lyo.Comic.Enums;

namespace Lyo.Comic;

/// <summary>Searching comic series filter parameters.</summary>
public sealed class ComicSeriesQuery
{
    /// <summary>Holds a title substring to match against (case-insensitive). Matches both primary title and alternate titles.</summary>
    public string? TitleContains { get; set; }

    /// <summary>Value of the comic type to filter by. Null means all types.</summary>
    public ComicType? ComicType { get; set; }

    /// <summary>Publication status to filter by. Null means all statuses.</summary>
    public ComicStatus? Status { get; set; }

    /// <summary>Holds the BCP 47 language to filter by (e.g. "ja", "en"). Null means all languages.</summary>
    public string? Language { get; set; }

    /// <summary>Value: tag strings to filter by. Only series that have ALL specified tags are returned. Null or empty means no tag filter.</summary>
    public IReadOnlyList<string>? Tags { get; set; }

    /// <summary>
    /// An explicit set of series IDs to restrict results to. Populated internally by the API after resolving <see cref="Tags" /> — do not set directly stored here.
    /// </summary>
    public IReadOnlyList<Guid>? FilterSeriesIds { get; set; }

    /// <summary>Maximum number of results to return. Null means no limit for this record.</summary>
    public int? Limit { get; set; }

    /// <summary>Number of results to skip (for pagination) for this record.</summary>
    public int Skip { get; set; }
}