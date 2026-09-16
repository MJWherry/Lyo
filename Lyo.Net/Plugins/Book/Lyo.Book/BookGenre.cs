using System.Diagnostics;

namespace Lyo.Book;

/// <summary>Hierarchical subject heading. <see cref="ParentGenreId" /> is optional.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class BookGenre
{
    public Guid Id { get; set; }

    public Guid? ParentGenreId { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"BookGenre(Id={Id}, Slug=\"{Slug}\")";
}
