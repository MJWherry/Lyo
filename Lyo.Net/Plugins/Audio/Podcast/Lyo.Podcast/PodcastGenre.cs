using System.Diagnostics;

namespace Lyo.Podcast;

/// <summary>Hierarchical podcast genre. <see cref="ParentGenreId" /> is optional. A parent cycle is rejected.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class PodcastGenre
{
    public Guid Id { get; set; }

    public Guid? ParentGenreId { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"PodcastGenre(Slug=\"{Slug}\")";
}
