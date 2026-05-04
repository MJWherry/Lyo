using System.Diagnostics;
using Lyo.Comic.Enums;

namespace Lyo.Comic;

/// <summary>Represents a comic series (the top-level title, e.g. "One Piece", "Batman").</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ComicSeries
{
    /// <summary>Gets or sets the unique identifier of the series.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the primary display title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the URL-friendly slug, unique across all series.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets or sets the publication style and region of origin.</summary>
    public ComicType ComicType { get; set; }

    /// <summary>Gets or sets the current publication status.</summary>
    public ComicStatus Status { get; set; }

    /// <summary>Gets or sets a description / synopsis of the series.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the BCP 47 language tag for the original publication language (e.g. "ja", "en", "ko", "zh").</summary>
    public string? Language { get; set; }

    /// <summary>Gets or sets the year the series was first published.</summary>
    public int? PublishedYear { get; set; }

    /// <summary>Gets or sets the primary author or writer of the series.</summary>
    public string? Author { get; set; }

    /// <summary>Gets or sets the artist. May differ from <see cref="Author" /> in series where the writer and illustrator are separate people.</summary>
    public string? Artist { get; set; }

    /// <summary>Gets or sets the original publisher (e.g. "Shueisha", "Viz Media", "Marvel").</summary>
    public string? Publisher { get; set; }

    /// <summary>Gets or sets the source URL or site name this series data was obtained from, used for attribution and citation.</summary>
    public string? Source { get; set; }

    /// <summary>Gets or sets a reference to the series cover image in file storage. Format is determined by the consuming application (e.g. a file storage key or URI).</summary>
    public string? CoverImageRef { get; set; }

    /// <summary>Gets or sets the target demographic as a free-form label (e.g. "Shonen", "Seinen", "Josei", "Mature").</summary>
    public string? Demographic { get; set; }

    /// <summary>Gets or sets when this record was created.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Gets or sets when this record was last updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <summary>Gets or sets the alternate and translated titles for this series.</summary>
    public IReadOnlyList<ComicAlternateTitle> AlternateTitles { get; set; } = [];

    /// <summary>Gets or sets tags associated with this series (e.g. from search enrichment).</summary>
    public IReadOnlyList<string> Tags { get; set; } = [];

    /// <inheritdoc />
    public override string ToString() => $"ComicSeries(Id={Id}, Title=\"{Title}\", Slug=\"{Slug}\", Type={ComicType}, Status={Status})";
}