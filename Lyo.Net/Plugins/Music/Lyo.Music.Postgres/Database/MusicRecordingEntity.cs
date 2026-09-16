namespace Lyo.Music.Postgres.Database;

public sealed class MusicRecordingEntity
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public ICollection<MusicTrackEntity> Tracks { get; set; } = [];
}
