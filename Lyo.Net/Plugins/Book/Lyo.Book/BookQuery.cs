using Lyo.Book.Enums;

namespace Lyo.Book;

/// <summary>Filter and pagination for catalog book search.</summary>
public sealed class BookQuery
{
    public string? TitleContains { get; set; }

    public string? PrimaryAuthorContains { get; set; }

    public string? PublisherContains { get; set; }

    public string? SubtitleContains { get; set; }

    public Guid? TypeId { get; set; }

    public Guid? FormatId { get; set; }

    public IReadOnlyList<Guid>? GenreIds { get; set; }

    public Guid? SeriesId { get; set; }

    public string? Language { get; set; }

    public int? PublishedYearFrom { get; set; }

    public int? PublishedYearTo { get; set; }

    public string? IdentifierScheme { get; set; }

    public string? IdentifierValue { get; set; }

    public string? Audience { get; set; }

    public BookStatus? Status { get; set; }

    /// <summary>Null means no id filter. An empty list returns no rows (host tag/favorite intersect).</summary>
    public IReadOnlyList<Guid>? FilterBookIds { get; set; }

    /// <summary>Full-text over page bodies and chapter title/subtitle (English config).</summary>
    public string? FullText { get; set; }

    public int? Take { get; set; }

    public int Skip { get; set; }
}
