using System.Diagnostics;
using Lyo.Music.Enums;

namespace Lyo.Music.Postgres.Database;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class MusicReleaseEntity
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public Guid? ReleaseGroupId { get; set; }

    public MusicReleaseKind Kind { get; set; }

    public MusicReleaseStatus Status { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    public string? Label { get; set; }

    public string? Language { get; set; }

    public string? CoverImageRef { get; set; }

    public string? PrimaryArtist { get; set; }

    public Guid? FormatId { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public MusicReleaseGroupEntity? ReleaseGroup { get; set; }

    public ICollection<MusicIdentifierEntity> Identifiers { get; set; } = [];

    public ICollection<MusicCreditEntity> Credits { get; set; } = [];

    public ICollection<MusicTrackEntity> Tracks { get; set; } = [];

    public ICollection<MusicReleaseGenreEntity> GenreLinks { get; set; } = [];

    public MusicFormatEntity? Format { get; set; }

    /// <summary>Type name, id, and title.</summary>
    public override string ToString() => $"{GetType().Name} {Id} {Title}";
}