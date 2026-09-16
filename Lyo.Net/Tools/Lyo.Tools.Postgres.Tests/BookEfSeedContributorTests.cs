using Lyo.Book.Postgres.Database;
using Lyo.Seed;
using Lyo.Tools.Postgres.Seeds;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lyo.Tools.Postgres.Tests;

public sealed class BookEfSeedContributorTests
{
    [Fact]
    public async Task BookEfSeedContributor_Count_PersistsAlternateTitles()
    {
        var transport = await SeedAsync(new BookEfSeedContributor(NullLogger<BookEfSeedContributor>.Instance), 2);
        var series = Assert.Single(transport.Items.OfType<BookSeriesEntity>());
        var books = transport.Items.OfType<BookEntity>().ToList();
        var seriesTitles = transport.Items.OfType<BookSeriesAlternateTitleEntity>().ToList();
        var bookTitles = transport.Items.OfType<BookAlternateTitleEntity>().ToList();
        Assert.Equal(2, books.Count);
        Assert.Contains(seriesTitles, row => row.SeriesId == series.Id && !string.IsNullOrWhiteSpace(row.Title));
        Assert.All(books, book => Assert.Contains(bookTitles, row => row.BookId == book.Id && !string.IsNullOrWhiteSpace(row.Title)));
    }

    private static async Task<RecordingSeedTransport> SeedAsync(SeedContributor contributor, int count)
    {
        var transport = new RecordingSeedTransport();
        var result = await new SeedRunner().SeedAsync(contributor, transport, new() { Count = count, RandomSeed = 1 }, TestCt);
        Assert.True(result.Success, string.Join("; ", result.Errors));
        return transport;
    }

    private sealed class RecordingSeedTransport : ISeedTransport
    {
        public List<object> Items { get; } = [];

        public SeedTransportKind Kind => SeedTransportKind.Ef;

        public Task<bool> IsEmptyAsync<T>(CancellationToken ct = default)
            where T : class
            => Task.FromResult(!Items.OfType<T>().Any());

        public Task PersistAsync<T>(IReadOnlyList<T> items, CancellationToken ct = default)
            where T : class
        {
            foreach (var item in items)
                Items.Add(item);
            return Task.CompletedTask;
        }
    }
}
