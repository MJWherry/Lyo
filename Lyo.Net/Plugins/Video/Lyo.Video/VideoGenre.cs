using System.Diagnostics;

namespace Lyo.Video;

/// <summary>Hierarchical video genre. <see cref="ParentGenreId" /> is optional.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class VideoGenre
{
    public Guid Id { get; set; }

    public Guid? ParentGenreId { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"VideoGenre(Slug=\"{Slug}\")";
}