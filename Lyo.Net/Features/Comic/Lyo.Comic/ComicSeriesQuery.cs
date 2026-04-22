using Lyo.Comic.Enums;

namespace Lyo.Comic;

/// <summary>Filter parameters for searching comic series.</summary>
public sealed class ComicSeriesQuery
{
    /// <summary>Gets or sets a title substring to match against (case-insensitive). Matches both primary title and alternate titles.</summary>
    public string? TitleContains { get; set; }

    /// <summary>Gets or sets the comic type to filter by. Null means all types.</summary>
    public ComicType? ComicType { get; set; }

    /// <summary>Gets or sets the publication status to filter by. Null means all statuses.</summary>
    public ComicStatus? Status { get; set; }

    /// <summary>Gets or sets the BCP 47 original language to filter by (e.g. "ja", "en"). Null means all languages.</summary>
    public string? OriginalLanguage { get; set; }

    /// <summary>Gets or sets the maximum number of results to return. Null means no limit.</summary>
    public int? Limit { get; set; }

    /// <summary>Gets or sets the number of results to skip (for pagination).</summary>
    public int Skip { get; set; }
}
