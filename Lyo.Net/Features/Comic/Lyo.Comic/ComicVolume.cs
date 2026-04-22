using System.Diagnostics;

namespace Lyo.Comic;

/// <summary>
/// Represents a collected volume of a comic series.
/// Volumes are optional — many serialized series (especially manga) group chapters into volumes,
/// while webtoons and some Western comics may not use volumes at all.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class ComicVolume
{
    /// <summary>Gets or sets the unique identifier of the volume.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the series this volume belongs to.</summary>
    public Guid SeriesId { get; set; }

    /// <summary>Gets or sets the volume number. Null when the series has no formal volume structure.</summary>
    public decimal? VolumeNumber { get; set; }

    /// <summary>Gets or sets the volume title (e.g. "Volume 1: The Beginning"), if any.</summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets a reference to the cover image in file storage.
    /// Format is determined by the consuming application (e.g. a file storage key or URI).
    /// </summary>
    public string? CoverImageRef { get; set; }

    /// <summary>Gets or sets the original publication date of this volume. Time portion is ignored; treat as date only.</summary>
    public DateTime? PublishedDate { get; set; }

    /// <summary>Gets or sets when this record was created.</summary>
    public DateTime CreatedTimestamp { get; set; }

    /// <summary>Gets or sets when this record was last updated.</summary>
    public DateTime? UpdatedTimestamp { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"ComicVolume(Id={Id}, SeriesId={SeriesId}, Volume={VolumeNumber?.ToString() ?? "null"}, Title=\"{Title ?? string.Empty}\")";
}
