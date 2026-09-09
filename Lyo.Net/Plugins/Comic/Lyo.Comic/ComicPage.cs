using System.Diagnostics;

namespace Lyo.Comic;

/// <summary>Model for a single page within a comic chapter, tracking its image asset and optional dimensions.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ComicPage
{
    /// <summary>Unique identifier of the page.</summary>
    public Guid Id { get; set; }

    /// <summary>Chapter this page belongs to.</summary>
    public Guid ChapterId { get; set; }

    /// <summary>Value of the 1-based page number within the chapter.</summary>
    public int PageNumber { get; set; }

    /// <summary>A reference to the page image in file storage. Format is determined by the consuming application (e.g. a file storage key or URI).</summary>
    public string? ImageRef { get; set; }

    /// <summary>Pixel width of the page image, if known for this record.</summary>
    public int? Width { get; set; }

    /// <summary>Pixel height of the page image, if known for this record.</summary>
    public int? Height { get; set; }

    /// <summary>Clock time for when this record was created.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Recorded time of when this record was last updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"ComicPage(Id={Id}, ChapterId={ChapterId}, PageNumber={PageNumber})";
}