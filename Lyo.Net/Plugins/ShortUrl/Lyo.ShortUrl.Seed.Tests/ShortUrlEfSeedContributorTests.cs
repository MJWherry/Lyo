using Lyo.Seed;
using Lyo.ShortUrl.Postgres.Database;

namespace Lyo.ShortUrl.Seed.Tests;

public sealed class ShortUrlEfSeedContributorTests
{
    [Fact]
    public async Task ShortUrlEfSeedContributor_Count_PersistsUrlsAndClicks()
    {
        var transport = await SeedAsync(new ShortUrlEfSeedContributor(), 3);
        var urls = transport.Items.OfType<ShortUrlEntity>().ToList();
        var clicks = transport.Items.OfType<UrlClickEntity>().ToList();
        Assert.Equal(3, urls.Count);
        Assert.Equal(3, clicks.Count);
        Assert.All(
            urls, url => {
                Assert.True(url.IsActive);
                Assert.False(string.IsNullOrWhiteSpace(url.LongUrl));
                Assert.Contains(clicks, click => click.ShortUrlId == url.Id);
            });
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