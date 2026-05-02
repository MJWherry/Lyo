using System.Diagnostics;

namespace Lyo.Comic;

/// <summary>Represents a single page within a comic chapter, tracking its image asset and optional dimensions.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ComicPage
{
    /// <summary>Gets or sets the unique identifier of the page.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the chapter this page belongs to.</summary>
    public Guid ChapterId { get; set; }

    /// <summary>Gets or sets the 1-based page number within the chapter.</summary>
    public int PageNumber { get; set; }

    /// <summary>Gets or sets a reference to the page image in file storage. Format is determined by the consuming application (e.g. a file storage key or URI).</summary>
    public string? ImageRef { get; set; }

    /// <summary>Gets or sets the pixel width of the page image, if known.</summary>
    public int? Width { get; set; }

    /// <summary>Gets or sets the pixel height of the page image, if known.</summary>
    public int? Height { get; set; }

    /// <summary>Gets or sets when this record was created.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Gets or sets when this record was last updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"ComicPage(Id={Id}, ChapterId={ChapterId}, PageNumber={PageNumber})";
}