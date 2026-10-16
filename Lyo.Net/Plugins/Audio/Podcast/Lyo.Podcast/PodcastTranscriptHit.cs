using System.Diagnostics;

namespace Lyo.Podcast;

/// <summary>An episode transcript full-text hit.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class PodcastTranscriptHit
{
    public Guid ShowId { get; set; }

    public Guid EpisodeId { get; set; }

    public string? Snippet { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"PodcastTranscriptHit(ShowId={ShowId}, EpisodeId={EpisodeId})";
}
