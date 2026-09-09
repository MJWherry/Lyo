using System.Diagnostics;

namespace Lyo.Comic;

/// <summary>
/// Model for a single chapter of a comic series in a specific language. Chapter numbers use decimal to accommodate half-chapters (e.g. 10.5). A series may have the same
/// chapter number across languages; each pair is its own row so availability can be tracked per language.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ComicChapter
{
    /// <summary>Unique identifier of the chapter for this record.</summary>
    public Guid Id { get; set; }

    /// <summary>Holds the series this chapter belongs to.</summary>
    public Guid SeriesId { get; set; }

    /// <summary>Value of the volume this chapter belongs to, if any.</summary>
    public Guid? VolumeId { get; set; }

    /// <summary>Chapter number. Decimal to support half-chapters (e.g. 10.5) for this record.</summary>
    public decimal ChapterNumber { get; set; }

    /// <summary>Holds the chapter title, if the chapter has one.</summary>
    public string? Title { get; set; }

    /// <summary>Value of the BCP 47 language tag for this chapter (e.g. "en", "ja", "ko").</summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>Number of pages in this chapter.</summary>
    public int? PageCount { get; set; }

    /// <summary>Value of the original release date of this chapter. Time portion is ignored; treat as date only.</summary>
    public DateTime? PublishedDate { get; set; }

    /// <summary>
    /// An opaque reference to the source this chapter was ingested from (e.g. a scraper site identifier, URL, or external ID). Null for internally created records.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>An opaque reference to the chapter cover image (e.g. file storage id) stored here.</summary>
    public string? CoverImageRef { get; set; }

    /// <summary>Clock time for when this record was created.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Recorded time of when this record was last updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"ComicChapter(Id={Id}, SeriesId={SeriesId}, Chapter={ChapterNumber}, Language=\"{Language}\", Title=\"{Title ?? string.Empty}\")";
}