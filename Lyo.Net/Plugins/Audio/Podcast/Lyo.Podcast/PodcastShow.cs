using System.Diagnostics;
using Lyo.Podcast.Enums;

namespace Lyo.Podcast;

/// <summary>A podcast show. Episodes live on seasons. A show may have zero seasons. Transcripts are omitted from reads.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class PodcastShow
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Subtitle { get; set; }

    public string Slug { get; set; } = string.Empty;

    public PodcastShowStatus Status { get; set; }

    public string? Language { get; set; }

    public string? CoverImageRef { get; set; }

    public string? Overview { get; set; }

    public IReadOnlyList<Guid> GenreIds { get; set; } = [];

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public IReadOnlyList<PodcastCredit> Credits { get; set; } = [];

    public IReadOnlyList<PodcastIdentifier> Identifiers { get; set; } = [];

    public IReadOnlyList<PodcastSeason> Seasons { get; set; } = [];

    /// <inheritdoc />
    public override string ToString() => $"PodcastShow(Id={Id}, Title=\"{Title}\", Status={Status})";
}
