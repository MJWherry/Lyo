using System.Diagnostics;
using System.Text.Json.Serialization;
using NpgsqlTypes;

namespace Lyo.Music.Postgres.Database;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class MusicTrackEntity
{
    public Guid Id { get; set; }

    public Guid ReleaseId { get; set; }

    public int DiscNumber { get; set; }

    public int TrackNumber { get; set; }

    public string Title { get; set; } = string.Empty;

    public int? DurationSeconds { get; set; }

    public string? Isrc { get; set; }

    public string? AudioFileRef { get; set; }

    public Guid? RecordingId { get; set; }

    public MusicRecordingEntity? Recording { get; set; }

    [JsonIgnore]
    public string? Lyrics { get; set; }

    [JsonIgnore]
    public NpgsqlTsVector SearchVector { get; set; } = null!;

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public MusicReleaseEntity? Release { get; set; }

    public ICollection<MusicCreditEntity> Credits { get; set; } = [];

    /// <summary>Type name, id, and title.</summary>
    public override string ToString() => $"{GetType().Name} {Id} {Title}";
}