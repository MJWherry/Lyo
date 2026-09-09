using System.Diagnostics;

namespace Lyo.Comic;

/// <summary>
/// Model for a collected volume of a comic series. Volumes are optional — many serialized series (especially manga) group chapters into volumes, while webtoons and some
/// Western titles sometimes skip volumes entirely.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ComicVolume
{
    /// <summary>Unique identifier of the volume.</summary>
    public Guid Id { get; set; }

    /// <summary>Series this volume belongs to for this record.</summary>
    public Guid SeriesId { get; set; }

    /// <summary>Volume number. Null when the series has no formal volume structure for this record.</summary>
    public decimal? VolumeNumber { get; set; }

    /// <summary>Volume title (e.g. "Volume 1: The Beginning"), if any for this record.</summary>
    public string? Title { get; set; }

    /// <summary>Holds a reference to the cover image in file storage. Format is determined by the consuming application (e.g. a file storage key or URI).</summary>
    public string? CoverImageRef { get; set; }

    /// <summary>Original publication date of this volume. Time portion is ignored; treat as date only for this record.</summary>
    public DateTime? PublishedDate { get; set; }

    /// <summary>Clock time for when this record was created.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Recorded time of when this record was last updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"ComicVolume(Id={Id}, SeriesId={SeriesId}, Volume={VolumeNumber?.ToString() ?? "null"}, Title=\"{Title ?? string.Empty}\")";
}