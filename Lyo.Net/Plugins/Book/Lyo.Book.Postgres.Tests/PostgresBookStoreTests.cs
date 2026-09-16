using Lyo.Book.Enums;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Microsoft.Extensions.DependencyInjection;
using Lyo.Common.Core.Identifiers;

namespace Lyo.Book.Postgres.Tests;

public sealed class PostgresBookStoreTests(BookPostgresFixture fixture)
{
    private IBookStore Store => fixture.ServiceProvider.GetRequiredService<IBookStore>();

    [Fact]
    public async Task SaveBook_WithGraph_RoundTrips()
    {
        var ct = TestCt;
        var type = await SaveTypeAsync("fiction", "Fiction", ct);
        var format = await SaveFormatAsync("hardcover", "Hardcover", ct);
        var genre = await SaveGenreAsync("fantasy", "Fantasy", ct: ct);
        var book = NewBook($"rt-{LyoGuid.CreateV7():N}", type.Id, format.Id, [genre.Id]);
        book.AlternateTitles = [new BookAlternateTitle { Title = "Alt", Kind = TitleKind.Translated, Language = "fr", SortOrder = 1 }];
        book.Identifiers = [new BookIdentifier { Scheme = BookIdentifierSchemes.Isbn13, Value = "978-1-4028-9462-6", IsPrimary = true }];
        book.Contributors = [new BookContributor { DisplayName = "Ada Lovelace", Role = ContributorRole.Author, Ordinal = 1 }];

        await Store.SaveBookAsync(book, ct);
        var loaded = await Store.GetBookByIdAsync(book.Id, ct);
        Assert.NotNull(loaded);
        Assert.Equal("Ada Lovelace", loaded.PrimaryAuthor);
        Assert.Single(loaded.AlternateTitles);
        Assert.Equal("9781402894626", loaded.Identifiers[0].NormalizedValue);
        Assert.Equal(genre.Id, Assert.Single(loaded.GenreIds));
        Assert.Equal("Ada Lovelace", Assert.Single(loaded.Contributors).DisplayName);
    }

    [Fact]
    public async Task GetBookByIdentifier_AfterNormalize_FindsBook()
    {
        var ct = TestCt;
        var isbn = UniqueIsbn13();
        var book = NewBook($"isbn-{LyoGuid.CreateV7():N}");
        book.Identifiers = [new BookIdentifier { Scheme = BookIdentifierSchemes.Isbn13, Value = FormatIsbn(isbn), IsPrimary = true }];
        await Store.SaveBookAsync(book, ct);

        var found = await Store.GetBookByIdentifierAsync(BookIdentifierSchemes.Isbn13, FormatIsbn(isbn), ct);
        Assert.NotNull(found);
        Assert.Equal(book.Id, found.Id);
    }

    [Fact]
    public async Task SaveBook_DuplicateIsbn_ThrowsConflict()
    {
        var ct = TestCt;
        var isbn = UniqueIsbn13();
        var first = NewBook($"dup-a-{LyoGuid.CreateV7():N}");
        first.Identifiers = [new BookIdentifier { Scheme = BookIdentifierSchemes.Isbn13, Value = isbn, IsPrimary = true }];
        await Store.SaveBookAsync(first, ct);

        var second = NewBook($"dup-b-{LyoGuid.CreateV7():N}");
        second.Identifiers = [new BookIdentifier { Scheme = BookIdentifierSchemes.Isbn13, Value = FormatIsbn(isbn), IsPrimary = true }];
        await Assert.ThrowsAsync<ConflictException>(() => Store.SaveBookAsync(second, ct));
    }

    [Fact]
    public async Task SaveBook_DuplicateSlug_ThrowsConflict()
    {
        var ct = TestCt;
        var slug = $"slug-{LyoGuid.CreateV7():N}";
        await Store.SaveBookAsync(NewBook(slug), ct);
        var second = NewBook(slug);
        second.Title = "Other title";
        await Assert.ThrowsAsync<ConflictException>(() => Store.SaveBookAsync(second, ct));
    }

    [Fact]
    public async Task SearchBooks_TypeFilter_ReturnsOnlyMatching()
    {
        var ct = TestCt;
        var type = await SaveTypeAsync("filter-type", "Filter Type", ct);
        var prefix = $"typeflt-{LyoGuid.CreateV7():N}";
        var matching = NewBook($"{prefix}-yes", type.Id);
        var other = NewBook($"{prefix}-no");
        await Store.SaveBookAsync(matching, ct);
        await Store.SaveBookAsync(other, ct);
        var results = await Store.SearchBooksAsync(new BookQuery { TitleContains = prefix, TypeId = type.Id }, ct);
        Assert.Equal(matching.Id, Assert.Single(results).Id);
    }

    [Fact]
    public async Task SaveGenre_Hierarchical_PersistsParent()
    {
        var ct = TestCt;
        var parent = await SaveGenreAsync($"hp-{LyoGuid.CreateV7():N}", "Parent", ct: ct);
        var child = await SaveGenreAsync($"hc-{LyoGuid.CreateV7():N}", "Child", parent.Id, ct);
        var loaded = await Store.GetGenreByIdAsync(child.Id, ct);
        Assert.NotNull(loaded);
        Assert.Equal(parent.Id, loaded.ParentGenreId);
        await Assert.ThrowsAsync<ConflictException>(() => Store.DeleteGenreAsync(parent.Id, ct));
    }

    [Fact]
    public async Task SearchBooks_EmptyFilterBookIds_ReturnsNone()
    {
        var ct = TestCt;
        var book = NewBook($"flt-{LyoGuid.CreateV7():N}");
        await Store.SaveBookAsync(book, ct);
        var results = await Store.SearchBooksAsync(new BookQuery { FilterBookIds = [] }, ct);
        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchBooks_TakeSkip_Paginates()
    {
        var ct = TestCt;
        var prefix = $"pg-{LyoGuid.CreateV7():N}";
        await Store.SaveBookAsync(NewBook($"{prefix}-a"), ct);
        await Store.SaveBookAsync(NewBook($"{prefix}-b"), ct);
        await Store.SaveBookAsync(NewBook($"{prefix}-c"), ct);
        var page = await Store.SearchBooksAsync(new BookQuery { TitleContains = prefix, Skip = 1, Take = 1 }, ct);
        Assert.Single(page);
    }

    [Fact]
    public async Task DeleteSeries_LeavesBooksAndClearsIndex()
    {
        var ct = TestCt;
        var series = new BookSeries { Title = "Series", Slug = $"ser-{LyoGuid.CreateV7():N}", Status = BookSeriesStatus.Ongoing };
        await Store.SaveSeriesAsync(series, ct);
        var book = NewBook($"ser-book-{LyoGuid.CreateV7():N}");
        book.SeriesId = series.Id;
        book.SeriesIndex = 2;
        await Store.SaveBookAsync(book, ct);

        await Store.DeleteSeriesAsync(series.Id, ct);
        var loaded = await Store.GetBookByIdAsync(book.Id, ct);
        Assert.NotNull(loaded);
        Assert.Null(loaded.SeriesId);
        Assert.Null(loaded.SeriesIndex);
    }

    [Fact]
    public async Task SaveGenre_Cycle_Throws()
    {
        var ct = TestCt;
        var parent = await SaveGenreAsync($"p-{LyoGuid.CreateV7():N}", "Parent", ct: ct);
        var child = await SaveGenreAsync($"c-{LyoGuid.CreateV7():N}", "Child", parent.Id, ct);
        child.ParentGenreId = child.Id;
        await Assert.ThrowsAsync<UnprocessableEntityException>(() => Store.SaveGenreAsync(child, ct));
    }

    [Fact]
    public async Task DeleteBook_CascadesPages()
    {
        var ct = TestCt;
        var book = NewBook($"casc-{LyoGuid.CreateV7():N}");
        await Store.SaveBookAsync(book, ct);
        var chapter = new BookChapter { BookId = book.Id, ChapterNumber = 1, Language = "en", Title = "One" };
        await Store.SaveChapterAsync(chapter, ct);
        var page = new BookPage { ChapterId = chapter.Id, PageNumber = 1, Text = "hello world", ContentType = BookContentTypes.PlainText };
        await Store.SavePageAsync(page, ct);

        await Store.DeleteBookAsync(book.Id, ct);
        Assert.Null(await Store.GetPageByIdAsync(page.Id, ct: ct));
        Assert.Null(await Store.GetBookByIdAsync(book.Id, ct));
    }

    [Fact]
    public async Task GetPagesByChapter_OmitsText()
    {
        var ct = TestCt;
        var (_, chapter, page) = await SeedPageAsync("visible body token", BookContentTypes.PlainText, ct);
        var listed = await Store.GetPagesByChapterAsync(chapter.Id, includeText: false, ct);
        var row = Assert.Single(listed);
        Assert.Null(row.Text);
        Assert.True(row.HasContent);
        Assert.Equal(page.Id, row.Id);
        var withText = await Store.GetPageTextAsync(page.Id, ct);
        Assert.Equal("visible body token", withText);
    }

    [Fact]
    public async Task SearchPages_FindsToken_AndBookFullText()
    {
        var ct = TestCt;
        var token = $"lyoztoken{LyoGuid.CreateV7():N}";
        var (book, _, _) = await SeedPageAsync($"A paragraph about {token} in the woods.", BookContentTypes.PlainText, ct);

        var hits = await Store.SearchPagesAsync(new PageQuery { FullText = token }, ct);
        Assert.Contains(hits, h => h.BookId == book.Id && !string.IsNullOrEmpty(h.Snippet));

        var books = await Store.SearchBooksAsync(new BookQuery { FullText = token }, ct);
        Assert.Contains(books, b => b.Id == book.Id);
    }

    [Fact]
    public async Task SearchPages_HtmlTags_AreNotIndexed()
    {
        var ct = TestCt;
        var visible = $"vis{LyoGuid.CreateV7():N}";
        await SeedPageAsync($"<div class=\"hiddentagattr\">{visible}</div>", BookContentTypes.Html, ct);

        var visibleHits = await Store.SearchPagesAsync(new PageQuery { FullText = visible }, ct);
        Assert.NotEmpty(visibleHits);
        var tagHits = await Store.SearchPagesAsync(new PageQuery { FullText = "hiddentagattr" }, ct);
        Assert.Empty(tagHits);
    }

    [Fact]
    public async Task SavePage_ScanFileRefWithoutText_Persists()
    {
        var ct = TestCt;
        var book = NewBook($"scan-{LyoGuid.CreateV7():N}");
        await Store.SaveBookAsync(book, ct);
        var chapter = new BookChapter { BookId = book.Id, ChapterNumber = 1, Language = "en" };
        await Store.SaveChapterAsync(chapter, ct);
        var page = new BookPage { ChapterId = chapter.Id, PageNumber = 1, ScanFileRef = LyoGuid.CreateV7().ToString("D"), Width = 800, Height = 1200 };
        await Store.SavePageAsync(page, ct);
        var loaded = await Store.GetPageByIdAsync(page.Id, ct: ct);
        Assert.NotNull(loaded);
        Assert.Equal(page.ScanFileRef, loaded.ScanFileRef);
        Assert.False(loaded.HasContent);
    }

    [Fact]
    public async Task SavePageText_RollsUpWordCounts()
    {
        var ct = TestCt;
        var (book, chapter, page) = await SeedPageAsync("one two three", BookContentTypes.PlainText, ct);
        await Store.SavePageTextAsync(page.Id, "one two three four", BookContentTypes.PlainText, ct);
        var saved = await Store.GetPageByIdAsync(page.Id, includeText: true, ct);
        Assert.Equal("one two three four", saved!.Text);
        Assert.Equal(4, saved.WordCount);
        var ch = await Store.GetChapterByIdAsync(chapter.Id, ct);
        var loaded = await Store.GetBookByIdAsync(book.Id, ct);
        Assert.Equal(4, ch!.WordCount);
        Assert.Equal(4, loaded!.WordCount);
    }

    private async Task<(Book Book, BookChapter Chapter, BookPage Page)> SeedPageAsync(string text, string contentType, CancellationToken ct)
    {
        var book = NewBook($"txt-{LyoGuid.CreateV7():N}");
        await Store.SaveBookAsync(book, ct);
        var chapter = new BookChapter { BookId = book.Id, ChapterNumber = 1, Language = "en", Title = "Ch" };
        await Store.SaveChapterAsync(chapter, ct);
        var page = new BookPage { ChapterId = chapter.Id, PageNumber = 1, Text = text, ContentType = contentType };
        await Store.SavePageAsync(page, ct);
        return (book, chapter, page);
    }

    private async Task<BookType> SaveTypeAsync(string slug, string name, CancellationToken ct)
    {
        var type = new BookType { Slug = $"{slug}-{LyoGuid.CreateV7():N}", Name = name };
        await Store.SaveTypeAsync(type, ct);
        return type;
    }

    private async Task<BookFormat> SaveFormatAsync(string slug, string name, CancellationToken ct)
    {
        var format = new BookFormat { Slug = $"{slug}-{LyoGuid.CreateV7():N}", Name = name };
        await Store.SaveFormatAsync(format, ct);
        return format;
    }

    private async Task<BookGenre> SaveGenreAsync(string slug, string name, Guid? parentId = null, CancellationToken ct = default)
    {
        var genre = new BookGenre { Slug = $"{slug}-{LyoGuid.CreateV7():N}", Name = name, ParentGenreId = parentId };
        await Store.SaveGenreAsync(genre, ct);
        return genre;
    }

    private static Book NewBook(string slug, Guid? typeId = null, Guid? formatId = null, IReadOnlyList<Guid>? genreIds = null)
        => new() {
            Title = slug,
            Slug = slug,
            Language = "en",
            Status = BookStatus.Published,
            TypeId = typeId,
            FormatId = formatId,
            GenreIds = genreIds ?? []
        };

    private static string UniqueIsbn13()
    {
        var n = BitConverter.ToUInt64(LyoGuid.CreateV7().ToByteArray(), 0) % 10_000_000_000UL;
        return $"978{n:D10}";
    }

    private static string FormatIsbn(string digits)
        => digits.Length == 13 ? $"{digits[..3]}-{digits[3]}-{digits[4..8]}-{digits[8..12]}-{digits[12]}" : digits;
}
