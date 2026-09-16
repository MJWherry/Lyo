namespace Lyo.Book;

/// <summary>Full-text search over page bodies.</summary>
public sealed class PageQuery
{
    public string? FullText { get; set; }

    public Guid? BookId { get; set; }

    public Guid? ChapterId { get; set; }

    public string? Language { get; set; }

    public int? Take { get; set; }

    public int Skip { get; set; }
}
