using System.Diagnostics;
using Lyo.Google.Youtube.Models;

namespace Lyo.Video;

/// <summary>One episode of a season.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class VideoEpisode
{
    public Guid Id { get; set; }

    public Guid SeasonId { get; set; }

    public int EpisodeNumber { get; set; }

    public string Title { get; set; } = string.Empty;

    public int? RuntimeSeconds { get; set; }

    public DateTime? AirDate { get; set; }

    public string? VideoFileRef { get; set; }

    public string? StillImageRef { get; set; }

    /// <summary>Plain-text transcript. Omitted from title reads; use <see cref="IVideoStore.SearchTranscriptsAsync" />.</summary>
    public string? Transcript { get; set; }

    public YoutubeVideoLink? Youtube { get; set; }

    public IReadOnlyList<VideoCredit> Credits { get; set; } = [];

    public IReadOnlyList<VideoIdentifier> Identifiers { get; set; } = [];

    /// <inheritdoc />
    public override string ToString() => $"VideoEpisode(Number={EpisodeNumber}, Title=\"{Title}\")";
}
