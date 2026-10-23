using System.Diagnostics;

namespace Lyo.Podcast;

/// <summary>An episode of a season. Episode number is unique per season. Transcripts are omitted from show reads.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class PodcastEpisode
{
    public Guid Id { get; set; }

    public Guid SeasonId { get; set; }

    public int EpisodeNumber { get; set; }

    public string Title { get; set; } = string.Empty;

    public int? RuntimeSeconds { get; set; }

    public DateTime? PublishedDate { get; set; }

    public string? AudioFileRef { get; set; }

    public string? Description { get; set; }

    /// <summary>Plain-text transcript. Omitted from show reads; use <see cref="IPodcastStore.SearchTranscriptsAsync" />.</summary>
    public string? Transcript { get; set; }

    public IReadOnlyList<PodcastCredit> Credits { get; set; } = [];

    public IReadOnlyList<PodcastIdentifier> Identifiers { get; set; } = [];

    /// <inheritdoc />
    public override string ToString() => $"PodcastEpisode(Number={EpisodeNumber}, Title=\"{Title}\")";
}