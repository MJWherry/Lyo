namespace Lyo.Comic.Api.Models.Request;

/// <summary>Request model for creating or updating a comic chapter.</summary>
public sealed class ComicChapterReq
{
    /// <summary>Gets or sets the series this chapter belongs to.</summary>
    public Guid SeriesId { get; set; }

    /// <summary>Gets or sets the volume this chapter belongs to, if any.</summary>
    public Guid? VolumeId { get; set; }

    /// <summary>Gets or sets the chapter number. Decimal to support half-chapters (e.g. 10.5).</summary>
    public decimal ChapterNumber { get; set; }

    /// <summary>Gets or sets the chapter title, if the chapter has one.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the BCP 47 language tag for this chapter (e.g. "en", "ja").</summary>
    public string Language { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of pages in this chapter.</summary>
    public int? PageCount { get; set; }

    /// <summary>Gets or sets the original release date of this chapter.</summary>
    public DateTime? PublishedDate { get; set; }

    /// <summary>Gets or sets an opaque reference to the source this chapter was ingested from.</summary>
    public string? Source { get; set; }

    /// <summary>Gets or sets an opaque reference to the chapter cover image (e.g. file storage id).</summary>
    public string? CoverImageRef { get; set; }
}