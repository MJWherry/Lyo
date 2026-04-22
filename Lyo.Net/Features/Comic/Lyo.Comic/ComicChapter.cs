using System.Diagnostics;

namespace Lyo.Comic;

/// <summary>
/// Represents a single chapter of a comic series in a specific language. Chapter numbers use decimal to accommodate half-chapters (e.g. 10.5). A series may have the same
/// chapter number in multiple languages; each combination is a distinct record so the store can track per-language availability independently.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ComicChapter
{
    /// <summary>Gets or sets the unique identifier of the chapter.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the series this chapter belongs to.</summary>
    public Guid SeriesId { get; set; }

    /// <summary>Gets or sets the volume this chapter belongs to, if any.</summary>
    public Guid? VolumeId { get; set; }

    /// <summary>Gets or sets the chapter number. Decimal to support half-chapters (e.g. 10.5).</summary>
    public decimal ChapterNumber { get; set; }

    /// <summary>Gets or sets the chapter title, if the chapter has one.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the BCP 47 language tag for this chapter (e.g. "en", "ja", "ko").</summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of pages in this chapter.</summary>
    public int? PageCount { get; set; }

    /// <summary>Gets or sets the original release date of this chapter. Time portion is ignored; treat as date only.</summary>
    public DateTime? PublishedDate { get; set; }

    /// <summary>Gets or sets an opaque reference to the source this chapter was ingested from (e.g. a scraper site identifier, URL, or external ID). Null for internally created records.</summary>
    public string? SourceRef { get; set; }

    /// <summary>Gets or sets when this record was created.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Gets or sets when this record was last updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"ComicChapter(Id={Id}, SeriesId={SeriesId}, Chapter={ChapterNumber}, Language=\"{Language}\", Title=\"{Title ?? string.Empty}\")";
}