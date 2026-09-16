using Lyo.Seed;
using Lyo.Video.Postgres.Database;
using Lyo.Video.Seed;

namespace Lyo.Video.Seed.Tests;

public sealed class VideoEfSeedContributorTests
{
    [Fact]
    public async Task VideoEfSeedContributor_Count_PersistsSeriesGraph()
    {
        var transport = await SeedAsync(new VideoEfSeedContributor(), 2);
        Assert.Equal(3, transport.Items.OfType<VideoGenreEntity>().Count());
        var collection = Assert.Single(transport.Items.OfType<VideoCollectionEntity>());
        var titles = transport.Items.OfType<VideoTitleEntity>().ToList();
        var seasons = transport.Items.OfType<VideoSeasonEntity>().ToList();
        var episodes = transport.Items.OfType<VideoEpisodeEntity>().ToList();
        Assert.Equal(2, titles.Count);
        Assert.All(titles, title => {
            Assert.Equal(collection.Id, title.CollectionId);
            Assert.Contains(transport.Items.OfType<VideoAlternateTitleEntity>(), row => row.TitleId == title.Id);
            Assert.Contains(transport.Items.OfType<VideoTitleGenreEntity>(), row => row.TitleId == title.Id);
            Assert.Contains(transport.Items.OfType<VideoCreditEntity>(), row => row.TitleId == title.Id);
            var titleId = Assert.Single(transport.Items.OfType<VideoTitleIdentifierEntity>(), row => row.TitleId == title.Id);
            Assert.Equal(title.Id.ToString("N"), titleId.NormalizedValue);
            var youtube = Assert.Single(transport.Items.OfType<VideoYoutubeLinkEntity>(), row => row.TitleId == title.Id);
            Assert.Equal(16, youtube.VideoId.Length);
            Assert.Contains(transport.Items.OfType<VideoCutEntity>(), row => row.TitleId == title.Id && row.IsPrimary);
            var season = Assert.Single(seasons, row => row.TitleId == title.Id);
            var seasonEpisodes = episodes.Where(row => row.SeasonId == season.Id).ToList();
            Assert.Equal(2, seasonEpisodes.Count);
            Assert.All(seasonEpisodes, episode => {
                var episodeId = Assert.Single(transport.Items.OfType<VideoEpisodeIdentifierEntity>(), row => row.EpisodeId == episode.Id);
                Assert.Equal(episode.Id.ToString("N"), episodeId.NormalizedValue);
            });
        });
        var videoIds = transport.Items.OfType<VideoYoutubeLinkEntity>().Select(row => row.VideoId).ToList();
        Assert.Equal(videoIds.Count, videoIds.Distinct().Count());
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
