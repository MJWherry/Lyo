using Lyo.Book.Enums;
using Lyo.Book.Postgres.Database;
using Lyo.Exceptions;
using Lyo.Exceptions.Models;
using Lyo.Health;
using Lyo.Postgres;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Lyo.Common.Core.Identifiers;

namespace Lyo.Book.Postgres;

/// <summary>Postgres-backed <see cref="IBookStore" />.</summary>
public sealed class PostgresBookStore : IBookStore, IHealth
{
    private readonly IDbContextFactory<BookDbContext> _contextFactory;

    public PostgresBookStore(IDbContextFactory<BookDbContext> contextFactory)
    {
        ArgumentHelpers.ThrowIfNull(contextFactory);
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public string HealthCheckName => "book-postgres";

    /// <inheritdoc />
    public Task<HealthResult> CheckHealthAsync(CancellationToken ct = default)
        => PostgresHealth.CheckAsync(_contextFactory, PostgresBookOptions.Schema, ct);

    /// <inheritdoc />
    public async Task SaveBookAsync(Book book, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(book);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var existing = book.Id != Guid.Empty
            ? await ctx.Books
                .Include(b => b.AlternateTitles)
                .Include(b => b.Identifiers)
                .Include(b => b.Contributors)
                .Include(b => b.GenreLinks)
                .FirstOrDefaultAsync(b => b.Id == book.Id, ct)
                .ConfigureAwait(false)
            : null;

        var id = existing?.Id ?? (book.Id == Guid.Empty ? LyoGuid.CreateV7() : book.Id);
        if (existing == null)
            ctx.Books.Add(existing = new BookEntity { Id = id, CreatedTimestamp = DateTime.UtcNow });

        ApplyBookFields(existing, book);
        existing.PrimaryAuthor = ComputePrimaryAuthor(book.Contributors);

        ctx.AlternateTitles.RemoveRange(existing.AlternateTitles);
        foreach (var alt in book.AlternateTitles)
            ctx.AlternateTitles.Add(ToAlternateTitleEntity(alt, id));

        ctx.Identifiers.RemoveRange(existing.Identifiers);
        foreach (var ident in book.Identifiers)
            ctx.Identifiers.Add(ToIdentifierEntity(ident, id));

        ctx.Contributors.RemoveRange(existing.Contributors);
        foreach (var contributor in book.Contributors)
            ctx.Contributors.Add(ToContributorEntity(contributor, id));

        ctx.BookGenres.RemoveRange(existing.GenreLinks);
        foreach (var genreId in book.GenreIds.Distinct())
            ctx.BookGenres.Add(new BookBookGenreEntity { BookId = id, GenreId = genreId });

        await SaveCatchingConflictAsync(ctx, "Book", ct).ConfigureAwait(false);
        book.Id = id;
        book.PrimaryAuthor = existing.PrimaryAuthor;
    }

    /// <inheritdoc />
    public async Task<Book?> GetBookByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await LoadBookQuery(ctx).FirstOrDefaultAsync(b => b.Id == id, ct).ConfigureAwait(false);
        return entity == null ? null : ToBook(entity);
    }

    /// <inheritdoc />
    public async Task<Book?> GetBookBySlugAsync(string slug, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(slug);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await LoadBookQuery(ctx).FirstOrDefaultAsync(b => b.Slug == slug, ct).ConfigureAwait(false);
        return entity == null ? null : ToBook(entity);
    }

    /// <inheritdoc />
    public async Task<Book?> GetBookByIdentifierAsync(string scheme, string value, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(scheme);
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(value);
        var normalized = BookIdentifierSchemes.Normalize(scheme, value);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var bookId = await ctx.Identifiers.AsNoTracking()
            .Where(i => i.Scheme == scheme && i.NormalizedValue == normalized)
            .Select(i => (Guid?)i.BookId)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        return bookId is null ? null : await GetBookByIdAsync(bookId.Value, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Book>> SearchBooksAsync(BookQuery query, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(query);
        if (query.FilterBookIds is { Count: 0 })
            return [];

        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var q = LoadBookQuery(ctx);

        if (!string.IsNullOrWhiteSpace(query.TitleContains)) {
            var needle = query.TitleContains.ToLowerInvariant();
            q = q.Where(b => b.Title.ToLower().Contains(needle) || b.AlternateTitles.Any(a => a.Title.ToLower().Contains(needle)));
        }

        if (!string.IsNullOrWhiteSpace(query.SubtitleContains)) {
            var needle = query.SubtitleContains.ToLowerInvariant();
            q = q.Where(b => b.Subtitle != null && b.Subtitle.ToLower().Contains(needle));
        }

        if (!string.IsNullOrWhiteSpace(query.PrimaryAuthorContains)) {
            var needle = query.PrimaryAuthorContains.ToLowerInvariant();
            q = q.Where(b => b.PrimaryAuthor != null && b.PrimaryAuthor.ToLower().Contains(needle));
        }

        if (!string.IsNullOrWhiteSpace(query.PublisherContains)) {
            var needle = query.PublisherContains.ToLowerInvariant();
            q = q.Where(b => b.Publisher != null && b.Publisher.ToLower().Contains(needle));
        }

        if (query.TypeId.HasValue)
            q = q.Where(b => b.TypeId == query.TypeId);
        if (query.FormatId.HasValue)
            q = q.Where(b => b.FormatId == query.FormatId);
        if (query.SeriesId.HasValue)
            q = q.Where(b => b.SeriesId == query.SeriesId);
        if (query.Status.HasValue)
            q = q.Where(b => b.Status == query.Status.Value);
        if (!string.IsNullOrWhiteSpace(query.Language))
            q = q.Where(b => b.Language == query.Language);
        if (!string.IsNullOrWhiteSpace(query.Audience))
            q = q.Where(b => b.Audience == query.Audience);
        if (query.PublishedYearFrom.HasValue)
            q = q.Where(b => b.PublishedYear >= query.PublishedYearFrom);
        if (query.PublishedYearTo.HasValue)
            q = q.Where(b => b.PublishedYear <= query.PublishedYearTo);
        if (query.GenreIds is { Count: > 0 }) {
            var genreIds = query.GenreIds;
            q = q.Where(b => b.GenreLinks.Any(g => genreIds.Contains(g.GenreId)));
        }

        if (!string.IsNullOrWhiteSpace(query.IdentifierScheme) && !string.IsNullOrWhiteSpace(query.IdentifierValue)) {
            var normalized = BookIdentifierSchemes.Normalize(query.IdentifierScheme, query.IdentifierValue);
            q = q.Where(b => b.Identifiers.Any(i => i.Scheme == query.IdentifierScheme && i.NormalizedValue == normalized));
        }

        if (query.FilterBookIds is { Count: > 0 })
            q = q.Where(b => query.FilterBookIds.Contains(b.Id));

        if (!string.IsNullOrWhiteSpace(query.FullText)) {
            var tsQuery = query.FullText;
            q = q.Where(b =>
                b.Chapters.Any(c =>
                    EF.Functions.ToTsVector("english", (c.Title ?? "") + " " + (c.Subtitle ?? "")).Matches(EF.Functions.PlainToTsQuery("english", tsQuery))
                    || c.Pages.Any(p => p.SearchVector.Matches(EF.Functions.PlainToTsQuery("english", tsQuery)))));
        }

        q = q.OrderBy(b => b.Title).Skip(query.Skip);
        if (query.Take.HasValue)
            q = q.Take(query.Take.Value);

        var rows = await q.ToListAsync(ct).ConfigureAwait(false);
        return rows.Select(ToBook).ToArray();
    }

    /// <inheritdoc />
    public async Task DeleteBookAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Books.FindAsync([id], ct).ConfigureAwait(false);
        if (entity == null)
            return;

        ctx.Books.Remove(entity);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SaveSeriesAsync(BookSeries series, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(series);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var existing = series.Id != Guid.Empty
            ? await ctx.Series.Include(s => s.AlternateTitles).FirstOrDefaultAsync(s => s.Id == series.Id, ct).ConfigureAwait(false)
            : null;
        var id = existing?.Id ?? (series.Id == Guid.Empty ? LyoGuid.CreateV7() : series.Id);
        if (existing == null)
            ctx.Series.Add(existing = new BookSeriesEntity { Id = id, CreatedTimestamp = DateTime.UtcNow });

        existing.Title = series.Title;
        existing.Slug = series.Slug;
        existing.Subtitle = series.Subtitle;
        existing.Description = series.Description;
        existing.Language = series.Language;
        existing.OriginalLanguage = series.OriginalLanguage;
        existing.Publisher = series.Publisher;
        existing.Imprint = series.Imprint;
        existing.Status = series.Status;
        existing.PlannedBookCount = series.PlannedBookCount;
        existing.StartedDate = ToDateOnly(series.StartedDate);
        existing.EndedDate = ToDateOnly(series.EndedDate);
        existing.CoverImageRef = series.CoverImageRef;
        existing.Source = series.Source;

        ctx.SeriesAlternateTitles.RemoveRange(existing.AlternateTitles);
        foreach (var alt in series.AlternateTitles)
            ctx.SeriesAlternateTitles.Add(ToSeriesAlternateTitleEntity(alt, id));

        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        series.Id = id;
    }

    /// <inheritdoc />
    public async Task<BookSeries?> GetSeriesByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Series.AsNoTracking().Include(s => s.AlternateTitles).FirstOrDefaultAsync(s => s.Id == id, ct).ConfigureAwait(false);
        return entity == null ? null : ToSeries(entity);
    }

    /// <inheritdoc />
    public async Task<BookSeries?> GetSeriesBySlugAsync(string slug, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNullOrWhiteSpace(slug);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Series.AsNoTracking().Include(s => s.AlternateTitles).FirstOrDefaultAsync(s => s.Slug == slug, ct).ConfigureAwait(false);
        return entity == null ? null : ToSeries(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BookSeries>> ListSeriesAsync(CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rows = await ctx.Series.AsNoTracking().Include(s => s.AlternateTitles).OrderBy(s => s.Title).ToListAsync(ct).ConfigureAwait(false);
        return rows.Select(ToSeries).ToArray();
    }

    /// <inheritdoc />
    public async Task DeleteSeriesAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Series.FindAsync([id], ct).ConfigureAwait(false);
        if (entity == null)
            return;

        var members = await ctx.Books.Where(b => b.SeriesId == id).ToListAsync(ct).ConfigureAwait(false);
        foreach (var book in members) {
            book.SeriesId = null;
            book.SeriesIndex = null;
        }

        ctx.Series.Remove(entity);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SaveTypeAsync(BookType type, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(type);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await UpsertLookupAsync(ctx.Types, type.Id, () => new BookTypeEntity(), e => {
            e.Slug = type.Slug;
            e.Name = type.Name;
            e.Description = type.Description;
            e.SortOrder = type.SortOrder;
        }, id => type.Id = id, ct).ConfigureAwait(false);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<BookType?> GetTypeByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var e = await ctx.Types.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct).ConfigureAwait(false);
        return e == null ? null : ToType(e);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BookType>> ListTypesAsync(CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rows = await ctx.Types.AsNoTracking().OrderBy(t => t.SortOrder).ThenBy(t => t.Name).ToListAsync(ct).ConfigureAwait(false);
        return rows.Select(ToType).ToArray();
    }

    /// <inheritdoc />
    public async Task DeleteTypeAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (await ctx.Books.AnyAsync(b => b.TypeId == id, ct).ConfigureAwait(false))
            throw ConflictException.ForResource("BookType", id);

        var entity = await ctx.Types.FindAsync([id], ct).ConfigureAwait(false);
        if (entity == null)
            return;

        ctx.Types.Remove(entity);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SaveFormatAsync(BookFormat format, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(format);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await UpsertLookupAsync(ctx.Formats, format.Id, () => new BookFormatEntity(), e => {
            e.Slug = format.Slug;
            e.Name = format.Name;
            e.Description = format.Description;
            e.SortOrder = format.SortOrder;
        }, id => format.Id = id, ct).ConfigureAwait(false);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<BookFormat?> GetFormatByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var e = await ctx.Formats.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct).ConfigureAwait(false);
        return e == null ? null : ToFormat(e);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BookFormat>> ListFormatsAsync(CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rows = await ctx.Formats.AsNoTracking().OrderBy(t => t.SortOrder).ThenBy(t => t.Name).ToListAsync(ct).ConfigureAwait(false);
        return rows.Select(ToFormat).ToArray();
    }

    /// <inheritdoc />
    public async Task DeleteFormatAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (await ctx.Books.AnyAsync(b => b.FormatId == id, ct).ConfigureAwait(false))
            throw ConflictException.ForResource("BookFormat", id);

        var entity = await ctx.Formats.FindAsync([id], ct).ConfigureAwait(false);
        if (entity == null)
            return;

        ctx.Formats.Remove(entity);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SaveGenreAsync(BookGenre genre, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(genre);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var id = genre.Id == Guid.Empty ? LyoGuid.CreateV7() : genre.Id;
        if (genre.ParentGenreId is { } parentId)
            await EnsureNoGenreCycleAsync(ctx, id, parentId, ct).ConfigureAwait(false);

        await UpsertLookupAsync(ctx.Genres, genre.Id, () => new BookGenreEntity(), e => {
            e.ParentGenreId = genre.ParentGenreId;
            e.Slug = genre.Slug;
            e.Name = genre.Name;
            e.Description = genre.Description;
            e.SortOrder = genre.SortOrder;
        }, assigned => genre.Id = assigned, ct).ConfigureAwait(false);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<BookGenre?> GetGenreByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var e = await ctx.Genres.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct).ConfigureAwait(false);
        return e == null ? null : ToGenre(e);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BookGenre>> ListGenresAsync(CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rows = await ctx.Genres.AsNoTracking().OrderBy(t => t.SortOrder).ThenBy(t => t.Name).ToListAsync(ct).ConfigureAwait(false);
        return rows.Select(ToGenre).ToArray();
    }

    /// <inheritdoc />
    public async Task DeleteGenreAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        if (await ctx.Genres.AnyAsync(g => g.ParentGenreId == id, ct).ConfigureAwait(false)
            || await ctx.BookGenres.AnyAsync(g => g.GenreId == id, ct).ConfigureAwait(false))
            throw ConflictException.ForResource("BookGenre", id);

        var entity = await ctx.Genres.FindAsync([id], ct).ConfigureAwait(false);
        if (entity == null)
            return;

        ctx.Genres.Remove(entity);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SaveChapterAsync(BookChapter chapter, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(chapter);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var existing = chapter.Id != Guid.Empty ? await ctx.Chapters.FindAsync([chapter.Id], ct).ConfigureAwait(false) : null;
        var id = existing?.Id ?? (chapter.Id == Guid.Empty ? LyoGuid.CreateV7() : chapter.Id);
        if (existing == null)
            ctx.Chapters.Add(existing = new BookChapterEntity { Id = id, CreatedTimestamp = DateTime.UtcNow });

        existing.BookId = chapter.BookId;
        existing.ChapterNumber = chapter.ChapterNumber;
        existing.Title = chapter.Title;
        existing.Subtitle = chapter.Subtitle;
        existing.Language = chapter.Language;
        existing.SortOrder = chapter.SortOrder;
        existing.StartPrintPage = chapter.StartPrintPage;
        existing.EndPrintPage = chapter.EndPrintPage;
        existing.Summary = chapter.Summary;
        existing.PublishedDate = ToDateOnly(chapter.PublishedDate);
        existing.CoverImageRef = chapter.CoverImageRef;
        existing.Source = chapter.Source;
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        chapter.Id = id;
    }

    /// <inheritdoc />
    public async Task<BookChapter?> GetChapterByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var e = await ctx.Chapters.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct).ConfigureAwait(false);
        return e == null ? null : ToChapter(e);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BookChapter>> GetChaptersByBookAsync(Guid bookId, string? language = null, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var q = ctx.Chapters.AsNoTracking().Where(c => c.BookId == bookId);
        if (!string.IsNullOrWhiteSpace(language))
            q = q.Where(c => c.Language == language);

        var rows = await q.OrderBy(c => c.SortOrder).ThenBy(c => c.ChapterNumber).ThenBy(c => c.Language).ToListAsync(ct).ConfigureAwait(false);
        return rows.Select(ToChapter).ToArray();
    }

    /// <inheritdoc />
    public async Task DeleteChapterAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Chapters.FindAsync([id], ct).ConfigureAwait(false);
        if (entity == null)
            return;

        var bookId = entity.BookId;
        ctx.Chapters.Remove(entity);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        await RecalculateBookWordCountAsync(ctx, bookId, ct).ConfigureAwait(false);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SavePageAsync(BookPage page, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(page);
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var existing = page.Id != Guid.Empty ? await ctx.Pages.FindAsync([page.Id], ct).ConfigureAwait(false) : null;
        var id = existing?.Id ?? (page.Id == Guid.Empty ? LyoGuid.CreateV7() : page.Id);
        if (existing == null)
            ctx.Pages.Add(existing = new BookPageEntity { Id = id, CreatedTimestamp = DateTime.UtcNow });

        existing.ChapterId = page.ChapterId;
        existing.PageNumber = page.PageNumber;
        existing.PrintPageLabel = page.PrintPageLabel;
        existing.ScanFileRef = page.ScanFileRef;
        existing.Width = page.Width;
        existing.Height = page.Height;
        if (page.Text != null || page.ContentType != BookContentTypes.PlainText) {
            existing.Text = page.Text;
            existing.ContentType = string.IsNullOrWhiteSpace(page.ContentType) ? BookContentTypes.PlainText : page.ContentType;
            existing.CharacterCount = BookText.CountCharacters(page.Text);
            existing.WordCount = BookText.CountWords(page.Text);
        }

        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        page.Id = id;
        if (page.Text != null)
            await RecalculateAggregatesAsync(ctx, existing.ChapterId, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<BookPage?> GetPageByIdAsync(Guid id, bool includeText = false, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var e = await ctx.Pages.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct).ConfigureAwait(false);
        return e == null ? null : ToPage(e, includeText);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BookPage>> GetPagesByChapterAsync(Guid chapterId, bool includeText = false, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var rows = await ctx.Pages.AsNoTracking().Where(p => p.ChapterId == chapterId).OrderBy(p => p.PageNumber).ToListAsync(ct).ConfigureAwait(false);
        return rows.Select(p => ToPage(p, includeText)).ToArray();
    }

    /// <inheritdoc />
    public async Task DeletePageAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Pages.FindAsync([id], ct).ConfigureAwait(false);
        if (entity == null)
            return;

        var chapterId = entity.ChapterId;
        ctx.Pages.Remove(entity);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        await RecalculateAggregatesAsync(ctx, chapterId, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string?> GetPageTextAsync(Guid id, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await ctx.Pages.AsNoTracking().Where(p => p.Id == id).Select(p => p.Text).FirstOrDefaultAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SavePageTextAsync(Guid id, string? text, string? contentType = null, CancellationToken ct = default)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var entity = await ctx.Pages.FindAsync([id], ct).ConfigureAwait(false);
        if (entity == null)
            throw NotFoundException.ForResource("BookPage", id);

        entity.Text = text;
        if (!string.IsNullOrWhiteSpace(contentType))
            entity.ContentType = contentType;
        entity.CharacterCount = BookText.CountCharacters(text);
        entity.WordCount = BookText.CountWords(text);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        await RecalculateAggregatesAsync(ctx, entity.ChapterId, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<BookPageHit>> SearchPagesAsync(PageQuery query, CancellationToken ct = default)
    {
        ArgumentHelpers.ThrowIfNull(query);
        if (string.IsNullOrWhiteSpace(query.FullText))
            return [];

        await using var ctx = await _contextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var fullText = query.FullText;
        var q = from p in ctx.Pages.AsNoTracking()
            join c in ctx.Chapters.AsNoTracking() on p.ChapterId equals c.Id
            where p.SearchVector.Matches(EF.Functions.PlainToTsQuery("english", fullText))
            select new { p, c };

        if (query.BookId.HasValue)
            q = q.Where(x => x.c.BookId == query.BookId);
        if (query.ChapterId.HasValue)
            q = q.Where(x => x.p.ChapterId == query.ChapterId);
        if (!string.IsNullOrWhiteSpace(query.Language))
            q = q.Where(x => x.c.Language == query.Language);

        var projected = q.OrderBy(x => x.c.BookId).ThenBy(x => x.p.PageNumber).Skip(query.Skip);
        if (query.Take.HasValue)
            projected = projected.Take(query.Take.Value);

        var rows = await projected
            .Select(x => new BookPageHit {
                PageId = x.p.Id,
                ChapterId = x.p.ChapterId,
                BookId = x.c.BookId,
                PageNumber = x.p.PageNumber,
                Snippet = EF.Functions.PlainToTsQuery("english", fullText).GetResultHeadline(x.p.Text ?? "")
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        return rows;
    }

    private static IQueryable<BookEntity> LoadBookQuery(BookDbContext ctx)
        => ctx.Books.AsNoTracking()
            .Include(b => b.AlternateTitles)
            .Include(b => b.Identifiers)
            .Include(b => b.Contributors)
            .Include(b => b.GenreLinks)
            .ThenInclude(g => g.Genre);

    private static async Task UpsertLookupAsync<T>(
        DbSet<T> set,
        Guid requestedId,
        Func<T> create,
        Action<T> apply,
        Action<Guid> assignId,
        CancellationToken ct) where T : class
    {
        T? existing = null;
        if (requestedId != Guid.Empty)
            existing = await set.FindAsync([requestedId], ct).ConfigureAwait(false);

        var id = existing is BookTypeEntity t ? t.Id
            : existing is BookFormatEntity f ? f.Id
            : existing is BookGenreEntity g ? g.Id
            : requestedId == Guid.Empty ? LyoGuid.CreateV7() : requestedId;

        if (existing == null) {
            existing = create();
            switch (existing) {
                case BookTypeEntity type:
                    type.Id = id;
                    break;
                case BookFormatEntity format:
                    format.Id = id;
                    break;
                case BookGenreEntity genre:
                    genre.Id = id;
                    break;
            }

            set.Add(existing);
        }

        apply(existing);
        assignId(id);
    }

    private static async Task EnsureNoGenreCycleAsync(BookDbContext ctx, Guid genreId, Guid parentId, CancellationToken ct)
    {
        if (parentId == genreId)
            throw new UnprocessableEntityException("A genre cannot be its own parent.");

        var current = parentId;
        var guard = 0;
        while (guard++ < 64) {
            var next = await ctx.Genres.AsNoTracking().Where(g => g.Id == current).Select(g => g.ParentGenreId).FirstOrDefaultAsync(ct).ConfigureAwait(false);
            if (next == null)
                return;
            if (next == genreId)
                throw new UnprocessableEntityException("Genre parent would create a cycle.");
            current = next.Value;
        }

        throw new UnprocessableEntityException("Genre parent chain is too deep.");
    }

    private static async Task RecalculateAggregatesAsync(BookDbContext ctx, Guid chapterId, CancellationToken ct)
    {
        var chapter = await ctx.Chapters.FindAsync([chapterId], ct).ConfigureAwait(false);
        if (chapter == null)
            return;

        await ctx.Pages.Where(p => p.ChapterId == chapterId).LoadAsync(ct).ConfigureAwait(false);
        var pages = ctx.Pages.Local.Where(p => p.ChapterId == chapterId).ToArray();
        foreach (var page in pages)
            page.WordCount = BookText.CountWords(page.Text);
        chapter.PageCount = pages.Length;
        chapter.WordCount = pages.Sum(p => p.WordCount);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);

        var book = await ctx.Books.FindAsync([chapter.BookId], ct).ConfigureAwait(false);
        if (book == null)
            return;

        await ctx.Chapters.Where(c => c.BookId == chapter.BookId).LoadAsync(ct).ConfigureAwait(false);
        book.WordCount = ctx.Chapters.Local.Where(c => c.BookId == chapter.BookId).Sum(c => c.WordCount ?? 0);
        await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private static async Task RecalculateBookWordCountAsync(BookDbContext ctx, Guid bookId, CancellationToken ct)
    {
        var book = await ctx.Books.FindAsync([bookId], ct).ConfigureAwait(false);
        if (book == null)
            return;

        await ctx.Chapters.Where(c => c.BookId == bookId).LoadAsync(ct).ConfigureAwait(false);
        book.WordCount = ctx.Chapters.Local.Where(c => c.BookId == bookId).Sum(c => c.WordCount ?? 0);
    }

    private static async Task SaveCatchingConflictAsync(BookDbContext ctx, string resource, CancellationToken ct)
    {
        try {
            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg) {
            var name = pg.ConstraintName?.Contains("identifier", StringComparison.OrdinalIgnoreCase) == true
                ? "BookIdentifier"
                : resource;
            throw ConflictException.ForResource(name, innerException: ex);
        }
    }

    private static void ApplyBookFields(BookEntity existing, Book book)
    {
        existing.Title = book.Title;
        existing.Subtitle = book.Subtitle;
        existing.Slug = book.Slug;
        existing.SeriesId = book.SeriesId;
        existing.SeriesIndex = book.SeriesIndex;
        existing.TypeId = book.TypeId;
        existing.FormatId = book.FormatId;
        existing.Status = book.Status;
        existing.Description = book.Description;
        existing.Language = book.Language;
        existing.OriginalLanguage = book.OriginalLanguage;
        existing.PublishedYear = book.PublishedYear;
        existing.PublishedDate = ToDateOnly(book.PublishedDate);
        existing.Publisher = book.Publisher;
        existing.Imprint = book.Imprint;
        existing.EditionStatement = book.EditionStatement;
        existing.EditionNumber = book.EditionNumber;
        existing.PrintPageCount = book.PrintPageCount;
        existing.Audience = book.Audience;
        existing.ContentRating = book.ContentRating;
        existing.CountryOfOrigin = book.CountryOfOrigin;
        existing.CoverImageRef = book.CoverImageRef;
        existing.Source = book.Source;
    }

    private static string? ComputePrimaryAuthor(IReadOnlyList<BookContributor> contributors)
        => contributors
            .Where(c => c.Role is ContributorRole.Author or ContributorRole.CoAuthor)
            .OrderBy(c => c.Ordinal)
            .Select(c => c.DisplayName)
            .FirstOrDefault();

    private static DateOnly? ToDateOnly(DateTime? value) => value.HasValue ? DateOnly.FromDateTime(value.Value) : null;

    private static DateTime? ToDateTime(DateOnly? value) => value?.ToDateTime(TimeOnly.MinValue);

    private static BookAlternateTitleEntity ToAlternateTitleEntity(BookAlternateTitle r, Guid bookId)
        => new() {
            Id = r.Id == Guid.Empty ? LyoGuid.CreateV7() : r.Id,
            BookId = bookId,
            Title = r.Title,
            Language = r.Language,
            Kind = r.Kind,
            IsPrimary = r.IsPrimary,
            SortOrder = r.SortOrder
        };

    private static BookSeriesAlternateTitleEntity ToSeriesAlternateTitleEntity(BookSeriesAlternateTitle r, Guid seriesId)
        => new() {
            Id = r.Id == Guid.Empty ? LyoGuid.CreateV7() : r.Id,
            SeriesId = seriesId,
            Title = r.Title,
            Language = r.Language,
            Kind = r.Kind,
            IsPrimary = r.IsPrimary,
            SortOrder = r.SortOrder
        };

    private static BookIdentifierEntity ToIdentifierEntity(BookIdentifier r, Guid bookId)
    {
        var scheme = r.Scheme;
        var value = r.Value;
        return new BookIdentifierEntity {
            Id = r.Id == Guid.Empty ? LyoGuid.CreateV7() : r.Id,
            BookId = bookId,
            Scheme = scheme,
            Value = value,
            NormalizedValue = string.IsNullOrEmpty(r.NormalizedValue) ? BookIdentifierSchemes.Normalize(scheme, value) : r.NormalizedValue,
            IsPrimary = r.IsPrimary,
            Notes = r.Notes
        };
    }

    private static BookContributorEntity ToContributorEntity(BookContributor r, Guid bookId)
        => new() {
            Id = r.Id == Guid.Empty ? LyoGuid.CreateV7() : r.Id,
            BookId = bookId,
            DisplayName = r.DisplayName,
            SortName = r.SortName,
            Role = r.Role,
            Ordinal = r.Ordinal,
            Language = r.Language,
            PersonRef = r.PersonRef,
            Notes = r.Notes
        };

    private static Book ToBook(BookEntity e)
        => new() {
            Id = e.Id,
            Title = e.Title,
            Subtitle = e.Subtitle,
            Slug = e.Slug,
            SeriesId = e.SeriesId,
            SeriesIndex = e.SeriesIndex,
            PrimaryAuthor = e.PrimaryAuthor,
            TypeId = e.TypeId,
            FormatId = e.FormatId,
            Status = e.Status,
            Description = e.Description,
            Language = e.Language,
            OriginalLanguage = e.OriginalLanguage,
            PublishedYear = e.PublishedYear,
            PublishedDate = ToDateTime(e.PublishedDate),
            Publisher = e.Publisher,
            Imprint = e.Imprint,
            EditionStatement = e.EditionStatement,
            EditionNumber = e.EditionNumber,
            PrintPageCount = e.PrintPageCount,
            WordCount = e.WordCount,
            Audience = e.Audience,
            ContentRating = e.ContentRating,
            CountryOfOrigin = e.CountryOfOrigin,
            CoverImageRef = e.CoverImageRef,
            Source = e.Source,
            CreatedTimestamp = e.CreatedTimestamp,
            UpdatedTimestamp = e.UpdatedTimestamp,
            AlternateTitles = e.AlternateTitles.OrderBy(a => a.SortOrder).Select(a => new BookAlternateTitle {
                Id = a.Id,
                BookId = a.BookId,
                Title = a.Title,
                Language = a.Language,
                Kind = a.Kind,
                IsPrimary = a.IsPrimary,
                SortOrder = a.SortOrder
            }).ToArray(),
            Identifiers = e.Identifiers.Select(i => new BookIdentifier {
                Id = i.Id,
                BookId = i.BookId,
                Scheme = i.Scheme,
                Value = i.Value,
                NormalizedValue = i.NormalizedValue,
                IsPrimary = i.IsPrimary,
                Notes = i.Notes
            }).ToArray(),
            Contributors = e.Contributors.OrderBy(c => c.Ordinal).Select(c => new BookContributor {
                Id = c.Id,
                BookId = c.BookId,
                DisplayName = c.DisplayName,
                SortName = c.SortName,
                Role = c.Role,
                Ordinal = c.Ordinal,
                Language = c.Language,
                PersonRef = c.PersonRef,
                Notes = c.Notes
            }).ToArray(),
            GenreIds = e.GenreLinks.Select(g => g.GenreId).ToArray(),
            Genres = e.GenreLinks.Select(g => g.Genre).Where(g => g != null).Select(ToGenre).ToArray()
        };

    private static BookSeries ToSeries(BookSeriesEntity e)
        => new() {
            Id = e.Id,
            Title = e.Title,
            Slug = e.Slug,
            Subtitle = e.Subtitle,
            Description = e.Description,
            Language = e.Language,
            OriginalLanguage = e.OriginalLanguage,
            Publisher = e.Publisher,
            Imprint = e.Imprint,
            Status = e.Status,
            PlannedBookCount = e.PlannedBookCount,
            StartedDate = ToDateTime(e.StartedDate),
            EndedDate = ToDateTime(e.EndedDate),
            CoverImageRef = e.CoverImageRef,
            Source = e.Source,
            CreatedTimestamp = e.CreatedTimestamp,
            UpdatedTimestamp = e.UpdatedTimestamp,
            AlternateTitles = e.AlternateTitles.OrderBy(a => a.SortOrder).Select(a => new BookSeriesAlternateTitle {
                Id = a.Id,
                SeriesId = a.SeriesId,
                Title = a.Title,
                Language = a.Language,
                Kind = a.Kind,
                IsPrimary = a.IsPrimary,
                SortOrder = a.SortOrder
            }).ToArray()
        };

    private static BookType ToType(BookTypeEntity e) => new() { Id = e.Id, Slug = e.Slug, Name = e.Name, Description = e.Description, SortOrder = e.SortOrder };

    private static BookFormat ToFormat(BookFormatEntity e) => new() { Id = e.Id, Slug = e.Slug, Name = e.Name, Description = e.Description, SortOrder = e.SortOrder };

    private static BookGenre ToGenre(BookGenreEntity e)
        => new() { Id = e.Id, ParentGenreId = e.ParentGenreId, Slug = e.Slug, Name = e.Name, Description = e.Description, SortOrder = e.SortOrder };

    private static BookChapter ToChapter(BookChapterEntity e)
        => new() {
            Id = e.Id,
            BookId = e.BookId,
            ChapterNumber = e.ChapterNumber,
            Title = e.Title,
            Subtitle = e.Subtitle,
            Language = e.Language,
            SortOrder = e.SortOrder,
            StartPrintPage = e.StartPrintPage,
            EndPrintPage = e.EndPrintPage,
            PageCount = e.PageCount,
            WordCount = e.WordCount,
            Summary = e.Summary,
            PublishedDate = ToDateTime(e.PublishedDate),
            CoverImageRef = e.CoverImageRef,
            Source = e.Source,
            CreatedTimestamp = e.CreatedTimestamp,
            UpdatedTimestamp = e.UpdatedTimestamp
        };

    private static BookPage ToPage(BookPageEntity e, bool includeText)
        => new() {
            Id = e.Id,
            ChapterId = e.ChapterId,
            PageNumber = e.PageNumber,
            PrintPageLabel = e.PrintPageLabel,
            Text = includeText ? e.Text : null,
            ContentType = e.ContentType,
            CharacterCount = e.CharacterCount,
            WordCount = e.WordCount,
            ScanFileRef = e.ScanFileRef,
            Width = e.Width,
            Height = e.Height,
            CreatedTimestamp = e.CreatedTimestamp,
            UpdatedTimestamp = e.UpdatedTimestamp
        };
}
