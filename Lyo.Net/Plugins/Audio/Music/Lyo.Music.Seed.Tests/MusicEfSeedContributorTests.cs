using Lyo.Music.Postgres.Database;
using Lyo.Seed;

namespace Lyo.Music.Seed.Tests;

public sealed class MusicEfSeedContributorTests
{
    [Fact]
    public async Task MusicEfSeedContributor_Count_PersistsReleaseGraph()
    {
        var transport = await SeedAsync(new MusicEfSeedContributor(), 2);
        Assert.Equal(3, transport.Items.OfType<MusicGenreEntity>().Count());
        Assert.Equal(2, transport.Items.OfType<MusicFormatEntity>().Count());
        var groups = transport.Items.OfType<MusicReleaseGroupEntity>().ToList();
        var releases = transport.Items.OfType<MusicReleaseEntity>().ToList();
        var tracks = transport.Items.OfType<MusicTrackEntity>().ToList();
        var recordings = transport.Items.OfType<MusicRecordingEntity>().ToList();
        var credits = transport.Items.OfType<MusicCreditEntity>().ToList();
        Assert.Equal(2, groups.Count);
        Assert.Equal(2, releases.Count);
        Assert.Equal(4, tracks.Count);
        Assert.Equal(4, recordings.Count);
        Assert.All(
            releases, release => {
                Assert.Contains(groups, group => group.Id == release.ReleaseGroupId);
                Assert.NotNull(release.FormatId);
                Assert.Equal(2, transport.Items.OfType<MusicReleaseGenreEntity>().Count(row => row.ReleaseId == release.Id));
                var identifier = Assert.Single(transport.Items.OfType<MusicIdentifierEntity>(), row => row.ReleaseId == release.Id);
                Assert.Equal(release.Id.ToString("N"), identifier.NormalizedValue);
                Assert.Contains(credits, row => row.ReleaseId == release.Id);
                var releaseTracks = tracks.Where(row => row.ReleaseId == release.Id).ToList();
                Assert.Equal(2, releaseTracks.Count);
                Assert.All(
                    releaseTracks, track => {
                        Assert.Contains(recordings, row => row.Id == track.RecordingId);
                        Assert.Contains(credits, row => row.TrackId == track.Id);
                    });
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