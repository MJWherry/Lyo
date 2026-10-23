using Lyo.Podcast.Postgres.Database;
using Lyo.Seed;

namespace Lyo.Podcast.Seed.Tests;

public sealed class PodcastEfSeedContributorTests
{
    [Fact]
    public async Task PodcastEfSeedContributor_Count_PersistsShowGraph()
    {
        var transport = await SeedAsync(new PodcastEfSeedContributor(), 1);
        Assert.Equal(3, transport.Items.OfType<PodcastGenreEntity>().Count());
        var show = Assert.Single(transport.Items.OfType<PodcastShowEntity>());
        Assert.Contains(transport.Items.OfType<PodcastShowGenreEntity>(), row => row.ShowId == show.Id);
        var itunes = Assert.Single(transport.Items.OfType<PodcastShowIdentifierEntity>(), row => row.ShowId == show.Id);
        Assert.Equal(PodcastIdentifierSchemes.Itunes, itunes.Scheme);
        var season = Assert.Single(transport.Items.OfType<PodcastSeasonEntity>(), row => row.ShowId == show.Id);
        var episodes = transport.Items.OfType<PodcastEpisodeEntity>().Where(row => row.SeasonId == season.Id).ToList();
        Assert.Equal(2, episodes.Count);
        var guided = Assert.Single(episodes, row => row.EpisodeNumber == 1);
        var guid = Assert.Single(transport.Items.OfType<PodcastEpisodeIdentifierEntity>());
        Assert.Equal(guided.Id, guid.EpisodeId);
        Assert.Equal(PodcastIdentifierSchemes.EpisodeGuid, guid.Scheme);
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