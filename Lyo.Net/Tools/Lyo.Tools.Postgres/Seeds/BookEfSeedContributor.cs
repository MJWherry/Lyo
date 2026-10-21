using Bogus;
using Lyo.Book;
using Lyo.Book.Enums;
using Lyo.Book.Postgres;
using Lyo.Book.Postgres.Database;
using Lyo.Common.Core.Identifiers;
using Lyo.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Lyo.Tools.Postgres.Seeds;

/// <summary>EF seed of book types, formats, genres, series, alternate titles, books, chapters, and searchable pages.</summary>
public sealed class BookEfSeedContributor(ILogger<BookEfSeedContributor> logger) : SeedContributor
{
    /// <inheritdoc />
    public override string Name => "Book";

    /// <inheritdoc />
    public override SeedTransportKind SupportedTransports => SeedTransportKind.Ef;

    /// <inheritdoc />
    protected override void Configure(SeedGraph graph, SeedOptions options)
    {
        var faker = options.RandomSeed is { } seedValue ? new Faker { Random = new(seedValue) } : new Faker();
        var typeId = BookDbSeeder.FictionTypeId;
        var formatId = BookDbSeeder.HardcoverFormatId;
        var genreId = BookDbSeeder.FantasyGenreId;
        var seriesId = LyoGuid.CreateV7();
        graph.OnClear(async (session, ct) => {
            if (session.Transport is not EfSeedTransport<BookDbContext> ef)
                throw new SeedException("Book OnClear requires EfSeedTransport<BookDbContext>.");

            var db = ef.Context;
            await db.Pages.ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await db.Chapters.ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await db.BookGenres.ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await db.Contributors.ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await db.Identifiers.ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await db.AlternateTitles.ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await db.Books.ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await db.SeriesAlternateTitles.ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await db.Series.ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await db.Genres.ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await db.Formats.ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await db.Types.ExecuteDeleteAsync(ct).ConfigureAwait(false);
            await BookDbSeeder.SeedAsync(db, ct).ConfigureAwait(false);
        });

        graph.Entity(
                1, _ => new BookSeriesEntity {
                    Id = seriesId,
                    Title = faker.Lorem.Sentence(3).TrimEnd('.'),
                    Slug = $"series-{seriesId:N}",
                    Status = BookSeriesStatus.Ongoing,
                    CreatedTimestamp = DateTime.UtcNow
                })
            .ForEach(
                _ => 1, (series, _) => new BookSeriesAlternateTitleEntity {
                    Id = LyoGuid.CreateV7(),
                    SeriesId = series.Id,
                    Title = "Serie Semilla",
                    Language = "es",
                    Kind = TitleKind.Translated,
                    SortOrder = 1
                });

        graph.After(async (session, ct) => {
            if (session.Transport is EfSeedTransport<BookDbContext> ef)
                await BookDbSeeder.SeedAsync(ef.Context, ct).ConfigureAwait(false);
        });

        graph.Entity(options.Count, _ => BuildBook(faker, typeId, formatId, genreId, seriesId))
            .After(async (session, ct) => {
                var books = session.Generated<BookEntity>().ToList();
                var chapters = new List<BookChapterEntity>();
                var pages = new List<BookPageEntity>();
                var alternateTitles = new List<BookAlternateTitleEntity>();
                foreach (var book in books) {
                    alternateTitles.Add(
                        new() {
                            Id = LyoGuid.CreateV7(),
                            BookId = book.Id,
                            Title = book.Title + " (alt)",
                            Language = "es",
                            Kind = TitleKind.Translated,
                            SortOrder = 1
                        });

                    var chapter = new BookChapterEntity {
                        Id = LyoGuid.CreateV7(),
                        BookId = book.Id,
                        ChapterNumber = 1,
                        Title = "Chapter 1",
                        Language = "en",
                        CreatedTimestamp = DateTime.UtcNow
                    };

                    chapters.Add(chapter);
                    pages.Add(
                        new() {
                            Id = LyoGuid.CreateV7(),
                            ChapterId = chapter.Id,
                            PageNumber = 1,
                            Text = faker.Lorem.Paragraphs(2),
                            ContentType = BookContentTypes.PlainText,
                            CharacterCount = 20,
                            WordCount = 4,
                            CreatedTimestamp = DateTime.UtcNow
                        });
                }

                await session.PersistAsync(alternateTitles, ct).ConfigureAwait(false);
                await session.PersistAsync(chapters, ct).ConfigureAwait(false);
                await session.PersistAsync(pages, ct).ConfigureAwait(false);
                logger.LogInformation("Seeded {BookCount} books, {ChapterCount} chapters, {PageCount} pages.", books.Count, chapters.Count, pages.Count);
            });
    }

    private static BookEntity BuildBook(Faker faker, Guid typeId, Guid formatId, Guid genreId, Guid seriesId)
    {
        var title = faker.Lorem.Sentence(3).TrimEnd('.');
        var id = LyoGuid.CreateV7();
        var isbn = UniqueIsbn13(id);
        return new() {
            Id = id,
            Title = title,
            Slug = $"{SlugOf(title)}-{id:N}",
            SeriesId = seriesId,
            SeriesIndex = faker.Random.Int(1, 7),
            PrimaryAuthor = faker.Name.FullName(),
            TypeId = typeId,
            FormatId = formatId,
            Status = BookStatus.Published,
            Language = "en",
            PublishedYear = faker.Random.Int(1980, 2025),
            Publisher = faker.Company.CompanyName(),
            CreatedTimestamp = DateTime.UtcNow,
            Identifiers = [
                new() {
                    Id = LyoGuid.CreateV7(),
                    BookId = id,
                    Scheme = BookIdentifierSchemes.Isbn13,
                    Value = isbn,
                    NormalizedValue = isbn,
                    IsPrimary = true
                }
            ],
            Contributors = [
                new() {
                    Id = LyoGuid.CreateV7(),
                    BookId = id,
                    DisplayName = faker.Name.FullName(),
                    Role = ContributorRole.Author,
                    Ordinal = 1
                }
            ],
            GenreLinks = [new() { BookId = id, GenreId = genreId }]
        };
    }

    private static string UniqueIsbn13(Guid id)
    {
        var n = BitConverter.ToUInt64(id.ToByteArray(), 0) % 10_000_000_000UL;
        return $"978{n:D10}";
    }

    private static string SlugOf(string title) => string.Join('-', title.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
}