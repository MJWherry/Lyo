using System.Diagnostics;
using Lyo.Music.Enums;

namespace Lyo.Music;

/// <summary>One edition of an album, single, or EP. Optional <see cref="ReleaseGroupId" />.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class MusicRelease
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public Guid? ReleaseGroupId { get; set; }

    public MusicReleaseKind Kind { get; set; }

    public MusicReleaseStatus Status { get; set; }

    public DateTime? ReleaseDate { get; set; }

    public string? Label { get; set; }

    public string? Language { get; set; }

    public string? CoverImageRef { get; set; }

    public Guid? FormatId { get; set; }

    /// <summary>Artist or featured credit with the lowest ordinal. Recomputed on save.</summary>
    public string? PrimaryArtist { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    public IReadOnlyList<MusicIdentifier> Identifiers { get; set; } = [];

    public IReadOnlyList<MusicCredit> Credits { get; set; } = [];

    public IReadOnlyList<MusicTrack> Tracks { get; set; } = [];

    public IReadOnlyList<Guid> GenreIds { get; set; } = [];

    /// <inheritdoc />
    public override string ToString() => $"MusicRelease(Id={Id}, Title=\"{Title}\", Slug=\"{Slug}\")";
}
