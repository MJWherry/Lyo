using System.Diagnostics;

namespace Lyo.Music.Postgres.Database;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class MusicRecordingEntity
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public ICollection<MusicTrackEntity> Tracks { get; set; } = [];

    /// <summary>Type name, id, and title.</summary>
    public override string ToString() => $"{GetType().Name} {Id} {Title}";
}