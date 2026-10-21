using Lyo.Music.Enums;

namespace Lyo.Music.Postgres.Database;

public sealed class MusicReleaseGroupEntity
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public MusicReleaseKind Kind { get; set; }

    public MusicReleaseStatus Status { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public ICollection<MusicReleaseEntity> Releases { get; set; } = [];
}