using System.Diagnostics;

namespace Lyo.Video;

/// <summary>A transcript full-text hit. <see cref="EpisodeId" /> is set for an episode and null for a movie.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class VideoTranscriptHit
{
    public Guid TitleId { get; set; }

    public Guid? EpisodeId { get; set; }

    public string? Snippet { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"VideoTranscriptHit(TitleId={TitleId}, EpisodeId={EpisodeId})";
}
