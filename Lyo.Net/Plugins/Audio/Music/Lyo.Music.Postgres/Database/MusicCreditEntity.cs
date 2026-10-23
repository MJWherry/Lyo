using System.Diagnostics;
using Lyo.Music.Enums;

namespace Lyo.Music.Postgres.Database;

[DebuggerDisplay("{ToString(),nq}")]
public sealed class MusicCreditEntity
{
    public Guid Id { get; set; }

    public Guid? ReleaseId { get; set; }

    public Guid? TrackId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? SortName { get; set; }

    public MusicCreditRole Role { get; set; }

    public int Ordinal { get; set; }

    public string? PersonRef { get; set; }

    public MusicReleaseEntity? Release { get; set; }

    public MusicTrackEntity? Track { get; set; }

    /// <summary>Type name and id.</summary>
    public override string ToString() => $"{GetType().Name} {Id}";
}