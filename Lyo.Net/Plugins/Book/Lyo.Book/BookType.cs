using System.Diagnostics;

namespace Lyo.Book;

/// <summary>Lookup row for the kind of work (fiction, textbook, …).</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class BookType
{
    public Guid Id { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"BookType(Id={Id}, Slug=\"{Slug}\")";
}
