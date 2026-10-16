using Lyo.Audiobook.Postgres.Database;
using Lyo.Audiobook.Seed;
using Lyo.Seed;

namespace Lyo.Audiobook.Seed.Tests;

public sealed class AudiobookEfSeedContributorTests
{
    [Fact]
    public async Task AudiobookEfSeedContributor_Count_PersistsWorkGraph()
    {
        var transport = await SeedAsync(new AudiobookEfSeedContributor(), 1);
        Assert.Equal(3, transport.Items.OfType<AudiobookGenreEntity>().Count());
        var series = Assert.Single(transport.Items.OfType<AudiobookSeriesEntity>());
        var work = Assert.Single(transport.Items.OfType<AudiobookWorkEntity>());
        Assert.Equal(series.Id, work.SeriesId);
        Assert.Contains(transport.Items.OfType<AudiobookWorkGenreEntity>(), row => row.WorkId == work.Id);
        Assert.Contains(transport.Items.OfType<AudiobookCreditEntity>(), row => row.WorkId == work.Id && row.Role == Enums.AudiobookCreditRole.Narrator);
        var identifier = Assert.Single(transport.Items.OfType<AudiobookIdentifierEntity>(), row => row.WorkId == work.Id);
        Assert.Equal(AudiobookIdentifierSchemes.Isbn, identifier.Scheme);
        Assert.Equal(work.Id.ToString("N"), identifier.NormalizedValue);
        var chapter = Assert.Single(transport.Items.OfType<AudiobookChapterEntity>(), row => row.WorkId == work.Id);
        Assert.Equal(1, chapter.Number);
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
