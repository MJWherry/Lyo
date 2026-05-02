namespace Lyo.Comic.Api.Models.Request;

/// <summary>Request model for creating or updating a comic volume.</summary>
public sealed class ComicVolumeReq
{
    /// <summary>Gets or sets the series this volume belongs to.</summary>
    public Guid SeriesId { get; set; }

    /// <summary>Gets or sets the volume number. Null when the series has no formal volume structure.</summary>
    public decimal? VolumeNumber { get; set; }

    /// <summary>Gets or sets the volume title, if any.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets a reference to the cover image in file storage.</summary>
    public string? CoverImageRef { get; set; }

    /// <summary>Gets or sets the original publication date of this volume.</summary>
    public DateTime? PublishedDate { get; set; }
}
