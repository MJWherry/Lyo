using System.Diagnostics;

namespace Lyo.Music;

/// <summary>One position on a release. <see cref="AudioFileRef" /> is an opaque file ref, not stored bytes.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class MusicTrack
{
    public Guid Id { get; set; }

    public Guid ReleaseId { get; set; }

    public int DiscNumber { get; set; } = 1;

    public int TrackNumber { get; set; }

    public string Title { get; set; } = string.Empty;

    public int? DurationSeconds { get; set; }

    /// <summary>ISRC, stored normalized (uppercase, hyphens removed). Unique when set.</summary>
    public string? Isrc { get; set; }

    public string? AudioFileRef { get; set; }

    public Guid? RecordingId { get; set; }

    /// <summary>Plain-text lyrics. Omitted from release reads; use <see cref="IMusicStore.SearchLyricsAsync" />.</summary>
    public string? Lyrics { get; set; }

    public IReadOnlyList<MusicCredit> Credits { get; set; } = [];

    /// <inheritdoc />
    public override string ToString() => $"MusicTrack(Disc={DiscNumber}, Number={TrackNumber}, Title=\"{Title}\")";
}
