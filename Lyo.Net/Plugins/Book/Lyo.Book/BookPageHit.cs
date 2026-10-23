using System.Diagnostics;

namespace Lyo.Book;

/// <summary>A page-level full-text hit with a headline snippet.</summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class BookPageHit
{
    public Guid PageId { get; set; }

    public Guid ChapterId { get; set; }

    public Guid BookId { get; set; }

    public int PageNumber { get; set; }

    public string? Snippet { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"BookPageHit(PageId={PageId}, BookId={BookId}, Page={PageNumber})";
}