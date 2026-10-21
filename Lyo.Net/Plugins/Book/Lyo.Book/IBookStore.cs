namespace Lyo.Book;

/// <summary>Persistence for the book catalog: works, series, lookups, chapters, and page text.</summary>
public interface IBookStore
{
    Task SaveBookAsync(Book book, CancellationToken ct = default);

    Task<Book?> GetBookByIdAsync(Guid id, CancellationToken ct = default);

    Task<Book?> GetBookBySlugAsync(string slug, CancellationToken ct = default);

    Task<Book?> GetBookByIdentifierAsync(string scheme, string value, CancellationToken ct = default);

    Task<IReadOnlyList<Book>> SearchBooksAsync(BookQuery query, CancellationToken ct = default);

    Task DeleteBookAsync(Guid id, CancellationToken ct = default);

    Task SaveSeriesAsync(BookSeries series, CancellationToken ct = default);

    Task<BookSeries?> GetSeriesByIdAsync(Guid id, CancellationToken ct = default);

    Task<BookSeries?> GetSeriesBySlugAsync(string slug, CancellationToken ct = default);

    Task<IReadOnlyList<BookSeries>> ListSeriesAsync(CancellationToken ct = default);

    Task DeleteSeriesAsync(Guid id, CancellationToken ct = default);

    Task SaveTypeAsync(BookType type, CancellationToken ct = default);

    Task<BookType?> GetTypeByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<BookType>> ListTypesAsync(CancellationToken ct = default);

    Task DeleteTypeAsync(Guid id, CancellationToken ct = default);

    Task SaveFormatAsync(BookFormat format, CancellationToken ct = default);

    Task<BookFormat?> GetFormatByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<BookFormat>> ListFormatsAsync(CancellationToken ct = default);

    Task DeleteFormatAsync(Guid id, CancellationToken ct = default);

    Task SaveGenreAsync(BookGenre genre, CancellationToken ct = default);

    Task<BookGenre?> GetGenreByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<BookGenre>> ListGenresAsync(CancellationToken ct = default);

    Task DeleteGenreAsync(Guid id, CancellationToken ct = default);

    Task SaveChapterAsync(BookChapter chapter, CancellationToken ct = default);

    Task<BookChapter?> GetChapterByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<BookChapter>> GetChaptersByBookAsync(Guid bookId, string? language = null, CancellationToken ct = default);

    Task DeleteChapterAsync(Guid id, CancellationToken ct = default);

    Task SavePageAsync(BookPage page, CancellationToken ct = default);

    Task<BookPage?> GetPageByIdAsync(Guid id, bool includeText = false, CancellationToken ct = default);

    Task<IReadOnlyList<BookPage>> GetPagesByChapterAsync(Guid chapterId, bool includeText = false, CancellationToken ct = default);

    Task DeletePageAsync(Guid id, CancellationToken ct = default);

    Task<string?> GetPageTextAsync(Guid id, CancellationToken ct = default);

    Task SavePageTextAsync(Guid id, string? text, string? contentType = null, CancellationToken ct = default);

    Task<IReadOnlyList<BookPageHit>> SearchPagesAsync(PageQuery query, CancellationToken ct = default);
}