using System.Diagnostics;
using Lyo.Google.Youtube.Models;
using Lyo.Video.Enums;

namespace Lyo.Video;

/// <summary>A movie or a television series. A movie has runtime and no seasons.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class VideoTitle
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public VideoTitleKind Kind { get; set; }

    public VideoSeriesStatus Status { get; set; }

    public int? Year { get; set; }

    public string? Language { get; set; }

    public int? RuntimeSeconds { get; set; }

    public string? CoverImageRef { get; set; }

    public string? VideoFileRef { get; set; }

    public string? Overview { get; set; }

    /// <summary>Plain-text transcript for a movie. Omitted from title reads; use <see cref="IVideoStore.SearchTranscriptsAsync" />.</summary>
    public string? Transcript { get; set; }

    public IReadOnlyList<Guid> GenreIds { get; set; } = [];

    public IReadOnlyList<VideoAlternateTitle> AlternateTitles { get; set; } = [];

    public Guid? CollectionId { get; set; }

    public YoutubeVideoLink? Youtube { get; set; }

    public IReadOnlyList<VideoCut> Cuts { get; set; } = [];

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public IReadOnlyList<VideoCredit> Credits { get; set; } = [];

    public IReadOnlyList<VideoIdentifier> Identifiers { get; set; } = [];

    public IReadOnlyList<VideoSeason> Seasons { get; set; } = [];

    /// <inheritdoc />
    public override string ToString() => $"VideoTitle(Id={Id}, Title=\"{Title}\", Kind={Kind})";
}