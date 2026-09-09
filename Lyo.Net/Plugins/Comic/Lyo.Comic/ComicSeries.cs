using System.Diagnostics;
using Lyo.Comic.Enums;

namespace Lyo.Comic;

/// <summary>Model for a comic series (the top-level title, e.g. "One Piece", "Batman").</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ComicSeries
{
    /// <summary>Unique identifier of the series.</summary>
    public Guid Id { get; set; }

    /// <summary>Primary display title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Value of the URL-friendly slug, unique across all series.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Publication style and region of origin for this record.</summary>
    public ComicType ComicType { get; set; }

    /// <summary>Current publication status for this record.</summary>
    public ComicStatus Status { get; set; }

    /// <summary>Holds a description / synopsis of the series.</summary>
    public string? Description { get; set; }

    /// <summary>Value of the BCP 47 language tag for the original publication language (e.g. "ja", "en", "ko", "zh").</summary>
    public string? Language { get; set; }

    /// <summary>Year the series was first published for this record.</summary>
    public int? PublishedYear { get; set; }

    /// <summary>Holds the primary author or writer of the series.</summary>
    public string? Author { get; set; }

    /// <summary>Artist. May differ from <see cref="Author" /> in series where the writer and illustrator are separate people for this record.</summary>
    public string? Artist { get; set; }

    /// <summary>Original publisher (e.g. "Shueisha", "Viz Media", "Marvel").</summary>
    public string? Publisher { get; set; }

    /// <summary>Source URL or site name this series data was obtained from, used for attribution and citation for this record.</summary>
    public string? Source { get; set; }

    /// <summary>
    /// Optional field: a reference to the series cover image in file storage. Format is determined by the consuming application (e.g. a file storage key or URI).
    /// </summary>
    public string? CoverImageRef { get; set; }

    /// <summary>Holds the target demographic as a free-form label (e.g. "Shonen", "Seinen", "Josei", "Mature").</summary>
    public string? Demographic { get; set; }

    /// <summary>Clock time for when this record was created.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Recorded time of when this record was last updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <summary>Alternate and translated titles for this series.</summary>
    public IReadOnlyList<ComicAlternateTitle> AlternateTitles { get; set; } = [];

    /// <summary>Value: tags associated with this series (e.g. from search enrichment).</summary>
    public IReadOnlyList<string> Tags { get; set; } = [];

    /// <inheritdoc />
    public override string ToString() => $"ComicSeries(Id={Id}, Title=\"{Title}\", Slug=\"{Slug}\", Type={ComicType}, Status={Status})";
}