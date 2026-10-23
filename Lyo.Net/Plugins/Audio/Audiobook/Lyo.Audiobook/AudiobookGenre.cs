using System.Diagnostics;

namespace Lyo.Audiobook;

/// <summary>Hierarchical audiobook genre. <see cref="ParentGenreId" /> is optional. A parent cycle is rejected.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class AudiobookGenre
{
    public Guid Id { get; set; }

    public Guid? ParentGenreId { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"AudiobookGenre(Slug=\"{Slug}\")";
}