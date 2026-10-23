using System.Diagnostics;

namespace Lyo.Music;

/// <summary>A lyrics full-text hit. The snippet is a headline, not the full lyrics.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class MusicLyricsHit
{
    public Guid TrackId { get; set; }

    public Guid ReleaseId { get; set; }

    public string? Snippet { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"MusicLyricsHit(TrackId={TrackId}, ReleaseId={ReleaseId})";
}