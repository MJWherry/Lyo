using System.Diagnostics;
using Lyo.Music.Enums;

namespace Lyo.Music;

/// <summary>The album as a work across editions (original, deluxe, remaster). Deleting a group keeps its releases.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class MusicReleaseGroup
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public MusicReleaseKind Kind { get; set; }

    public MusicReleaseStatus Status { get; set; }

    public DateTime CreatedTimestamp { get; set; }

    public DateTime? UpdatedTimestamp { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"MusicReleaseGroup(Id={Id}, Title=\"{Title}\", Slug=\"{Slug}\")";
}