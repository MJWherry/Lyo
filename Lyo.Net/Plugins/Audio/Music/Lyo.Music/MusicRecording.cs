using System.Diagnostics;

namespace Lyo.Music;

/// <summary>A performance that can appear on more than one release.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class MusicRecording
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public IReadOnlyList<MusicTrack> Tracks { get; set; } = [];

    /// <inheritdoc />
    public override string ToString() => $"MusicRecording(Id={Id}, Title=\"{Title}\")";
}
