using System.Diagnostics;

namespace Lyo.Music;

/// <summary>Hierarchical music genre. <see cref="ParentGenreId" /> is optional.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class MusicGenre
{
    public Guid Id { get; set; }

    public Guid? ParentGenreId { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"MusicGenre(Slug=\"{Slug}\")";
}
